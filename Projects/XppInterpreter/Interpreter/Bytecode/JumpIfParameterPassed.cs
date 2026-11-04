namespace XppInterpreter.Interpreter.Bytecode
{
    class JumpIfParameterPassed : Jump
    {
        public override string OperationCode => $"JUMP_IF_PASSED {Name} {Offset}";
        public string Name { get; }

        public JumpIfParameterPassed(string name, int offset) : base(offset)
        {
            Name = name;
        }

        public override void Execute(RuntimeContext context)
        {
            if (context.ScopeHandler.CurrentScope.VariableCollection.Exists(Name))
            {
                base.Execute(context);
            }
        }
    }
}
