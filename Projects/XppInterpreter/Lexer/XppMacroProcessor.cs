using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace XppInterpreter.Lexer
{
    /// <summary>
    /// The X++ preprocessor. The rules come from what the real compiler does: see the macro cases of
    /// Projects\XppInterpreter.Tests\Conformance\conformance.txt. A directive leaves blanks and an expansion
    /// stays on one line, so the lines of the source don't move.
    /// </summary>
    internal sealed class XppMacroProcessor
    {
        private const int MaxDepth = 32;
        private const int MaxExpansions = 10000;
        private const string EndMacro = "#endmacro";
        private const string EndIf = "#endif";

        private static readonly Regex Argument = new Regex(@"%(\d+)", RegexOptions.Compiled);

        private readonly Func<string, string> _library;
        private readonly Dictionary<string, string> _macros = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _libraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private int _expansions;

        private XppMacroProcessor(Func<string, string> library)
        {
            _library = library;
        }

        /// <param name="source">The source code</param>
        /// <param name="library">Returns the text of a macro library, or null when there is none with that name</param>
        /// <returns>The source without directives and with the macros replaced</returns>
        public static string Process(string source, Func<string, string> library)
        {
            return source.IndexOf('#') < 0 ? source : new XppMacroProcessor(library).Expand(source, 0);
        }

        private string Expand(string text, int depth)
        {
            var output = new StringBuilder(text.Length);
            int i = 0;

            while (i < text.Length)
            {
                int end = SkipLiteralOrComment(text, i);

                if (end > i)
                {
                    output.Append(text, i, end - i);
                    i = end;
                }
                else if (text[i] == '#')
                {
                    i = Directive(text, i, output, depth);
                }
                else
                {
                    output.Append(text[i]);
                    i++;
                }
            }

            return output.ToString();
        }

        private int Directive(string text, int start, StringBuilder output, int depth)
        {
            int nameEnd = IdentifierEnd(text, start + 1);

            if (nameEnd == start + 1)
            {
                output.Append('#');
                return start + 1;
            }

            string name = text.Substring(start + 1, nameEnd - start - 1);
            bool dotted = nameEnd < text.Length && text[nameEnd] == '.';

            switch (name.ToLowerInvariant())
            {
                case "define" when dotted:
                    return Define(text, start, nameEnd + 1, output);

                case "localmacro" when dotted:
                case "globalmacro" when dotted:
                case "macro" when dotted:
                    return MacroBlock(text, start, nameEnd + 1, output);

                case "undef" when dotted:
                    {
                        int end = IdentifierEnd(text, nameEnd + 1);
                        _macros.Remove(text.Substring(nameEnd + 1, end - nameEnd - 1));
                        return Blank(text, start, end, output);
                    }

                case "if" when dotted:
                    return Conditional(text, start, nameEnd + 1, false, output);

                case "ifnot" when dotted:
                    return Conditional(text, start, nameEnd + 1, true, output);

                case "macrolib" when dotted:
                    {
                        int end = IdentifierEnd(text, nameEnd + 1);

                        if (LoadLibrary(text.Substring(nameEnd + 1, end - nameEnd - 1), depth))
                        {
                            return Blank(text, start, end, output);
                        }

                        output.Append(text, start, end - start);
                        return end;
                    }

                case "endif":
                case "endmacro":
                    return Blank(text, start, nameEnd, output);

                default:
                    return Use(text, start, name, nameEnd, output, depth);
            }
        }

        private int Define(string text, int start, int nameStart, StringBuilder output)
        {
            int end = IdentifierEnd(text, nameStart);
            string name = text.Substring(nameStart, end - nameStart);

            _macros[name] = ReadParenthesized(text, ref end)?.Trim() ?? string.Empty;

            return Blank(text, start, end, output);
        }

        private int MacroBlock(string text, int start, int nameStart, StringBuilder output)
        {
            int bodyStart = IdentifierEnd(text, nameStart);
            int bodyEnd = text.IndexOf(EndMacro, bodyStart, StringComparison.OrdinalIgnoreCase);
            int end = bodyEnd < 0 ? text.Length : bodyEnd + EndMacro.Length;

            _macros[text.Substring(nameStart, bodyStart - nameStart)] = text.Substring(bodyStart, (bodyEnd < 0 ? text.Length : bodyEnd) - bodyStart).Trim();

            return Blank(text, start, end, output);
        }

        private int Conditional(string text, int start, int nameStart, bool negate, StringBuilder output)
        {
            int end = IdentifierEnd(text, nameStart);
            bool isDefined = _macros.TryGetValue(text.Substring(nameStart, end - nameStart), out string current);
            string expected = ReadParenthesized(text, ref end)?.Trim();
            bool condition = isDefined && (expected is null || string.Equals(current, expected, StringComparison.OrdinalIgnoreCase));

            return Blank(text, start, condition != negate ? end : FindEndIf(text, end), output);
        }

        private int Use(string text, int start, string name, int nameEnd, StringBuilder output, int depth)
        {
            // A macro that uses itself twice would grow without end: what is left unexpanded is reported by the lexer
            if (++_expansions > MaxExpansions)
            {
                output.Append(text, start, nameEnd - start);
                return nameEnd;
            }

            if (!_macros.TryGetValue(name, out string body))
            {
                if (LoadLibrary(name, depth))
                {
                    return Blank(text, start, nameEnd, output);
                }

                // Nothing has this name: the lexer reports it
                output.Append(text, start, nameEnd - start);
                return nameEnd;
            }

            int end = nameEnd;
            List<string> arguments = SplitArguments(ReadParenthesized(text, ref end));

            // Without an argument a %1 becomes empty text, even when the macro is used without parentheses
            string expanded = Argument.Replace(body, match =>
            {
                int number = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);

                return number >= 1 && number <= arguments.Count ? arguments[number - 1] : string.Empty;
            });

            if (depth < MaxDepth)
            {
                expanded = Expand(expanded, depth + 1);
            }

            output.Append(OneLine(expanded));

            // The arguments can use several lines
            for (int i = start; i < end; i++)
            {
                if (text[i] == '\n')
                {
                    output.Append('\n');
                }
            }

            return end;
        }

        private bool LoadLibrary(string name, int depth)
        {
            if (_libraries.Contains(name))
            {
                return true;
            }

            string source = depth < MaxDepth ? _library?.Invoke(name) : null;

            if (source is null)
            {
                return false;
            }

            _libraries.Add(name);

            // Only its definitions matter
            Expand(source, depth + 1);

            return true;
        }

        private static int IdentifierEnd(string text, int start)
        {
            int i = start;

            while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_'))
            {
                i++;
            }

            return i;
        }

        /// <returns>The text, or null when no parenthesis starts there</returns>
        private static string ReadParenthesized(string text, ref int position)
        {
            if (position >= text.Length || text[position] != '(')
            {
                return null;
            }

            int nesting = 0;
            int i = position;

            while (i < text.Length)
            {
                int end = SkipLiteralOrComment(text, i);

                if (end > i)
                {
                    i = end;
                    continue;
                }

                if (text[i] == '(')
                {
                    nesting++;
                }
                else if (text[i] == ')' && --nesting == 0)
                {
                    string content = text.Substring(position + 1, i - position - 1);
                    position = i + 1;
                    return content;
                }

                i++;
            }

            return null;
        }

        /// <summary>
        /// The comma of a nested call or a string does not separate arguments.
        /// </summary>
        private static List<string> SplitArguments(string arguments)
        {
            var result = new List<string>();

            if (arguments is null)
            {
                return result;
            }

            int nesting = 0, argumentStart = 0, i = 0;

            while (i < arguments.Length)
            {
                int end = SkipLiteralOrComment(arguments, i);

                if (end > i)
                {
                    i = end;
                    continue;
                }

                char c = arguments[i];

                if (c == '(' || c == '[') nesting++;
                if (c == ')' || c == ']') nesting--;

                if (c == ',' && nesting == 0)
                {
                    result.Add(arguments.Substring(argumentStart, i - argumentStart).Trim());
                    argumentStart = i + 1;
                }

                i++;
            }

            result.Add(arguments.Substring(argumentStart).Trim());

            return result;
        }

        private static int FindEndIf(string text, int start)
        {
            int nesting = 0;
            int i = start;

            while (i < text.Length)
            {
                int end = SkipLiteralOrComment(text, i);

                if (end > i)
                {
                    i = end;
                    continue;
                }

                if (text[i] == '#')
                {
                    if (StartsWith(text, i, EndIf))
                    {
                        if (nesting == 0)
                        {
                            return i + EndIf.Length;
                        }

                        nesting--;
                    }
                    else if (StartsWith(text, i, "#if.") || StartsWith(text, i, "#ifnot."))
                    {
                        nesting++;
                    }
                }

                i++;
            }

            return text.Length;
        }

        private static bool StartsWith(string text, int position, string value) =>
            string.Compare(text, position, value, 0, value.Length, StringComparison.OrdinalIgnoreCase) == 0;

        private static int Blank(string text, int start, int end, StringBuilder output)
        {
            for (int i = start; i < end; i++)
            {
                output.Append(text[i] == '\n' || text[i] == '\r' ? text[i] : ' ');
            }

            return end;
        }

        /// <summary>
        /// An expansion has no line breaks, so the next lines keep their numbers.
        /// </summary>
        private static string OneLine(string text)
        {
            var output = new StringBuilder(text.Length);
            int i = 0;

            while (i < text.Length)
            {
                int end = SkipLiteralOrComment(text, i);

                if (end > i)
                {
                    // A comment disappears, and a string stays as it is
                    output.Append(text[i] == '/' ? " " : text.Substring(i, end - i));
                    i = end;
                }
                else
                {
                    output.Append(text[i] == '\n' || text[i] == '\r' ? ' ' : text[i]);
                    i++;
                }
            }

            return output.ToString();
        }

        /// <returns>Where the string or the comment that starts at the position ends. The same position when none starts there.</returns>
        private static int SkipLiteralOrComment(string text, int position)
        {
            char current = text[position];
            char next = position + 1 < text.Length ? text[position + 1] : '\0';

            if (current == '/' && next == '/')
            {
                int end = text.IndexOf('\n', position);
                return end < 0 ? text.Length : end;
            }

            if (current == '/' && next == '*')
            {
                int end = text.IndexOf("*/", position + 2, StringComparison.Ordinal);
                return end < 0 ? text.Length : end + 2;
            }

            if (current == '@' && (next == '"' || next == '\''))
            {
                int end = text.IndexOf(next, position + 2);
                return end < 0 ? text.Length : end + 1;
            }

            if (current != '"' && current != '\'')
            {
                return position;
            }

            string triple = new string(current, 3);

            if (string.CompareOrdinal(text, position, triple, 0, 3) == 0)
            {
                int end = text.IndexOf(triple, position + 3, StringComparison.Ordinal);
                return end < 0 ? text.Length : end + 3;
            }

            for (int i = position + 1; i < text.Length; i++)
            {
                if (text[i] == '\\')
                {
                    i++;
                }
                else if (text[i] == current)
                {
                    return i + 1;
                }
                else if (text[i] == '\n')
                {
                    return i;
                }
            }

            return text.Length;
        }
    }
}
