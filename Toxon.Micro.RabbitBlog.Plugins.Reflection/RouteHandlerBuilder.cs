using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Toxon.Micro.RabbitBlog.Core;
using Toxon.Micro.RabbitBlog.Routing.Json;

namespace Toxon.Micro.RabbitBlog.Plugins.Reflection
{
    public class RouteHandlerBuilder
    {
        private readonly ParameterExpression _message = Expression.Parameter(typeof(Message), "message");
        private readonly ParameterExpression _cancellationToken = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

        private bool _usedMessage;
        private bool _usedCancellationToken;

        private readonly List<Expression> _arguments = new List<Expression>();
        private Type _returnType;

        public void AddArgument(ParameterInfo argument)
        {
            var type = argument.ParameterType;

            if (type == typeof(CancellationToken))
            {
                if (_usedCancellationToken)
                {
                    throw new InvalidOperationException("Already used cancellationToken");
                }
                _usedCancellationToken = true;

                _arguments.Add(_cancellationToken);
            }
            else if (type == typeof(Message))
            {
                if (_usedMessage)
                {
                    throw new InvalidOperationException("Already used message");
                }
                _usedMessage = true;

                _arguments.Add(_message);
            }
            else
            {
                if (_usedMessage)
                {
                    throw new InvalidOperationException("Already used message");
                }
                _usedMessage = true;

                var readMethod = typeof(JsonMessage).GetMethod(nameof(JsonMessage.Read)).MakeGenericMethod(type);
                _arguments.Add(Expression.Call(null, readMethod, _message));
            }
        }

        public void SetReturnType(Type returnType)
        {
            _returnType = returnType;
        }

        public T Build<T>(object plugin, MethodInfo method)
        {
            Expression handler = Expression.Call(Expression.Constant(plugin), method, _arguments);

            if (_returnType != null)
            {
                handler = BuildReturn(handler);
            }

            return Expression.Lambda<T>(handler, _message, _cancellationToken).Compile();
        }

        private Expression BuildReturn(Expression handler)
        {
            if (_returnType == typeof(Message))
            {
                return handler;
            }

            var handlerTaskType = typeof(Task<>).MakeGenericType(_returnType);
            var continueWithMethod = handlerTaskType.GetMethods().Single(IsContinueWith).MakeGenericMethod(typeof(Message));
            var writeMethod = typeof(JsonMessage).GetMethod(nameof(JsonMessage.Write)).MakeGenericMethod(_returnType);

            var parameter = Expression.Parameter(handlerTaskType, "handlerTask");
            var transformLambda = Expression.Lambda(
                Expression.Call(null, writeMethod, Expression.Property(parameter, nameof(Task<object>.Result))),
                parameter
            );
            var transformFunc = transformLambda.Compile();

            return Expression.Call(handler, continueWithMethod, Expression.Constant(transformFunc));
        }

        private bool IsContinueWith(MethodInfo arg)
        {
            // Looking for Task<T1>.ContinueWith<T2>(Func<Task<T1>, T2>)
            if (arg.Name != nameof(Task<object>.ContinueWith)) return false;

            var parameters = arg.GetParameters();
            if (parameters.Length != 1) return false;

            var parameter = parameters[0];
            if (!parameter.ParameterType.IsConstructedGenericType) return false;
            if (parameter.ParameterType.GetGenericTypeDefinition() != typeof(Func<,>)) return false;
            
            var input = parameter.ParameterType.GetGenericArguments()[0];
            if (!input.IsConstructedGenericType) return false;

            return input.GetGenericTypeDefinition() == typeof(Task<>);
        }
    }
}