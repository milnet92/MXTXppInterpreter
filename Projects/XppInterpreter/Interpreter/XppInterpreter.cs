using XppInterpreter.Core;
using XppInterpreter.Interpreter.Bytecode;
using XppInterpreter.Interpreter.Debug;
using XppInterpreter.Interpreter.Proxy;
using XppInterpreter.Parser;

namespace XppInterpreter.Interpreter
{
    public class XppInterpreter
    {
        private readonly XppProxy _proxy;
        private DebugAction _nextAction = DebugAction.Continue;
        private bool _isActionDefault = true;

        public IDebugger Debugger { get; set; }
        public XppInterpreterOptions Options { get; }
        public XppInterpreter(XppProxy proxy, XppInterpreterOptions options = null)
        {
            _proxy = proxy;
            Options = options;
        }

        public InterpreterResult Continue(ByteCode byteCode, RuntimeContext context, DebugAction debugAction)
        {
            _isActionDefault = false;
            _nextAction = debugAction;

            bool globalTtsAbort = Options?.EmitGlobalTtsAbort ?? false;

            InterpreterResult ret = null;
            try
            {
                ret = Interpret(byteCode, context);
            }
            finally
            {
                if (ret?.HasFinished ?? true && globalTtsAbort)
                {
                    _proxy.Data.TtsAbort();
                }
            }

            return ret;
        }

        public InterpreterResult Continue(InterpreterResult interpreterResult, DebugAction debugAction)
        {
            return Continue(interpreterResult.SaveState.ByteCode, interpreterResult.SaveState.Context, debugAction);
        }

        public InterpreterResult Interpret(ByteCode byteCode, RuntimeContext context = null, bool reuseCounter = true, DebugAction nextAction = DebugAction.None)
        {
            if (nextAction != DebugAction.None)
            {
                _nextAction = nextAction;
            }

            RuntimeContext c = context != null ?
                reuseCounter ? context : new RuntimeContext(context.Proxy, byteCode, context.ScopeHandler)
                : new RuntimeContext(_proxy, byteCode);

            c.NextAction = _nextAction;
            c.InnerContext = context?.InnerContext;
            c.Interpreter = this;

            if (_nextAction == DebugAction.CancelExecution)
            {
                return InterpreterResult.Finished;
            }

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

                try
                { 
                    instruction.Execute(c);
                }
                catch (System.Exception ex)
                {
                    if (c.ScopeHandler.AreExceptionsHandled)
                    {
                        c.ScopeHandler.CurrentExceptionHandler.HandleException(ex, c);
                    }
                    else
                    {
                        throw ex;
                    }
                }

                if (c.Returned)
                {
                    c.InnerContext = null;

                    if (context != null)
                    {
                        context.Returned = true;
                        context.InnerContext = null;
                    }

                    break;
                }

                if (instruction is IInterpretableInstruction interpretable)
                {
                    if (!interpretable.LastResult?.HasFinished ?? false)
                    {
                        return new InterpreterResult(interpretable.LastResult.Breakpoint, new InterpreterSaveState(c, byteCode));
                    }
                    else
                    {
                        c.InnerContext = null;

                        if (context != null)
                        {
                            context.InnerContext = null;
                        }
                    }
                }

                // If no jump was executed, then increment the counter
                if (pc == c.Counter)
                {
                    c.MoveCounter(1);
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

        public InterpreterResult Interpret(Program program, XppInterpreterDependencyCollection dependencies = null)
        {
            bool globalTtsAbort = Options?.EmitGlobalTtsAbort ?? false;

            if (globalTtsAbort)
            {
                _proxy.Data.TtsBegin();
            }

            InterpreterResult ret = null;

            try
            {
                ret = Interpret(Compile(program, dependencies));
            }
            finally
            {
                if (ret?.HasFinished ?? true && globalTtsAbort)
                {
                    _proxy.Data.TtsAbort();
                }
            }

            return ret;
        }

        public ByteCode Compile(Program program, XppInterpreterDependencyCollection dependencies = null)
        {
            ByteCode fullByteCode;

            // Compile dependencies first
            if (dependencies != null)
            {
                fullByteCode = CompileDependencies(dependencies);
            }
            else
            {
                fullByteCode = new ByteCode();
            }

            ByteCode compiledProgram = new ByteCodeGenerator(Options).Generate(program, IsDebuggerAttached(), fullByteCode.DeclaredFunctions);

            // We don't add declared functions as the bytecode generation
            // for the full program already include the ref functions
            fullByteCode.Instructions.AddRange(compiledProgram.Instructions);

            foreach (var instruction in fullByteCode.Instructions)
            {
                System.Console.WriteLine(instruction.OperationCode);
            }

            return fullByteCode;
        }

        private ByteCode CompileDependencies(XppInterpreterDependencyCollection dependencies)
        {
            ByteCode byteCode = new ByteCode();

            foreach (var dependency in dependencies)
            {
                var compiledDependency = new ByteCodeGenerator(Options).Generate(dependency, false , byteCode.DeclaredFunctions);

                // Merge dependencies
                byteCode.Instructions.AddRange(compiledDependency.Instructions);

                // Compiled bytecode contains aggregated function references
                if (compiledDependency.DeclaredFunctions != null)
                {
                    byteCode.DeclaredFunctions = compiledDependency.DeclaredFunctions;
                }
            }

            return byteCode;
        }

        private bool IsDebuggerAttached()
        {
            return Debugger != null;
        }
    }
}
