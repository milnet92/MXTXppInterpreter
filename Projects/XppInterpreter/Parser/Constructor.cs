﻿using System.Collections.Generic;
using System.Diagnostics;
using XppInterpreter.Interpreter;
using XppInterpreter.Lexer;
using XppInterpreter.Parser.Metadata;

namespace XppInterpreter.Parser
{
    public class Constructor : FunctionCall
    {
        public string ClassName => (Token as Word).Lexeme;

        public Constructor(Word identifier, string nameSpace, List<Expression> parameters, Expression caller, bool staticCall, SourceCodeBinding sourceCodeBinding, SourceCodeBinding debuggeableBinding) :
            base(identifier, parameters, caller, staticCall, false, sourceCodeBinding, debuggeableBinding)
        {
            Namespace = nameSpace;
        }

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitConstructor(this);
        }

        internal override System.Type Accept(ITypeInferExpressionVisitor inferer)
        {
            return inferer.VisitConstructor(this);
        }
    }
}
