using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.Plugins.Core;
using Toxon.Micro.RabbitBlog.Plugins.Reflection;
using Toxon.Micro.RabbitBlog.Routing;
using Toxon.Micro.RabbitBlog.Routing.Patterns;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace Toxon.Micro.RabbitBlog.Serverless.FunctionBuilder
{
    public class FunctionImplementor
    {
        private readonly ModuleDefinition _module;
        private readonly IReadOnlyCollection<PluginMetadata> _plugins;

        private readonly ModuleDefinition _runtimeModule;
        private readonly ModuleDefinition _routingModule;
        private readonly ModuleDefinition _serverlessCoreModule;

        private readonly TypeReference _taskType;
        private readonly TypeReference _genericTaskType;
        private readonly TypeReference _cancellationTokenType;
        private readonly TypeReference _messageType;
        private readonly TypeReference _localModelType;

        private readonly TypeReference _rpcReturnType;
        private readonly TypeReference _busReturnType;
        private readonly MethodReference _rpcFuncConstructor;
        private readonly MethodReference _busFuncConstructor;
        private readonly MethodReference _rpcRegisterMethod;
        private readonly MethodReference _busRegisterMethod;

        private FunctionImplementor(AssemblyDefinition assembly, IReadOnlyCollection<PluginMetadata> plugins)
        {
            _module = assembly.MainModule;
            _plugins = plugins;

            _runtimeModule = GetModule("System.Runtime");
            var coreModule = GetModule("Toxon.Micro.RabbitBlog.Core");
            _routingModule = GetModule("Toxon.Micro.RabbitBlog.Routing");
            _serverlessCoreModule = GetModule("Toxon.Micro.RabbitBlog.Serverless.Core");
            
            _taskType = _module.ImportReference(new TypeReference("System.Threading.Tasks", "Task", _runtimeModule, _module.TypeSystem.CoreLibrary));
            _genericTaskType = _module.ImportReference(new TypeReference("System.Threading.Tasks", "Task`1", _runtimeModule, _module.TypeSystem.CoreLibrary)).Resolve();
            _cancellationTokenType = _module.ImportReference(new TypeReference("System.Threading", "CancellationToken", _runtimeModule, _module.TypeSystem.CoreLibrary, true));
            _messageType = _module.ImportReference(coreModule.GetType("Toxon.Micro.RabbitBlog.Core.Message"));
            _localModelType = _module.ImportReference(_serverlessCoreModule.GetType("Toxon.Micro.RabbitBlog.Serverless.Core.LocalModel"));

            _rpcReturnType = _genericTaskType.MakeGenericInstanceType(_messageType);
            _busReturnType = _taskType;
            _rpcFuncConstructor = GetFuncConstructor(_genericTaskType.MakeGenericInstanceType(_messageType), _messageType, _cancellationTokenType);
            _busFuncConstructor = GetFuncConstructor(_taskType, _messageType, _cancellationTokenType);
            _rpcRegisterMethod = _localModelType.Resolve().Methods.Single(x => x.Name == "RegisterRpcHandlerAsync");
            _busRegisterMethod = _localModelType.Resolve().Methods.Single(x => x.Name == "RegisterBusHandlerAsync");
        }

        private ModuleDefinition GetModule(string assemblyName)
        {
            return _module.AssemblyResolver.Resolve(new AssemblyNameReference(assemblyName, new Version())).MainModule;
        }

        public static void Implement(string entryAssemblyPath, IReadOnlyCollection<PluginMetadata> plugins)
        {
            using (var assemblyFile = File.Open(entryAssemblyPath, FileMode.Open, FileAccess.ReadWrite))
            {
                var assembly = AssemblyDefinition.ReadAssembly(assemblyFile, new ReaderParameters
                {
                    AssemblyResolver = new DirectoryAssemblyResolver(entryAssemblyPath)
                });
                var implementor = new FunctionImplementor(assembly, plugins);

                foreach (var type in implementor._module.Types.ToList())
                {
                    if (type.Name == "FunctionImpl")
                        implementor._module.Types.Remove(type);
                }

                if (plugins.Any(x => x.ServiceType == ServiceType.MessageHandler))
                {
                    var functionType = implementor.CreateServiceFunctionType();
                    implementor.ImplementCtor(functionType);
                    implementor.ImplementServiceCreateHandler(functionType);
                }
                else
                {
                    var functionType = implementor.CreateHttpFunctionType();
                    implementor.ImplementCtor(functionType);
                    implementor.ImplementHttpGetServiceKey(functionType);
                    implementor.ImplementHttpGetServiceType(functionType);
                }

                assembly.Write();
            }
        }

        private void ImplementCtor(TypeDefinition functionType)
        {
            var method = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, _module.TypeSystem.Void);
            functionType.Methods.Add(method);

            var il = method.Body.GetILProcessor();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, functionType.BaseType.Resolve().GetConstructors().Single());
            il.Emit(OpCodes.Ret);
        }

        #region Service
        private TypeDefinition CreateServiceFunctionType()
        {
            var functionType = new TypeDefinition("Toxon.Micro.RabbitBlog.Serverless.ServiceEntry", "FunctionImpl", TypeAttributes.Public | TypeAttributes.Class)
            {
                BaseType = _module.GetType("Toxon.Micro.RabbitBlog.Serverless.ServiceEntry.BaseFunction")
            };

            _module.Types.Add(functionType);

            return functionType;
        }

        private void ImplementServiceCreateHandler(TypeDefinition functionType)
        {
            var plugins = _plugins.Where(x => x.ServiceType == ServiceType.MessageHandler).ToList();
            var routes = plugins.ToDictionary(x => x.ServiceKey, RouteDiscoverer.Discover);

            var routingSenderType = _module.ImportReference(_routingModule.GetType("Toxon.Micro.RabbitBlog.Routing.IRoutingSender"));

            var method = new MethodDefinition("CreateHandler", MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.Virtual, _localModelType);
            method.Parameters.Add(new ParameterDefinition("sender", ParameterAttributes.None, routingSenderType));
            functionType.Methods.Add(method);

            var il = method.Body.GetILProcessor();

            // create model
            var modelVariable = new VariableDefinition(_localModelType);
            method.Body.Variables.Add(modelVariable);

            il.Emit(OpCodes.Newobj, _module.ImportReference(_localModelType.Resolve().GetConstructors().Single()));
            il.Emit(OpCodes.Stloc, modelVariable);

            // create field & init foreach service
            var serviceFieldMap = new Dictionary<string, FieldDefinition>();
            foreach (var service in plugins)
            {
                var field = new FieldDefinition("_service" + (serviceFieldMap.Count + 1), FieldAttributes.Private, _module.ImportReference(service.Type));
                functionType.Fields.Add(field);

                serviceFieldMap.Add(service.ServiceKey, field);

                ImplementServiceInstantiation(il, service, field);
            }

            // create array for registration tasks
            var registrationTasksVariable = new VariableDefinition(new ArrayType(_taskType));
            method.Body.Variables.Add(registrationTasksVariable);

            il.Emit(OpCodes.Ldc_I4, routes.Sum(x => x.Value.Count));
            il.Emit(OpCodes.Newarr, _taskType);
            il.Emit(OpCodes.Stloc, registrationTasksVariable);

            // register routes, add to array
            var index = 0;
            foreach (var service in plugins)
            {
                var serviceField = serviceFieldMap[service.ServiceKey];
                foreach (var route in routes[service.ServiceKey])
                {
                    il.Emit(OpCodes.Ldloc, registrationTasksVariable);
                    il.Emit(OpCodes.Ldc_I4, index++);

                    ImplementRegisterRoute(il, functionType, serviceField, modelVariable, service, route);

                    il.Emit(OpCodes.Stelem_Ref);
                }
            }

            // wait for all tasks
            il.Emit(OpCodes.Ldloc, registrationTasksVariable);
            il.Emit(OpCodes.Call, _module.ImportReference(_taskType.Resolve().Methods.Single(x => x.Name == "WaitAll" && x.Parameters.Count == 1)));

            // return model
            il.Emit(OpCodes.Ldloc, modelVariable);
            il.Emit(OpCodes.Ret);
        }

        private void ImplementServiceInstantiation(ILProcessor il, PluginMetadata service, FieldReference field)
        {
            (int Supported, int Total) CheckParameters(MethodBase method)
            {
                var parameters = method.GetParameters();
                var supported = parameters.Count(parameter => parameter.ParameterType == typeof(IRoutingSender));

                return (supported, parameters.Length);
            }
            int CountSupported(MethodBase method) => CheckParameters(method).Supported;
            bool IsSupported(MethodBase method)
            {
                var (supported, total) = CheckParameters(method);

                return supported == total;
            }

            var constructor = service.Type.GetConstructors().Where(IsSupported).OrderByDescending(CountSupported).First();

            il.Emit(OpCodes.Ldarg_0);

            foreach (var parameter in constructor.GetParameters())
            {
                if (parameter.ParameterType == typeof(IRoutingSender))
                {
                    il.Emit(OpCodes.Ldarg_1);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            il.Emit(OpCodes.Newobj, _module.ImportReference(constructor));
            il.Emit(OpCodes.Stfld, field);
        }

        private void ImplementRegisterRoute(ILProcessor il, TypeDefinition functionType, FieldDefinition serviceField, VariableDefinition modelVariable, PluginMetadata service, RouteMetadata route)
        {
            var key = $"Handle_{serviceField.Name}_{route.Method.Name}";
            var isRpc = RouteHandlerFactory.IsRpc(route);

            // create private handler function
            var method = functionType.Methods.SingleOrDefault(x => x.Name == key);
            if (method == null)
            {
                var returnType = _module.ImportReference(isRpc ? _rpcReturnType : _busReturnType);
                method = new MethodDefinition(key, MethodAttributes.Private | MethodAttributes.HideBySig, returnType);
                method.Parameters.Add(new ParameterDefinition("message", ParameterAttributes.None, _messageType));
                method.Parameters.Add(new ParameterDefinition("cancellationToken", ParameterAttributes.None, _cancellationTokenType));
                functionType.Methods.Add(method);

                ImplementRouteHandler(method, serviceField, route, isRpc);
            }

            il.Emit(OpCodes.Ldloc, modelVariable);

            // RoutePatternParser.Parse("...")
            il.Emit(OpCodes.Ldstr, RouterPatternParser.UnParse(route.Route));
            var routePatternParserType = _routingModule.GetType("Toxon.Micro.RabbitBlog.Routing.Patterns.RouterPatternParser").Resolve();
            var patternParse = _module.ImportReference(routePatternParserType.Methods.Single(x => x.Name == "Parse"));
            il.Emit(OpCodes.Call, patternParse);

            // new Func<...>(method)
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldftn, method);
            il.Emit(OpCodes.Newobj, _module.ImportReference(isRpc ? _rpcFuncConstructor : _busFuncConstructor));

            // ...
            il.Emit(OpCodes.Ldc_I4, (int)(isRpc ? RouteExecution.Synchronous : RouteExecution.Asynchronous));
            il.Emit(OpCodes.Ldc_I4, (int)(isRpc ? RouteMode.Capture : RouteMode.Observe));
            var cancellationTokenNone = _module.ImportReference(_cancellationTokenType.Resolve().Properties.Single(x => x.Name == "None").GetMethod);
            il.Emit(OpCodes.Call, cancellationTokenNone);

            // model.RegisterAsync(...)
            var registerMethod = _module.ImportReference(isRpc ? _rpcRegisterMethod : _busRegisterMethod);
            il.Emit(OpCodes.Callvirt, registerMethod);
        }

        private void ImplementRouteHandler(MethodDefinition method, FieldDefinition serviceField, RouteMetadata route, bool isRpc)
        {
            var parameters = route.Method.GetParameters();
            // (Message) (T) (Message, CancellationToken) (T, CancellationToken)

            if (parameters.Length != 1 && parameters.Length != 2)
            {
                throw new NotImplementedException();
            }

            var il = method.Body.GetILProcessor();

            var messageParam = parameters[0];
            var hasCancellationToken = parameters.Length == 2;
            if (messageParam.ParameterType == typeof(Message))
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, serviceField);
                il.Emit(OpCodes.Ldarg_1);
                if (hasCancellationToken)
                {
                    il.Emit(OpCodes.Ldarg_2);
                }
                il.Emit(route.Method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, _module.ImportReference(route.Method));
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, serviceField);
                il.Emit(OpCodes.Ldftn, _module.ImportReference(route.Method));
                il.Emit(OpCodes.Newobj, _module.ImportReference(GetFuncConstructor(_module.ImportReference(route.Method.ReturnType), _module.ImportReference(messageParam.ParameterType))));

                var jsonHandlerMethod = new GenericInstanceMethod(serviceField.DeclaringType.BaseType.Resolve().Methods.Single(x => x.Name == (isRpc ? "JsonRpcHandler" : "JsonBusHandler")));
                jsonHandlerMethod.GenericArguments.Add(_module.ImportReference(messageParam.ParameterType));
                if (isRpc)
                {
                    jsonHandlerMethod.GenericArguments.Add(_module.ImportReference(route.Method.ReturnType.GetGenericArguments()[0]));
                }
                il.Emit(OpCodes.Call, jsonHandlerMethod);
            }

            il.Emit(OpCodes.Ret);
        }

        private MethodReference GetFuncConstructor(TypeReference returnType, params TypeReference[] paramTypes)
        {
            var types = paramTypes.Append(returnType).Select(x => _module.ImportReference(x)).ToArray();

            var funcType = _module.ImportReference(new TypeReference("System", $"Func`{paramTypes.Length + 1}", _runtimeModule, _module.TypeSystem.CoreLibrary)).Resolve();
            var funcConstructor = funcType.GetConstructors().Single();

            var fixedFuncConstructor = new MethodReference(funcConstructor.Name, funcConstructor.ReturnType, funcType.MakeGenericInstanceType(types))
            {
                HasThis = funcConstructor.HasThis,
                ExplicitThis = funcConstructor.ExplicitThis,
                CallingConvention = funcConstructor.CallingConvention
            };

            foreach (var param in funcConstructor.Parameters)
                fixedFuncConstructor.Parameters.Add(param);

            return fixedFuncConstructor;
        }
        #endregion

        #region HTTP

        private TypeDefinition CreateHttpFunctionType()
        {
            var functionType = new TypeDefinition("Toxon.Micro.RabbitBlog.Serverless.HttpEntry", "FunctionImpl", TypeAttributes.Public | TypeAttributes.Class)
            {
                BaseType = _module.GetType("Toxon.Micro.RabbitBlog.Serverless.HttpEntry.BaseFunction")
            };

            _module.Types.Add(functionType);

            return functionType;
        }

        private void ImplementHttpGetServiceType(TypeDefinition functionType)
        {
            var plugin = _plugins.Single(x => x.ServiceType == ServiceType.Http);

            var typeType = _module.ImportReference(new TypeReference("System", "Type", _runtimeModule, _module.TypeSystem.CoreLibrary));
            var method = new MethodDefinition("GetServiceType", MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.Virtual, typeType);
            functionType.Methods.Add(method);

            var il = method.Body.GetILProcessor();

            il.Emit(OpCodes.Ldtoken, _module.ImportReference(plugin.Type));
            il.Emit(OpCodes.Call, _module.ImportReference(typeType.Resolve().Methods.Single(x => x.Name == "GetTypeFromHandle")));
            il.Emit(OpCodes.Ret);
        }

        private void ImplementHttpGetServiceKey(TypeDefinition functionType)
        {
            var plugin = _plugins.Single(x => x.ServiceType == ServiceType.Http);

            var method = new MethodDefinition("GetServiceKey", MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.Virtual, _module.TypeSystem.String);
            functionType.Methods.Add(method);

            var il = method.Body.GetILProcessor();

            il.Emit(OpCodes.Ldstr, plugin.ServiceKey);
            il.Emit(OpCodes.Ret);
        }
        #endregion
    }
}