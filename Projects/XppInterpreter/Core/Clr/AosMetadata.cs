using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace XppInterpreter.Core.Clr
{
    /// <summary>
    /// Reads the metadata of the running AOS: the macro libraries and the assemblies referenced by the models.
    /// Everything is found by name on purpose. An assembly named in X++ has to exist wherever the model is compiled,
    /// and a build server doesn't have all the assemblies an AOS has.
    /// </summary>
    public sealed class AosMetadata
    {
        private const string EnvironmentAssembly = "Microsoft.Dynamics.ApplicationPlatform.Environment";
        private const string StorageAssembly = "Microsoft.Dynamics.AX.Metadata.Storage";

        private static AosMetadata current;

        private readonly object provider;

        public AosMetadata(object provider)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// The metadata of this AOS. A failure is not kept, so the next call tries again.
        /// </summary>
        public static AosMetadata Current => current ?? (current = new AosMetadata(CreateRuntimeProvider()));

        /// <summary>
        /// The text of a macro library
        /// </summary>
        /// <param name="name">Library name</param>
        /// <returns>The text, or null when there is no library with that name</returns>
        public string ReadMacroLibrary(string name)
        {
            object library = Call(Get(provider, "MacroDictionaries"), "Read", name);

            return library == null ? null : (string)Get(library, "Source");
        }

        /// <summary>
        /// The display names of the assemblies referenced by the models
        /// </summary>
        /// <returns>Assembly display names</returns>
        public string[] ReadReferencedAssemblies()
        {
            object references = Get(provider, "References");
            var names = new List<string>();

            foreach (object key in (IEnumerable)Call(references, "GetPrimaryKeys"))
            {
                object reference = Call(references, "Read", key);

                if (reference != null)
                {
                    names.Add((string)Get(reference, "AssemblyDisplayName"));
                }
            }

            return names.ToArray();
        }

        private static object CreateRuntimeProvider()
        {
            Type environmentFactory = FindType("Microsoft.Dynamics.ApplicationPlatform.Environment.EnvironmentFactory", EnvironmentAssembly);
            object environment = environmentFactory.GetMethod("GetApplicationEnvironment", Type.EmptyTypes).Invoke(null, null);
            string packageDirectory = (string)Get(Get(environment, "Aos"), "PackageDirectory");

            Type configurationType = FindType("Microsoft.Dynamics.AX.Metadata.Storage.Runtime.RuntimeProviderConfiguration", StorageAssembly);
            object configuration = Activator.CreateInstance(configurationType, packageDirectory, true, true);
            object factory = Activator.CreateInstance(FindType("Microsoft.Dynamics.AX.Metadata.Storage.MetadataProviderFactory", StorageAssembly));

            return Call(factory, "CreateRuntimeProvider", configuration);
        }

        private static Type FindType(string name, string assembly) => Type.GetType($"{name}, {assembly}", true);

        private static object Get(object target, string property)
        {
            PropertyInfo info = TypesOf(target).Select(type => type.GetProperty(property)).FirstOrDefault(found => found != null);

            if (info == null)
            {
                throw new MissingMemberException(target.GetType().FullName, property);
            }

            return info.GetValue(target);
        }

        private static object Call(object target, string method, params object[] arguments)
        {
            Type[] argumentTypes = arguments.Select(argument => argument.GetType()).ToArray();
            MethodInfo info = TypesOf(target).Select(type => type.GetMethod(method, argumentTypes)).FirstOrDefault(found => found != null);

            if (info == null)
            {
                throw new MissingMemberException(target.GetType().FullName, method);
            }

            return info.Invoke(target, arguments);
        }

        // The members of the metadata providers are declared on their interfaces
        private static IEnumerable<Type> TypesOf(object target) => new[] { target.GetType() }.Concat(target.GetType().GetInterfaces());
    }
}
