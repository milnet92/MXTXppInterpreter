namespace XppInterpreter.Parser
{
    public abstract class Loop : Statement
    {
        public Block Block { get; }
        public Loop(Block block, SourceCodeBinding sourceCodeBinding, SourceCodeBinding debuggeableBinding) : base(sourceCodeBinding, debuggeableBinding)
        {
            Block = block;
        }
    }
}
