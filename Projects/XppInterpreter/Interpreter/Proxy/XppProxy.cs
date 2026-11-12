using System;
using System.Collections.Concurrent;

namespace XppInterpreter.Interpreter.Proxy
{
    public class XppProxy
    {
        public readonly IIntrinsicFunctionProvider Intrinsic;
        public readonly IXppBinaryOperationProxy Binary;
        public readonly IXppCastingProxy Casting;
        public readonly IXppReflectionProxy Reflection;
        public readonly IXppUnaryOperationProxy Unary;
        public readonly IXppDataAccessProxy Data;
        public readonly IXppExceptionsProxy Exceptions;
        public readonly IXppQueryGenerationProxy QueryGeneration;

        public readonly Core.Clr.ClrTypeResolver ClrTypes;

        // What the metadata says about a type. It doesn't change while the process runs, a script asks on every
        // operation, and each answer is a call to X++ that looks the type up in the data dictionary.
        private readonly ConcurrentDictionary<Type, bool> _xppTypes = new ConcurrentDictionary<Type, bool>();
        private readonly ConcurrentDictionary<Type, bool> _commonTypes = new ConcurrentDictionary<Type, bool>();
        private readonly ConcurrentDictionary<string, bool> _enums = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, bool> _stringEdts = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        public XppProxy(
            IIntrinsicFunctionProvider intrinsic,
            IXppBinaryOperationProxy binary,
            IXppCastingProxy casting,
            IXppUnaryOperationProxy unary,
            IXppDataAccessProxy data,
            IXppReflectionProxy reflection,
            IXppExceptionsProxy exceptions,
            IXppQueryGenerationProxy queryGeneration)
        {
            Intrinsic = intrinsic;
            Binary = binary;
            Casting = casting;
            Unary = unary;
            Data = data;
            Reflection = reflection;
            Exceptions = exceptions;
            QueryGeneration = queryGeneration;
            ClrTypes = new Core.Clr.ClrTypeResolver(reflection.GetReferencedAssemblies);
        }

        /// <summary>
        /// <see cref="IXppReflectionProxy.IsXppType"/>, asked once for each type.
        /// </summary>
        public bool IsXppType(Type type)
        {
            if (type is null) return false;

            if (!_xppTypes.TryGetValue(type, out bool isXppType))
            {
                isXppType = Reflection.IsXppType(type);
                _xppTypes[type] = isXppType;
            }

            return isXppType;
        }

        /// <summary>
        /// <see cref="IXppReflectionProxy.IsCommonType"/>, asked once for each type.
        /// </summary>
        public bool IsCommonType(Type type)
        {
            if (type is null) return false;

            if (!_commonTypes.TryGetValue(type, out bool isCommonType))
            {
                isCommonType = Reflection.IsCommonType(type);
                _commonTypes[type] = isCommonType;
            }

            return isCommonType;
        }

        /// <summary>
        /// <see cref="IXppReflectionProxy.IsEnum"/>, asked once for each name.
        /// </summary>
        public bool IsEnum(string name)
        {
            if (string.IsNullOrEmpty(name)) return Reflection.IsEnum(name);

            if (!_enums.TryGetValue(name, out bool isEnum))
            {
                isEnum = Reflection.IsEnum(name);
                _enums[name] = isEnum;
            }

            return isEnum;
        }

        /// <summary>
        /// <see cref="IXppCastingProxy.IsStringEDT"/>, asked once for each type name.
        /// </summary>
        public bool IsStringEdt(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return false;

            if (!_stringEdts.TryGetValue(typeName, out bool isStringEdt))
            {
                isStringEdt = Casting.IsStringEDT(typeName);
                _stringEdts[typeName] = isStringEdt;
            }

            return isStringEdt;
        }
    }
}
