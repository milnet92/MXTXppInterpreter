using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser
{
    public class FunctionDeclarationReference : ITypedObject
    {
        public string Name { get; }
        public Token Type { get; }
        public Word ReturnType => Type as Word;

        /// <summary>
        /// The .NET type of the return value, or null for an X++ type
        /// </summary>
        public System.Type ClrType { get; set; }

        public FunctionDeclarationReference(string name, Word type)
        {
            Name = name;
            Type = type;
        }
    }
}
