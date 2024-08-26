using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XppInterpreter.Core
{
    public static class MessageProvider
    {
        private static CultureInfo _cultureInfo;
        public static CultureInfo CultureInfo
        {
            get => _cultureInfo;
            set
            {
                if (value is null) throw new ArgumentNullException(nameof(value));
                _cultureInfo = value;
            }
        }

        static MessageProvider()
        {
            CultureInfo = CultureInfo.InvariantCulture;
        }

        private static string GetString(string id)
        {
            return Resources.Strings.ResourceManager.GetString(id, _cultureInfo);
        }

        public static string ExceptionTypeCannotBeNull => GetString(nameof(ExceptionTypeCannotBeNull));
        public static string ExceptionVariableNotDeclared => GetString(nameof(ExceptionVariableNotDeclared));
        public static string ExceptionTokenWasNotExpected => GetString(nameof(ExceptionTokenWasNotExpected));
        public static string ExceptionDefaultSwitchStmt => GetString(nameof(ExceptionDefaultSwitchStmt));
        public static string ExceptionMultiDefaultSwitchStmt => GetString(nameof(ExceptionMultiDefaultSwitchStmt));
        public static string ExceptionTypeNotFound => GetString(nameof(ExceptionTypeNotFound));
        public static string ExceptionInvalidUsing => GetString(nameof(ExceptionInvalidUsing));
        public static string ExceptionInitializationUnknown => GetString(nameof(ExceptionInitializationUnknown));
        public static string ExceptionImplicitConversion => GetString(nameof(ExceptionImplicitConversion));
        public static string ExceptionLeaveFinally => GetString(nameof(ExceptionLeaveFinally));
        public static string ExceptionExpressionIDisposable => GetString(nameof(ExceptionExpressionIDisposable));
        public static string ExceptionReturnOutOfFunction => GetString(nameof(ExceptionReturnOutOfFunction));
        public static string ExceptionInvalidCatchExpr => GetString(nameof(ExceptionInvalidCatchExpr));
        public static string ExceptionNotExceptionMember => GetString(nameof(ExceptionNotExceptionMember));
        public static string ExceptionInvalidSyntax => GetString(nameof(ExceptionInvalidSyntax));
        public static string ExceptionInLikeNotInQuery => GetString(nameof(ExceptionInLikeNotInQuery));
        public static string ExceptionIsInQuery => GetString(nameof(ExceptionIsInQuery));
        public static string ExceptionInNotContainer => GetString(nameof(ExceptionInNotContainer));
        public static string ExceptionTokenExpected => GetString(nameof(ExceptionTokenExpected));
        public static string ExceptionInvalidExceptionEnum => GetString(nameof(ExceptionInvalidExceptionEnum));
        public static string ExceptionRetryNotInCatch => GetString(nameof(ExceptionRetryNotInCatch));
        public static string ExceptionRefTypeContainer => GetString(nameof(ExceptionRefTypeContainer));
    }
}
