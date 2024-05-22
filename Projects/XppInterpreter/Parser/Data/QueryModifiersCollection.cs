using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Lexer;

namespace XppInterpreter.Parser.Data
{
    class QueryModifiersCollection
    {
        public ValidTimeState ValidTimeState { get; set; }
        public CrossCompany CrossCompany { get; set; }
        public List<TType> Flags { get;} = new List<TType>();

        public bool FirstFast => Flags.Contains(TType.FirstFast);
        public bool ForUpdate => Flags.Contains(TType.ForUpdate);
        public bool FirstOnly => Flags.Contains(TType.FirstOnly);
        public bool FirstOnly10 => Flags.Contains(TType.FirstOnly10);
        public bool FirstOnly100 => Flags.Contains(TType.FirstOnly100);
        public bool FirstOnly1000 => Flags.Contains(TType.FirstOnly1000);
        public bool ForceLiterals => Flags.Contains(TType.ForceLiterals);
        public bool ForceNestedLoops => Flags.Contains(TType.ForceNestedLoops);
        public bool ForcePlaceHolders => Flags.Contains(TType.ForcePlaceHolders);
        public bool Reverse => Flags.Contains(TType.Reverse);
        public bool RepeatableRead => Flags.Contains(TType.RepeatableRead);
        public bool OptimisticLock => Flags.Contains(TType.OptimisticLock);
        public bool PessimisticLock => Flags.Contains(TType.PessimisticLock);
        public bool GenerateOnly => Flags.Contains(TType.GenerateOnly);
        public bool NoFetch => Flags.Contains(TType.NoFetch);
        public bool ForceSelectOrder => Flags.Contains(TType.ForceSelectOrder);

        public void SetFlag(TType type, bool value = true)
        {
            if (Flags.Contains(type))
            {
                if (value)
                {
                    throw new Exception($"Query modified {type} was specified more than once.");
                }

                Flags.Remove(type);
            }
            else if (value)
            {
                Flags.Add(type);
            }
        }
    }
}
