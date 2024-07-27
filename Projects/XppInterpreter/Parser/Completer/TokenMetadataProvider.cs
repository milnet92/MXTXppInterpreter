using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser.Completer
{
    public class TokenMetadataProvider : AstSimpleVisitor
    {
        public int LookupLine { get; set; }
        public int LookupColumn { get; set; }


        public override void VisitConstant(Constant constant)
        {
            base.VisitConstant(constant);
        }

        public override void VisitConstructor(Constructor constructor)
        {
            base.VisitConstructor(constructor);
        }

        public override void VisitVariable(Variable variable)
        {
            base.VisitVariable(variable);
        }

        public override void VisitFunctionCall(FunctionCall functionCall)
        {
            base.VisitFunctionCall(functionCall);
        }
    }
}
