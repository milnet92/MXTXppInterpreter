using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Parser.Metadata
{
    public class Completion
    {
        public string Value { get; }
        public string Caption { get; }
        public CompletionEntryType EntryType { get; }
        public string DocHtml { get; set; }
        
        public Completion(string value, string caption, CompletionEntryType entryType)
        {
            Value = value;
            Caption = caption;
            EntryType = entryType;
        }
    }

    public enum CompletionEntryType
    {
        TableField,
        ClassProperty,
        Method,
        StaticMethod,
        EnumValue,
        TableIndex,
        Class,
        Namespace
    }
}
