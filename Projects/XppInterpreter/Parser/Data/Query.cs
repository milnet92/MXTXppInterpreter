using System.Collections.Generic;

namespace XppInterpreter.Parser.Data
{
    public class Query
    {
        public bool ForceSelectOrder { get; set; }
        public string Index { get; set; }
        public bool GenerateOnly { get; set; }
        public bool FirstFast { get; set; }
        public bool FirstOnly { get; set; }
        public bool FirstOnly10 { get; set; }
        public bool FirstOnly100 { get; set; }
        public bool FirstOnly1000 { get; set; }
        public bool ForUpdate { get; set; }
        public bool ForceLiterals { get; set; }
        public bool ForcePlaceHolders { get; set; }
        public bool ForceNetsedLoops { get; set; }
        public bool NoFetch { get; set; }
        public bool OptimisticLock { get; set; }
        public bool PessimisticLock { get; set; }
        public bool Reverse { get; set; }
        public bool RepeatableRead { get; set; }
        public ValidTimeState ValidTimeState { get; set; }
        public CrossCompany CrossCompany { get; set; }
        public string TableVariableName { get; }
        public List<SelectionField> SelectionFields { get; set; }
        public List<Field> GroupFields { get; set; }
        public List<OrderByField> OrderFields { get; set; }
        public Where Where { get; set; }
        public Join Join { get; set; }
        public Query(string tableVariableName)
        {
            TableVariableName = tableVariableName;
        }
    }
}
