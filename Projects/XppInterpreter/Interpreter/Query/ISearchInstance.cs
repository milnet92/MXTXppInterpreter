using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Interpreter.Query
{
    public interface ISearchInstance
    {
        bool Next();
        void SetSearchObjects(object _searchObject, object _searchStatement);
        bool IsNextExecuted();
    }
}
