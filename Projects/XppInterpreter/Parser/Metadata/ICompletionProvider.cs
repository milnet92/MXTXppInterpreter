using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Parser.Metadata
{
    public interface ICompletionProvider
    {
        CompletionCollection GetTableMethodCompletions(string tableName, bool isStatic);
        CompletionCollection GetTableFieldsCompletions(string tableName);
        CompletionCollection GetClassMethodCompletions(string className, bool isStatic);
        CompletionCollection GetClassFieldCompletions(string className, bool isStatic);
        CompletionCollection GetEnumCompletions(string enumName);
    }
}
