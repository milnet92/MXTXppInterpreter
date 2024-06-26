﻿using System.Collections.Generic;
using System.Diagnostics;
using XppInterpreter.Interpreter;
using XppInterpreter.Parser.Data;

namespace XppInterpreter.Parser
{
    public class UpdateRecordset : Statement
    {
        public string TableVariableName { get; }
        public Where Where { get; set; }
        public Join Join { get; set; }
        public List<Setting> Settings { get; }

        public UpdateRecordset(string tableVariableName, List<Setting> settings, SourceCodeBinding sourceCodeBinding) : base(sourceCodeBinding, sourceCodeBinding)
        {
            TableVariableName = tableVariableName;
            Settings = settings;
        }

        [DebuggerHidden]
        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitUpdateRecordset(this);
        }
    }
}
