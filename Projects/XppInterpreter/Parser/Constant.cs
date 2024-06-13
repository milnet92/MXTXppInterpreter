using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser
{
    public class Constant : Expression
    {
        public Constant(string value, SourceCodeBinding sourceCodeBinding) : base(new Lexer.String(value), sourceCodeBinding) { }
        public Constant(int value, SourceCodeBinding sourceCodeBinding) : base(new Lexer.Int32(value), sourceCodeBinding) { }
        public Constant(long value, SourceCodeBinding sourceCodeBinding) : base(new Lexer.Int64(value), sourceCodeBinding) { }
        public Constant(bool value, SourceCodeBinding sourceCodeBinding) : base(value ? Word.True : Word.False, sourceCodeBinding) { }
        public Constant(decimal value, SourceCodeBinding sourceCodeBinding) : base(new Lexer.Real(value), sourceCodeBinding) { }
        public Constant(Word value, SourceCodeBinding sourceCodeBinding) : base(value, sourceCodeBinding) { }
        public Constant(object date, SourceCodeBinding sourceCodeBinding) : base(new Lexer.Date(date), sourceCodeBinding) { }
        public Constant(object[] container, SourceCodeBinding sourceCodeBinding) : base(new Lexer.Container(container), sourceCodeBinding) { }

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitConstant(this);
        }
    }
}
