using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace XppInterpreter.Core.Clr
{
    /// <summary>
    /// Finds .NET types by the name used in a script. Names are case sensitive, as in the X++ compiler.
    /// Nothing is persisted: it looks in the loaded assemblies first, and loads the others only when a name is not found.
    /// </summary>
    public sealed class ClrTypeResolver
    {
        // Framework assemblies a script can use without any reference in the model, as in compiled X++
        private static readonly string[] FrameworkAssemblies =
        {
            "System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
            "System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
            "System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
            "System.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
            "System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
            "System.Numerics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
            "System.Transactions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
            "System.Runtime.Serialization, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
            "System.ServiceModel, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
            "System.IO.Compression, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
            "System.IO.Compression.FileSystem, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
            "System.Net.Http, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            "System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            "System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            "System.Security, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            "System.Configuration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            "Microsoft.CSharp, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
        };

        // Compiled X++ is not reached through .NET names, and these are the biggest assemblies of the AOS
        private const string XppAssemblyPrefix = "Dynamics.AX.";

        // Assemblies don't change in the life of the process: what was learnt serves all the proxies
        private static readonly ConcurrentDictionary<string, Type> Found = new ConcurrentDictionary<string, Type>();
        private static readonly ConcurrentDictionary<string, int> NotFound = new ConcurrentDictionary<string, int>();
        private static readonly object IndexLock = new object();
        private static readonly HashSet<string> IndexedAssemblies = new HashSet<string>();
        private static readonly Dictionary<string, HashSet<string>> NamespaceMembers = new Dictionary<string, HashSet<string>>();
        private static readonly HashSet<string> TriedReferences = new HashSet<string>();

        private readonly Func<IEnumerable<string>> _referencedAssemblies;
        private List<string> _references;

        /// <param name="referencedAssemblies">Display names of the assemblies referenced by the models</param>
        public ClrTypeResolver(Func<IEnumerable<string>> referencedAssemblies = null)
        {
            _referencedAssemblies = referencedAssemblies;
        }

        /// <summary>
        /// Finds a public type by its full name, with a dot also before the name of a nested type.
        /// </summary>
        /// <param name="fullName">For example System.IO.Path</param>
        /// <param name="genericArity">How many type arguments the name was followed by</param>
        /// <returns>The type (a generic type definition when genericArity is not zero), or null</returns>
        public Type Find(string fullName, int genericArity = 0)
        {
            string key = genericArity == 0 ? fullName : $"{fullName}`{genericArity}";

            if (Found.TryGetValue(key, out var cached))
            {
                return cached;
            }

            // A name that was not found is looked for again only when something changed since then
            int assemblyCount = AppDomain.CurrentDomain.GetAssemblies().Length;

            if (NotFound.TryGetValue(key, out int countWhenSearched) && countWhenSearched == assemblyCount && !HasUntriedReferences())
            {
                return null;
            }

            string[] candidates = ReflectionNames(fullName, genericArity).ToArray();

            Type type = FindInLoadedAssemblies(candidates) ?? FindByLoadingAssemblies(fullName, candidates);

            if (type != null)
            {
                Found[key] = type;
            }
            else
            {
                NotFound[key] = AppDomain.CurrentDomain.GetAssemblies().Length;
            }

            return type;
        }

        /// <summary>
        /// The first part of the namespaces, for the editor. They come from the names of the assemblies, because reading their types is slow.
        /// </summary>
        public string[] GetRootNamespaces()
        {
            var names = LoadedAssemblies().Select(a => a.GetName().Name).Concat(References().Select(SimpleName));

            return names
                .Select(n => n.Split('.')[0])
                .Where(n => n.Length > 0 && char.IsUpper(n[0]) && n != "Dynamics")
                .Concat(new[] { "System" })
                .Distinct()
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// Tells if the name is a namespace, or the start of one. Only needed by the editor: it reads all the public types once.
        /// </summary>
        public bool IsNamespace(string name)
        {
            EnsureIndex(name);

            lock (IndexLock)
            {
                return NamespaceMembers.ContainsKey(name);
            }
        }

        /// <summary>
        /// The types and the namespaces directly inside a namespace.
        /// </summary>
        /// <returns>The simple names. A namespace ends with a dot.</returns>
        public IEnumerable<string> GetNamespaceMembers(string name)
        {
            EnsureIndex(name);

            lock (IndexLock)
            {
                return NamespaceMembers.TryGetValue(name, out var members) ? members.ToArray() : new string[0];
            }
        }

        /// <summary>
        /// A.B.C can be the type C of the namespace A.B, or the type C nested in the type B of the namespace A...
        /// </summary>
        private static IEnumerable<string> ReflectionNames(string fullName, int genericArity)
        {
            string suffix = genericArity == 0 ? string.Empty : $"`{genericArity}";
            string[] parts = fullName.Split('.');

            for (int nested = 0; nested < parts.Length; nested++)
            {
                int namespaceParts = parts.Length - nested - 1;

                string @namespace = string.Join(".", parts, 0, namespaceParts);
                string type = string.Join("+", parts, namespaceParts, nested + 1);

                yield return (namespaceParts > 0 ? @namespace + "." : string.Empty) + type + suffix;
            }
        }

        private static IEnumerable<Assembly> LoadedAssemblies() =>
            AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic && !a.FullName.StartsWith(XppAssemblyPrefix, StringComparison.Ordinal));

        private static Type FindInLoadedAssemblies(string[] candidates)
        {
            foreach (var assembly in LoadedAssemblies())
            {
                var type = FindInAssembly(assembly, candidates);

                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static Type FindInAssembly(Assembly assembly, string[] candidates)
        {
            foreach (var candidate in candidates)
            {
                try
                {
                    var type = assembly.GetType(candidate, false, false);

                    if (type != null && type.IsVisible)
                    {
                        return type;
                    }
                }
                catch (Exception ex) when (IsLoadFailure(ex))
                {
                    // An assembly with a missing dependency can't have the type we need
                }
            }

            return null;
        }

        private Type FindByLoadingAssemblies(string fullName, string[] candidates)
        {
            string[] parts = fullName.Split('.');
            string root = parts[0];

            // Most assemblies are named after their namespace: Azure.Storage.Files.Shares.ShareClient
            for (int length = parts.Length - 1; length > 0; length--)
            {
                var type = FindInAssembly(TryLoad(string.Join(".", parts, 0, length)), candidates);

                if (type != null)
                {
                    return type;
                }
            }

            foreach (var name in FrameworkAssemblies.Where(n => SimpleName(n).StartsWith(root, StringComparison.Ordinal)))
            {
                var type = FindInAssembly(TryLoad(name), candidates);

                if (type != null)
                {
                    return type;
                }
            }

            // Assemblies with the same root: Azure.HttpRange is in Azure.Core
            foreach (var name in TakePendingReferences(n => SimpleName(n).StartsWith(root, StringComparison.Ordinal)))
            {
                var type = FindInAssembly(TryLoad(name), candidates);

                if (type != null)
                {
                    return type;
                }
            }

            // Finally, the rest: OfficeOpenXml is in EPPlus
            foreach (var name in TakePendingReferences(n => true))
            {
                var type = FindInAssembly(TryLoad(name), candidates);

                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private List<string> References()
        {
            lock (IndexLock)
            {
                if (_references is null)
                {
                    _references = _referencedAssemblies?.Invoke()?.Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList() ?? new List<string>();
                }

                return _references;
            }
        }

        private bool HasUntriedReferences()
        {
            lock (IndexLock)
            {
                return References().Any(n => !TriedReferences.Contains(n));
            }
        }

        /// <summary>
        /// Every reference is tried only once in the life of the process.
        /// </summary>
        private List<string> TakePendingReferences(Func<string, bool> filter)
        {
            lock (IndexLock)
            {
                var taken = References().Where(n => !TriedReferences.Contains(n) && filter(n)).ToList();

                TriedReferences.UnionWith(taken);

                return taken;
            }
        }

        private static string SimpleName(string displayName)
        {
            int comma = displayName.IndexOf(',');

            return comma < 0 ? displayName : displayName.Substring(0, comma);
        }

        private static Assembly TryLoad(string name)
        {
            try
            {
                return Assembly.Load(name);
            }
            catch (Exception ex) when (IsLoadFailure(ex))
            {
                return EmptyAssembly;
            }
        }

        // FindInAssembly works on any assembly, and this one has no type with the names of a script
        private static readonly Assembly EmptyAssembly = typeof(ClrTypeResolver).Assembly;

        private static bool IsLoadFailure(Exception ex) =>
            ex is System.IO.IOException || ex is BadImageFormatException || ex is TypeLoadException || ex is ArgumentException;

        private void EnsureIndex(string name)
        {
            // The assemblies of the first part of the name may not be loaded yet
            string root = name.Split('.')[0];

            foreach (var reference in TakePendingReferences(n => SimpleName(n).StartsWith(root, StringComparison.Ordinal)))
            {
                TryLoad(reference);
            }

            foreach (var framework in FrameworkAssemblies.Where(n => SimpleName(n).StartsWith(root, StringComparison.Ordinal)))
            {
                TryLoad(framework);
            }

            lock (IndexLock)
            {
                foreach (var assembly in LoadedAssemblies().Where(a => IndexedAssemblies.Add(a.FullName)))
                {
                    AddToIndex(assembly);
                }
            }
        }

        private static void AddToIndex(Assembly assembly)
        {
            Type[] types;

            try
            {
                types = assembly.GetExportedTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null && t.IsVisible).ToArray();
            }
            catch (Exception ex) when (IsLoadFailure(ex) || ex is NotSupportedException)
            {
                return;
            }

            foreach (var type in types.Where(t => !t.IsNested && !string.IsNullOrEmpty(t.Namespace)))
            {
                int tick = type.Name.IndexOf('`');

                AddNamespaceMember(type.Namespace, tick < 0 ? type.Name : type.Name.Substring(0, tick));

                string @namespace = type.Namespace;

                for (int dot = @namespace.LastIndexOf('.'); dot > 0; dot = @namespace.LastIndexOf('.'))
                {
                    AddNamespaceMember(@namespace.Substring(0, dot), @namespace.Substring(dot + 1) + ".");
                    @namespace = @namespace.Substring(0, dot);
                }
            }
        }

        private static void AddNamespaceMember(string @namespace, string member)
        {
            if (!NamespaceMembers.TryGetValue(@namespace, out var members))
            {
                members = new HashSet<string>();
                NamespaceMembers[@namespace] = members;
            }

            members.Add(member);
        }
    }
}
