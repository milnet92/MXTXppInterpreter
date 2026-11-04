using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XppInterpreter.Tests.Infrastructure;

namespace XppInterpreter.Tests
{
    /// <summary>
    /// Every snippet of Conformance\conformance.txt was compiled and run by the real X++ compiler on a D365 environment,
    /// and the interpreter has to give the same result. The snippets that need real D365 types run only on the AOS.
    /// </summary>
    [TestClass]
    public class ConformanceTests
    {
        private sealed class ConformanceCase
        {
            public string Name;
            public string Source;
            public string Expected;
            public bool NeedsD365;
        }

        // A snippet is the body of a method of a class with these directives
        private const string Header = "using System.IO; using IO = System.IO;\n";

        // Differences that are accepted on purpose. The reason is the documentation.
        private static readonly Dictionary<string, string> KnownDifferences = new Dictionary<string, string>
        {
            // Real X++ loses the value when a generic dictionary is declared with var: the interpreter gives the right one
            ["pB82_genericDictionary"] = "1 true 1",

            // Real X++ does not apply the implicit operator and leaves year 1: the interpreter gives the right one
            ["pC07_implicitOperatorOnAssign"] = "2020",

            // Real X++ loses the value of a new DateTime assigned to an utcdatetime: the interpreter gives the right one
            ["pB24_utcFromDateTime"] = "2020-01-02T03:04:05",
        };

        // The compiler refuses these and the interpreter runs them, so a script that runs doesn't always compile in a class
        private static readonly Dictionary<string, string> AcceptedByTheInterpreter = new Dictionary<string, string>
        {
            ["pA45_notEqualChain"] = "a < b == true needs parentheses",
            ["pB91_constFieldBlocked"] = "const fields of referenced assemblies",
            ["pC09_iteratorPrivateClass"] = "var with a nested generic struct",
            ["pC29_objectAddition"] = "+ on two System.Object values",
            ["pC40_linqNotAvailable"] = "var with a nested generic struct",
            ["pG48_shadowInInnerBlock"] = "a local variable with the name of another one in an outer block",
            ["pH34_macroMissingArgument"] = "a macro argument that becomes empty text leaves a valid expression here",
            ["pI38_globalFunctionPrefix"] = "Global:: with a function the class doesn't declare",
        };

        private static List<ConformanceCase> LoadCases() => ReadCases("=> ", "=>");

        private static List<ConformanceCase> LoadRefusedCases() => ReadCases("!! REJECTED");

        private static List<ConformanceCase> ReadCases(params string[] resultStarts)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Conformance", "conformance.txt");
            var cases = new List<ConformanceCase>();

            foreach (var block in Regex.Split(File.ReadAllText(path).Replace("\r\n", "\n"), @"^### ", RegexOptions.Multiline).Skip(1))
            {
                var lines = block.TrimEnd().Split('\n');
                var title = lines[0].Trim();
                var result = lines.Last();

                if (!resultStarts.Any(resultStart => result == resultStart || result.StartsWith(resultStart)))
                {
                    continue;
                }

                cases.Add(new ConformanceCase
                {
                    Name = title.Split(' ')[0],
                    NeedsD365 = title.EndsWith("[D365]"),
                    Source = string.Join("\n", lines.Skip(1).Take(lines.Length - 2)),
                    Expected = result.Length > 3 && result.StartsWith("=> ") ? result.Substring(3) : string.Empty
                });
            }

            return cases;
        }

        private static string ToScript(ConformanceCase conformanceCase)
        {
            // A static field of the class that ran them
            string source = Regex.Replace(conformanceCase.Source, @"(?<![\w:.])counter\b", "DEVXppProbe::counter");

            source = Regex.Replace(source, @"(?<![\w:.])staticValue\b", "DEVXppProbe::staticValue");

            return Header + source;
        }

        public static IEnumerable<object[]> Cases() =>
            LoadCases().Where(c => !c.NeedsD365).Select(c => new object[] { c.Name });

        public static IEnumerable<object[]> RefusedCases() =>
            LoadRefusedCases().Where(c => !c.NeedsD365).Select(c => new object[] { c.Name });

        [DataTestMethod]
        [DynamicData(nameof(RefusedCases), DynamicDataSourceType.Method)]
        public void RefusedLikeTheCompiler(string name)
        {
            var conformanceCase = LoadRefusedCases().Single(c => c.Name == name);

            Assert.AreEqual(AcceptedByTheInterpreter.ContainsKey(name), ScriptRunner.Parse(ToScript(conformanceCase)).IsCorrect);
        }

        [TestMethod]
        public void TheFileHasCases()
        {
            var cases = LoadCases();

            Assert.IsTrue(cases.Count > 150, $"Only {cases.Count} cases were read.");
            Assert.IsTrue(cases.Count(c => !c.NeedsD365) > 100, $"Only {cases.Count(c => !c.NeedsD365)} cases run without D365.");
        }

        [DataTestMethod]
        [DynamicData(nameof(Cases), DynamicDataSourceType.Method)]
        public void SameResultAsTheCompiler(string name)
        {
            var conformanceCase = LoadCases().Single(c => c.Name == name);

            DEVXppProbe.counter = 0;
            DEVXppProbe.staticValue = 0;

            object result = ScriptRunner.Evaluate(ToScript(conformanceCase));

            string expected = KnownDifferences.TryGetValue(name, out var difference) ? difference : conformanceCase.Expected;

            Assert.AreEqual(expected, FakeXppHost.ToText(result));
        }
    }
}
