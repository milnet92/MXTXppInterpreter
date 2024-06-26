﻿using System.Collections.Generic;
using System.Diagnostics;
using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser
{
    public class Print : Statement
    {
        public List<Expression> Parameters { get; }

        public Print(List<Expression> parameters, SourceCodeBinding sourceCodeBinding, SourceCodeBinding debuggeableBinding) : base(sourceCodeBinding, debuggeableBinding)
        {
            Parameters = parameters;
        }

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitPrint(this);
        }
    }
}
