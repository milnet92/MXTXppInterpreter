using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XppInterpreter.Core.Clr;

namespace XppInterpreter.Tests
{
    /// <summary>
    /// Compiling the model needs every assembly it references, and a build server doesn't have all the assemblies of an AOS.
    /// A reference that works on a development VM can break the build of every client, so these lists only change on purpose,
    /// after a build with the real NuGet packages. AosMetadata is the example of reaching other assemblies by name.
    /// </summary>
    [TestClass]
    public class BuildDependencyTests
    {
        private static readonly string[] InterpreterReferences = { "mscorlib", "System", "System.Core" };

        // The X++ runtime, the assemblies MXTXppCoreCastingProxy finds by name, and the XML namespace of the metadata files
        private static readonly Regex ModelNames = new Regex(@"^Microsoft\.Dynamics\.(Ax(\.Xpp\..+)?|AX\.Xpp\.Support|AX\.Data\.Core|AX\.Metadata\.V6)$");

        [TestMethod]
        public void InterpreterReferencesOnlyTheFramework()
        {
            string[] references = typeof(AosMetadata).Assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();

            CollectionAssert.IsSubsetOf(references, InterpreterReferences);
        }

        [TestMethod]
        public void ModelUsesOnlyTheExpectedAssemblies()
        {
            string model = ModelFolder();
            string[] files = new[] { "AxClass", "AxForm" }.SelectMany(folder => Directory.GetFiles(Path.Combine(model, folder), "*.xml")).ToArray();
            var unexpected = new SortedSet<string>();

            foreach (string file in files)
            {
                foreach (Match match in Regex.Matches(File.ReadAllText(file), @"\bMicrosoft(\.\w+)+"))
                {
                    if (!ModelNames.IsMatch(match.Value))
                    {
                        unexpected.Add($"{match.Value} in {Path.GetFileName(file)}");
                    }
                }
            }

            Assert.IsTrue(files.Length > 0, "The model has no files");
            Assert.AreEqual(0, unexpected.Count, string.Join(Environment.NewLine, unexpected));
        }

        private static string ModelFolder()
        {
            for (var folder = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory); folder != null; folder = folder.Parent)
            {
                string model = Path.Combine(folder.FullName, "Metadata", "MXppTools", "MXppTools");

                if (Directory.Exists(model))
                {
                    return model;
                }
            }

            throw new DirectoryNotFoundException("Metadata\\MXppTools\\MXppTools");
        }
    }

    /// <summary>
    /// The real types are only on an AOS, so these have the same shape: the members are declared on generic interfaces
    /// and Read has overloads. The real ones were checked with the macro library and reference cases on the AOS.
    /// </summary>
    [TestClass]
    public class AosMetadataTests
    {
        public sealed class Macro { public string Source { get; set; } }
        public sealed class Reference { public string AssemblyDisplayName { get; set; } }

        public interface IKeyedProvider<T> where T : class
        {
            IList<string> GetPrimaryKeys();
            T Read(string key);
            T Read(string key, int overload);
        }

        public interface IMacroProvider : IKeyedProvider<Macro> { }
        public interface IReferenceProvider : IKeyedProvider<Reference> { }

        public interface IProvider
        {
            IMacroProvider MacroDictionaries { get; }
            IReferenceProvider References { get; }
        }

        private abstract class Keyed<T> : IKeyedProvider<T> where T : class
        {
            public readonly Dictionary<string, T> Items = new Dictionary<string, T>();

            IList<string> IKeyedProvider<T>.GetPrimaryKeys() => Items.Keys.ToList();

            T IKeyedProvider<T>.Read(string key)
            {
                if (key == "Broken")
                {
                    throw new InvalidOperationException("Cannot read");
                }

                return Items.TryGetValue(key, out T item) ? item : null;
            }

            T IKeyedProvider<T>.Read(string key, int overload) => throw new InvalidOperationException("Wrong overload");
        }

        private sealed class Macros : Keyed<Macro>, IMacroProvider { }
        private sealed class References : Keyed<Reference>, IReferenceProvider { }

        private sealed class Provider : IProvider
        {
            public readonly Macros MacroItems = new Macros();
            public readonly References ReferenceItems = new References();

            IMacroProvider IProvider.MacroDictionaries => MacroItems;
            IReferenceProvider IProvider.References => ReferenceItems;
        }

        [TestMethod]
        public void ReadsMacroLibraryText()
        {
            var provider = new Provider();
            provider.MacroItems.Items["Lib"] = new Macro { Source = "#define.A(1)" };

            Assert.AreEqual("#define.A(1)", new AosMetadata(provider).ReadMacroLibrary("Lib"));
        }

        [TestMethod]
        public void UnknownMacroLibraryIsNull()
        {
            Assert.IsNull(new AosMetadata(new Provider()).ReadMacroLibrary("NoSuch"));
        }

        [TestMethod]
        public void ReadsReferencedAssemblyNames()
        {
            var provider = new Provider();
            provider.ReferenceItems.Items["A"] = new Reference { AssemblyDisplayName = "A, Version=1.0.0.0" };
            provider.ReferenceItems.Items["Gone"] = null;
            provider.ReferenceItems.Items["B"] = new Reference { AssemblyDisplayName = "B, Version=2.0.0.0" };

            CollectionAssert.AreEquivalent(new[] { "A, Version=1.0.0.0", "B, Version=2.0.0.0" }, new AosMetadata(provider).ReadReferencedAssemblies());
        }

        [TestMethod]
        public void NamesTheMissingMember()
        {
            var error = Assert.ThrowsException<MissingMemberException>(() => new AosMetadata(new object()).ReadMacroLibrary("Lib"));

            StringAssert.Contains(error.Message, "MacroDictionaries");
        }

        [TestMethod]
        public void FailureOfTheProviderReachesTheCaller()
        {
            // MXTXppCoreReflectionProxy catches it as a CLR error
            var error = Assert.ThrowsException<TargetInvocationException>(() => new AosMetadata(new Provider()).ReadMacroLibrary("Broken"));

            Assert.AreEqual("Cannot read", error.InnerException.Message);
        }

        [TestMethod]
        public void NeedsAProvider()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new AosMetadata(null));
        }
    }
}
