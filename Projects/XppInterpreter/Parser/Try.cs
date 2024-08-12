using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter;

namespace XppInterpreter.Parser
{
    public class Try : Statement
    {
        public Block TryBlock { get; }
        public List<Catch> Catches { get; }
        public Block Finally { get; }
        public bool HasFinally => Finally != null;

        public Try(Block tryBlock, SourceCodeBinding sourceCodeBinding, List<Catch> catches = null, Block finallyBlock = null)
            : base(sourceCodeBinding, null)
        {
            TryBlock = tryBlock;
            Catches = catches ?? new List<Catch>();
            Finally = finallyBlock;
        }

        public override void Accept(IAstVisitor interpreter)
        {
            interpreter.VisitTry(this);
        }
    }

    public static class CatchExceptionTypeHelper
    {
        public const string EXCEPTION_ENUM = "Exception";
        public static readonly string[] EXCEPTION_MEMBERS = new string[] 
           { "Break", "CLRError", "CodeAccessSecurity", "DDEerror", "Deadlock", "DuplicateKeyException", "DuplicateKeyExceptionNotRecovered",
            "Error", "Internal", "Error", "Info", "Internal", "Numeric", "Sequence", "UpdateConflict", "UpdateConflictNotRecovered", "Warning", 
            "TransientSqlConnectionError", "Timeout" };

        public static bool IsExceptionMember(string member)
        {
            return EXCEPTION_MEMBERS.Contains(member, StringComparer.InvariantCultureIgnoreCase);
        }

        public static bool IsExceptionEnum(string enumName)
        {
            return enumName.ToLowerInvariant() == EXCEPTION_ENUM.ToLowerInvariant();
        }
    }

    public class Catch
    {
        public string ExceptionMember { get; }
        public Block Block { get; }

        public Catch(string exceptionMember, Block block)
        {
            ExceptionMember = exceptionMember;
            Block = block;
        }

        public Catch(Block block)
        {
            Block = block;
        }
    }
}
