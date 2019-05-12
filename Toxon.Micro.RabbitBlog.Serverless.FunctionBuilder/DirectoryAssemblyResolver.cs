using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;

namespace Toxon.Micro.RabbitBlog.Serverless.FunctionBuilder
{
    internal class DirectoryAssemblyResolver : BaseAssemblyResolver
    {
        private readonly Dictionary<string, Lazy<AssemblyDefinition>> _libraries;

        public DirectoryAssemblyResolver(string assemblyPath)
        {
            _libraries = new Dictionary<string, Lazy<AssemblyDefinition>>();

            foreach (var library in Directory.GetFiles(Path.GetDirectoryName(assemblyPath), "*.dll"))
            {
                _libraries.Add(Path.GetFileNameWithoutExtension(library), new Lazy<AssemblyDefinition>(() => AssemblyDefinition.ReadAssembly(library, new ReaderParameters() { AssemblyResolver = this })));
            }
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            return Resolve(name, new ReaderParameters());
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (_libraries.TryGetValue(name.Name, out var asm))
                return asm.Value;

            return base.Resolve(name, parameters);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
                return;

            foreach (var lazy in _libraries.Values)
            {
                if (!lazy.IsValueCreated)
                    continue;

                lazy.Value.Dispose();
            }
        }

        public new void Dispose()
        {
            base.Dispose();
            Dispose(disposing: true);
            GC.SuppressFinalize(this);

        }
    }
}