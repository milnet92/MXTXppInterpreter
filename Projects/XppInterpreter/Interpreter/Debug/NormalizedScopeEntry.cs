using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Debug
{
    /// <summary>
    /// Stores the data for a scope variable at run-time with information
    /// related to what actions can be taken to that variable
    /// </summary>
    public class NormalizedScopeEntry
    {
        /// <summary>
        /// Base types that can be edited
        /// </summary>
        private readonly static Type[] _editableTypes = new Type[] {
            typeof(int), 
            typeof(string), 
            typeof(long), 
            typeof(decimal),
            typeof(bool)};

        public string TypeName { get; set; }
        public string VariableName { get; set; }
        public object Value { get; set; }
        public string[] EnumValues { get; set; }
        public bool IsExpandable { get; set; }
        public bool Changed { get; set; }

        /// <summary>
        /// If the entry can be edited by the client
        /// </summary>
        public bool Editable
        {
            get
            {
                return _editableTypes.Any(t => t.ToString() == TypeName);
            }
        }
    }
}
