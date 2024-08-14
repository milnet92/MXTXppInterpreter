using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser
{
    public class Using : Statement
    {
        public VariableDeclarations VariableDeclaration { get; }  
        public Block Block { get; }
        public string VariableName => VariableDeclaration.Identifiers.First().Key.Lexeme;
        public Using(VariableDeclarations declaration, Block block, SourceCodeBinding sourceCodeBinding) : base(sourceCodeBinding, null)
        {
            VariableDeclaration = declaration;
            Block = block;
        }

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitUsing(this);
        }
    }
}
