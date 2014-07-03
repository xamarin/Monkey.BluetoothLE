using System;
using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil;

namespace MFMetaDataProcessor
{
    /// <summary>
    /// Implements special external .NET Micro Framework assemblies resolution logic.
    /// MetadataTransformer gets maps with pair assembly name and assebly path in command line,
    /// if we unable to load assemlby using default resolver we will try to use this map.
    /// </summary>
    public sealed class LoadHintsAssemblyResolver : BaseAssemblyResolver
    {
        /// <summary>
        /// List of 'load hints' - map between assembly name and assembly path.
        /// </summary>
        private readonly IDictionary<String, String> _loadHints;

        /// <summary>
        /// Creates new instance of <see cref="LoadHintsAssemblyResolver"/> object.
        /// </summary>
        /// <param name="loadHints">Metadata transformer load hints.</param>
        public LoadHintsAssemblyResolver(
            IDictionary<String, String> loadHints)
        {
            _loadHints = loadHints;
        }

        /// <inheritdoc/>
        public override AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            try
            {
                return base.Resolve(name);
            }
            catch (Exception)
            {
                String assemblyFileName;
                if (_loadHints.TryGetValue(name.Name, out assemblyFileName))
                {
                    return AssemblyDefinition.ReadAssembly(assemblyFileName);
                }

                throw;
            }
        }

        /// <inheritdoc/>
        public override AssemblyDefinition Resolve(String fullName)
        {
            try
            {
                return base.Resolve(fullName);
            }
            catch (Exception)
            {
                String assemblyFileName;
                if (_loadHints.TryGetValue(new AssemblyName(fullName).Name, out assemblyFileName))
                {
                    return AssemblyDefinition.ReadAssembly(assemblyFileName);
                }

                throw;
            }
        }
    }
}