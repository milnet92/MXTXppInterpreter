using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Core;

namespace XppInterpreter.Lexer
{
    /// <summary>
    /// X++ language tokenizer
    /// </summary>
    public class XppLexer : ILexer
    {
        public static readonly Dictionary<string, Word> Keywords = new Dictionary<string, Word>(StringComparer.InvariantCultureIgnoreCase);
        readonly StringReader reader;
        readonly List<IScanResult> peekResults = new List<IScanResult>();
        readonly string _sourceCode;

        char peek = ' ';
        int line = 0;

        int positionEnd = 0;
        int positionStart = 0;

        int previousPositionStart = -1;
        int previousPositionEnd = -1;

        public XppLexer(string source)
        {
            _sourceCode = source;
            reader = new StringReader(_sourceCode);

            ReserveKeywords();
        }

        /// <summary>
        /// Converts an absolute source code position into [line, position] coordenates
        /// </summary>
        /// <param name="sourceCode">Source code</param>
        /// <param name="absolutPosition">Absolute position</param>
        /// <returns><see cref="SourceCodeLocation"/> with coordenate information</returns>
        public static SourceCodeLocation AnsolutePositionToSourceCodeLocation(string sourceCode, int absolutPosition)
        {
            string[] splitted = sourceCode.Substring(0, absolutPosition).Split('\n');

            int line = splitted.Count() - 1;
            int position = absolutPosition;

            if (line > 0)
            {
                int totalLength = 0;

                for (int i = 0; i <= line - 1; i++)
                {
                    totalLength += splitted[i].Length + 1;
                }

                position = absolutPosition - totalLength;
            }

            return new SourceCodeLocation(line, position);
        }

        /// <summary>
        /// Converts line, position coordinates into the absolute position
        /// </summary>
        /// <param name="sourceCode">Source code</param>
        /// <param name="location">Coordenate location</param>
        /// <returns>Absolute position</returns>
        public static int SourceCodeLocationToAbsolutePosition(string sourceCode, SourceCodeLocation location)
        {
            string[] splitted = sourceCode.Split('\n');
            int total = 0;
            for (int i = 0; i <= location.Line;i++)
            {
                // Add the carry we removed by splitting
                if (i != 0)
                {
                    total++;
                }

                if (i == location.Line)
                {
                    return total + location.Position;
                }
                else
                {
                    total += splitted[i].Length;
                }
            }

            return -1;
        }

        /// <summary>
        /// Method to be called to advance the code pointer anr returns the next token
        /// </summary>
        /// <returns></returns>
        public IScanResult GetNextToken()
        {
            return GetNextTokenInternal();
        }

        /// <summary>
        /// Gets the token at the current position if none is specified. If a position is specified,
        /// it it advances to the offset position without moving the current one
        /// </summary>
        /// <param name="offset">Token position</param>
        /// <returns>Scan result</returns>
        public IScanResult Peek(int offset = 0)
        {
            if (peekResults.Count >= offset + 1)
            {
                return peekResults[offset];
            }
            else
            {
                int skipped = peekResults.Count;
                IScanResult result = null;

                while (skipped <= offset)
                {
                    result = GetNextTokenInternal(true);
                    peekResults.Add(result);
                    skipped++;
                }

                return result;
            }
        }

        /// <summary>
        /// Reserves a token word
        /// </summary>
        /// <param name="w"></param>
        static void Reserve(Word w)
        {
            Keywords[w.Lexeme] = w;
        }

        /// <summary>
        /// Returns a new <see cref="ScanResult(Token)"/> with the <see cref="Token"/> specified
        /// and the current position
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public ScanResult ScanResult(Token token)
        {
            int resultLine = line;
            int resultPositionEnd = positionEnd - 1;
            int resultPositionStart = positionStart;

            if (positionEnd == 0)
            {
                resultLine --;
                resultPositionEnd = previousPositionEnd;
                resultPositionStart = previousPositionStart;
            }

            return new ScanResult(token, resultPositionStart, resultPositionEnd, resultLine);
        }

        /// <summary>
        /// Reads the next char and returns it only if it matches any of chars provided
        /// </summary>
        /// <param name="chars"></param>
        /// <returns></returns>
        char ReadChar(params char[] chars)
        {
            ReadChar();

            char ch = chars.FirstOrDefault(c => c == peek);

            // Not found
            if (ch != '\0')
            {
                peek = ' ';
            }

            return ch;
        }

        /// <summary>
        /// Reads the next source code char returning if it matches the one specified
        /// </summary>
        /// <param name="c">Character to check</param>
        /// <returns>True if matches, otherwise False</returns>
        bool ReadChar(char c)
        {
            ReadChar();

            if (peek != c)
            {
                return false;
            }

            peek = ' ';
            return true;
        }

        /// <summary>
        /// Reds the next character from the source code Stream
        /// </summary>
        void ReadChar()
        {
            var lastPeek = peek;

            peek = (char)reader.Read();

            if (lastPeek == char.MaxValue && peek == char.MaxValue)
            {
                return;
            }

            previousPositionEnd = positionEnd;
            positionEnd++;

            if (peek == '\n')
            {
                NewLine();
            }
        }

        /// <summary>
        /// Advances the line pointer
        /// </summary>
        void NewLine()
        {
            previousPositionStart = positionStart;
            previousPositionEnd = positionEnd - 1;

            line++;
            positionStart = 0;
            positionEnd = 0;
        }

        /// <summary>
        /// Scans a string token
        /// </summary>
        /// <param name="startingChar">String token delimiter</param>
        /// <returns>Read string</returns>
        public string ScanString(char startingChar)
        {
            StringBuilder builder = new StringBuilder();

            ReadChar();

            for (; ; ReadChar())
            {
                if (peek == startingChar)
                {
                    ReadChar();

                    return builder.ToString();
                }
                else if (peek == char.MaxValue)
                {
                    throw new ParseException("String not finalized.", new Token(TType.String), line, positionEnd);
                }

                builder.Append(peek);
            }
        }

        /// <summary>
        /// Advances the poisition to skip a code comment
        /// </summary>
        /// <returns>True if the comment was correctly formatted</returns>
        internal bool SkipComment()
        {
            char nextPeek = (char)reader.Peek();

            if (nextPeek == '/')
            {
                while (peek != '\n' && peek != char.MaxValue)
                {
                    ReadChar();
                }

                return peek == '\n';
            }
            else if (nextPeek == '*')
            {
                while (peek != char.MaxValue && (peek != '*' || ((char)reader.Peek()) != '/'))
                {
                    ReadChar();
                }

                if (peek == '*')
                {
                    ReadChar();
                    return true;
                }
            }

            return false;
        }
        
        /// <summary>
        /// Actual method to read the next token
        /// </summary>
        /// <param name="isPeek">If needs to be retrieved from the previous ones</param>
        /// <returns>Next token result</returns>
        internal IScanResult GetNextTokenInternal(bool isPeek = false)
        {
            if (!isPeek && peekResults.Count > 0)
            {
                IScanResult result = peekResults.First();
                peekResults.RemoveAt(0);
                return result;
            }

            // Keep reading until we find an elegible token
            for (; ; ReadChar())
            {
                if (peek == ' ' || peek == '\t' || peek == '\r' || peek == '\n')
                {
                    continue;
                }
                else if (peek == '/' && SkipComment())
                {
                    continue;
                }
                else
                {
                    break;
                }
            }

            previousPositionStart = positionStart;
            positionStart = positionEnd - 1;

            // We reached end of file
            if (peek == char.MaxValue)
            {
                return ScanResult(new Token(TType.EOF));
            }

            switch (peek)
            {
                case '=':
                    {
                        if (ReadChar('='))
                        {
                            ReadChar();
                            return ScanResult(Word.Equal);
                        }
                        else return ScanResult(new Token(TType.Assign));
                    }
                case '!':
                    {
                        if (ReadChar('='))
                        {
                            ReadChar();
                            return ScanResult(Word.NotEqual);
                        }
                        else return ScanResult(new Token(TType.Negation));
                    }
                case '"':
                case '\'':
                    {
                        return ScanResult(new String(ScanString(peek)));
                    }
                case '<':
                    {
                        if (ReadChar('=')) 
                        {
                            ReadChar();
                            return ScanResult(Word.LessOrEqual);
                        } 
                        else return ScanResult(new Token(TType.Smaller));
                    }
                case '>':
                    {
                        if (ReadChar('='))
                        {
                            ReadChar();
                            return ScanResult(Word.GreaterOrEqual);
                        }
                        else return ScanResult(new Token(TType.Greater));
                    }
                case '+':
                    {
                        char found = ReadChar('+', '=');
                        
                        if (found != '\0')
                        {
                            ReadChar();

                            if (found == '+') return ScanResult(Word.Increment);
                            else if (found == '=') return ScanResult(Word.PlusAssignment);
                        }

                        return ScanResult(new Token(TType.Plus));
                    }
                case '-':
                    {
                        char found = ReadChar('-', '=');

                        if (found != '\0')
                        {
                            ReadChar();

                            if (found == '-') return ScanResult(new Token(TType.Decrement));
                            else if (found == '=') return ScanResult(new Token(TType.MinusAssignment));
                        }
                        return ScanResult(new Token(TType.Minus));
                    }

                case ':':
                    {
                        if (ReadChar(':')) 
                        {
                            ReadChar();

                            return ScanResult(new Token(TType.StaticDoubleDot));
                        }
                        else return ScanResult(new Token(TType.DoubleDot));
                    }
                case '&':
                    {
                        ReadChar('&'); ReadChar(); return ScanResult(new Token(TType.And));
                    }
                case '|':
                    {
                        ReadChar('|'); ReadChar(); return ScanResult(new Token(TType.Or));
                    }
                default:
                    {
                        TType tokenType = Token.TagHelper.GetTokenType(peek);
                        if (tokenType != TType.Invalid)
                        {
                            ReadChar();
                            IScanResult result = ScanResult(new Token(tokenType));
                            return result;
                        }
                    }
                    break;
            }

            if (char.IsDigit(peek))
            {
                long value = 0;
                do
                {
                    // Converts char to his number representation
                    value = 10 * value + (peek - '0');
                    ReadChar();

                } while (char.IsDigit(peek));

                if (peek != '.')
                {
                    if (value > int.MaxValue)
                        return ScanResult(new Int64(value));
                    else
                        return ScanResult(new Int32((int)value));
                }

                decimal x = value, d = 10;
                while (true)
                {
                    ReadChar();
                    if (!char.IsDigit(peek)) break;
                    x += (peek - '0') / d;
                    d *= 10;
                }

                return ScanResult(new Real(x));
            }

            if (char.IsLetter(peek) || peek == '_')
            {
                StringBuilder builder = new StringBuilder();

                do
                {
                    builder.Append(peek);
                    ReadChar();
                } while (char.IsLetterOrDigit(peek) || peek == '_');

                string s = builder.ToString();

                if (Keywords.ContainsKey(s))
                {
                    return ScanResult(Keywords[s]);
                }
                else
                {
                    return ScanResult(new Word(s, TType.Id));
                }
            }

            throw new ParseException("Character not recognized.", new Token(TType.Invalid), line, previousPositionEnd);
        }

        /// <summary>
        /// Reserves all the word tokens
        /// </summary>
        internal static void ReserveKeywords()
        {
            Reserve(Word.If);
            Reserve(Word.Else);
            Reserve(Word.True);
            Reserve(Word.False);
            Reserve(Word.While);
            Reserve(Word.Do);
            Reserve(Word.New);
            Reserve(Word.For);
            Reserve(Word.Continue);
            Reserve(Word.Break);
            Reserve(Word.Breakpoint);
            Reserve(Word.Null);
            Reserve(Word.Try);
            Reserve(Word.Catch);
            Reserve(Word.Throw);
            Reserve(Word.Finally);
            Reserve(Word.IntegerDivision);
            Reserve(Word.Mod);
            Reserve(Word.Increment);
            Reserve(Word.Decrement);
            Reserve(Word.PlusAssignment);
            Reserve(Word.MinusAssignment);
            Reserve(Word.Or);
            Reserve(Word.And);
            Reserve(Word.Switch);
            Reserve(Word.Case);
            Reserve(Word.Default);
            Reserve(Word.TtsCommit);
            Reserve(Word.TtsAbort);
            Reserve(Word.TtsBegin);
            Reserve(Word.ChangeCompany);
            Reserve(Word.Select);
            Reserve(Word.From);
            Reserve(Word.Where);
            Reserve(Word.Join);
            Reserve(Word.Exists);
            Reserve(Word.NotExists);
            Reserve(Word.Outer);
            Reserve(Word.By);
            Reserve(Word.Group);
            Reserve(Word.Order);
            Reserve(Word.FirstOnly);
            Reserve(Word.FirstFast);
            Reserve(Word.FirstOnly10);
            Reserve(Word.FirstOnly100);
            Reserve(Word.FirstOnly1000);
            Reserve(Word.Reverse);
            Reserve(Word.RepeatableRead);
            Reserve(Word.CrossCompany);
            Reserve(Word.ValidTimeState);
            Reserve(Word.ForceLiterals);
            Reserve(Word.ForceNestedLoops);
            Reserve(Word.ForcePlaceHolders);
            Reserve(Word.GenerateOnly);
            Reserve(Word.ForceSelectOrder);
            Reserve(Word.ForUpdate);
            Reserve(Word.UpdateRecordset);
            Reserve(Word.InsertRecordset);
            Reserve(Word.DeleteFrom);
            Reserve(Word.Setting);
            Reserve(Word.MaxOf);
            Reserve(Word.MinOf);
            Reserve(Word.Count);
            Reserve(Word.Avg);
            Reserve(Word.Sum);
            Reserve(Word.Asc);
            Reserve(Word.Desc);
            Reserve(Word.Index);
            Reserve(Word.Next);
            Reserve(Word.In);
            Reserve(Word.Like);

            Reserve(Type.Int32);
            Reserve(Type.Int64);
            Reserve(Type.Anytype);
            Reserve(Type.Container);
            Reserve(Type.Real);
            Reserve(Type.DateTime);
            Reserve(Type.TimeOfDay);
            Reserve(Type.Str);
            Reserve(Type.Var);

            Reserve(Word.Void);
            Reserve(Word.Return);
        }
    }
}
