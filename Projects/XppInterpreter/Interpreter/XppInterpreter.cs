using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Core;
using XppInterpreter.Interpreter.Bytecode;
using XppInterpreter.Interpreter.Debug;
using XppInterpreter.Interpreter.Proxy;
using XppInterpreter.Parser;

namespace XppInterpreter.Interpreter
{
    public class XppInterpreter
    {
        private XppProxy _proxy;
        private DebugAction _nextAction = DebugAction.Continue;
        private bool _isActionDefault = true;

        public IDebugger Debugger { get; set; }
        public XppInterpreterOptions Options { get; }
        public XppInterpreter(XppProxy proxy, XppInterpreterOptions options = null)
        {
            _proxy = proxy;
            Options = options;
        }

        public InterpreterResult Continue(InterpreterResult interpreterResult, DebugAction debugAction)
        {
            _isActionDefault = false;
            _nextAction = debugAction;

            bool globalTtsAbort = Options?.EmitGlobalTtsAbort ?? false;

            InterpreterResult ret = null;
            try
            {
                ret = Interpret(interpreterResult.SaveState.ByteCode, interpreterResult.SaveState.Context);
            }
            finally
            {
                if (ret?.HasFinished ?? true && globalTtsAbort)
                    _proxy.Data.TtsAbort();
            }

            return ret;
        }

        public InterpreterResult Interpret(ByteCode byteCode, RuntimeContext context = null, bool reuseCounter = true)
        {
            RuntimeContext c =  context != null ? 
                reuseCounter ? context : new RuntimeContext(context.Proxy, byteCode, context.ScopeHandler, 0) 
                : new RuntimeContext(_proxy, byteCode);

            if (_nextAction == DebugAction.CancelExecution) return InterpreterResult.Finished;

            bool isFirst = true;

            while (c.Counter < byteCode.Instructions.Count)
            {
                var instruction = byteCode.Instructions[c.Counter];

                if (instruction is Bytecode.Debug debug && (!isFirst || _isActionDefault))
                { 
                    if (debug.Always && _nextAction != DebugAction.CancelExecution && _nextAction != DebugAction.StopDebugging)
                    {
                        return new InterpreterResult(new Debug.Breakpoint(debug.Debuggeable.DebuggeableBinding, debug.Debuggeable), new InterpreterSaveState(c, byteCode));
                    }
                    else if (_nextAction == DebugAction.Continue)
                    { 
                        var breakpointHit = TryBreakpointHit(debug.Debuggeable);

                        if (breakpointHit != null)
                        {
                            return new InterpreterResult(breakpointHit, new InterpreterSaveState(c, byteCode));
                        }
                    }
                    else if (_nextAction == DebugAction.StepOver)
                    {
                        return new InterpreterResult(new Debug.Breakpoint(new SourceCodeRange(
                                new SourceCodeLocation(debug.Debuggeable.DebuggeableBinding.FromLine, debug.Debuggeable.DebuggeableBinding.FromPosition),
                                new SourceCodeLocation(debug.Debuggeable.DebuggeableBinding.ToLine, debug.Debuggeable.DebuggeableBinding.ToPosition)),
                                debug.Debuggeable),
                                new InterpreterSaveState(c, byteCode));
                    }
                }

                isFirst = false;

                int pc = c.Counter;
                instruction.Execute(c);

                // If no jump was executed, then increment the counter
                if (pc == c.Counter)
                {
                    c.moveCounter(1);
                }
            }

            return InterpreterResult.Finished;
        }

        private Debug.Breakpoint TryBreakpointHit(IDebuggeable debuggeable)
        {
            if (IsDebuggerAttached() && debuggeable != null)
            {
                return Debugger.GetBreakpointAtElement(debuggeable);
            }

            return null;
        }

        public InterpreterResult Interpret(Program program)
        {
            bool globalTtsAbort = Options?.EmitGlobalTtsAbort ?? false;

            if (globalTtsAbort)
                _proxy.Data.TtsBegin();

            InterpreterResult ret = null;

            try
            {
                ret = Interpret(Compile(program));
            }
            finally
            {
                if (ret?.HasFinished ?? true && globalTtsAbort)
                    _proxy.Data.TtsAbort();
            }

            return ret;
        }

        public ByteCode Compile(Program program)
        {
            return new ByteCodeGenerator(Options).Generate(program, IsDebuggerAttached());
        }

        private bool IsDebuggerAttached()
        {
            return Debugger != null;
        }
    }
}
