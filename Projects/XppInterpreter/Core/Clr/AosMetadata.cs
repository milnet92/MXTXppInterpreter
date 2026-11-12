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

        // The compiled metadata the AOS runs, and the source XML of the models when the environment has it (a development VM)
        private readonly object provider;
        private object sourceProvider;
        private bool sourceProviderTried;

        public AosMetadata(object provider)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// The metadata of this AOS. A failure is not kept, so the next call tries again.
        /// </summary>
        public static AosMetadata Current => current ?? (current = new AosMetadata(CreateRuntimeProvider()));

        /// <summary>
        /// The source of a macro library, from the XML of the model when it is there: a library saved in Visual Studio is
        /// read as it is, without a compile or a restart of the AOS. The compiled metadata only changes with a restart.
        /// </summary>
        /// <param name="name">Library name</param>
        /// <returns>The text, or null when there is no library with that name</returns>
        public string ReadMacroLibrary(string name)
        {
            object library = ReadMacroLibrary(SourceProvider, name) ?? ReadMacroLibrary(provider, name);

            return library == null ? null : (string)Get(library, "Source");
        }

        private static object ReadMacroLibrary(object metadataProvider, string name) =>
            metadataProvider == null ? null : Call(Get(metadataProvider, "MacroDictionaries"), "Read", name);

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

        private object SourceProvider
        {
            get
            {
                if (!sourceProviderTried)
                {
                    sourceProviderTried = true;

                    try
                    {
                        object factory = Activator.CreateInstance(FindType("Microsoft.Dynamics.AX.Metadata.Storage.MetadataProviderFactory", StorageAssembly));
                        sourceProvider = factory.GetType().GetMethod("CreateDiskProvider", new[] { typeof(string) }).Invoke(factory, new object[] { PackageDirectory() });
                    }
                    catch (Exception)
                    {
                        // No source on this environment: the compiled metadata is all there is
                    }
                }

                return sourceProvider;
            }
        }

        private static string PackageDirectory()
        {
            Type environmentFactory = FindType("Microsoft.Dynamics.ApplicationPlatform.Environment.EnvironmentFactory", EnvironmentAssembly);
            object environment = environmentFactory.GetMethod("GetApplicationEnvironment", Type.EmptyTypes).Invoke(null, null);

            return (string)Get(Get(environment, "Aos"), "PackageDirectory");
        }

        private static object CreateRuntimeProvider()
        {
            Type configurationType = FindType("Microsoft.Dynamics.AX.Metadata.Storage.Runtime.RuntimeProviderConfiguration", StorageAssembly);
            object configuration = Activator.CreateInstance(configurationType, PackageDirectory(), true, true);
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
