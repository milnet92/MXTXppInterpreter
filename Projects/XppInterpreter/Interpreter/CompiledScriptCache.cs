using System;
using System.Collections.Concurrent;
using XppInterpreter.Interpreter.Proxy;

namespace XppInterpreter.Interpreter
{
    /// <summary>
    /// The compiled scripts of a host, by their source text: a text is parsed once, also when it has errors.
    /// </summary>
    public sealed class CompiledScriptCache
    {
        public const int DefaultCapacity = 500;

        private readonly ConcurrentDictionary<string, CompiledScript> _scripts = new ConcurrentDictionary<string, CompiledScript>(StringComparer.Ordinal);
        private readonly XppProxy _proxy;
        private readonly int _capacity;

        public int Count => _scripts.Count;

        public CompiledScriptCache(XppProxy proxy) : this(proxy, DefaultCapacity)
        {
        }

        public CompiledScriptCache(XppProxy proxy, int capacity)
        {
            _proxy = proxy;
            _capacity = Math.Max(capacity, 1);
        }

        public CompiledScript Get(string source)
        {
            source = source ?? string.Empty;

            if (_scripts.TryGetValue(source, out CompiledScript script))
            {
                return script;
            }

            script = new CompiledScript(source, _proxy);

            // A text that was edited stays here under its old version. Emptying a full cache costs one parse
            // for each script that is still in use, and keeps a host that generates its texts from filling the memory
            if (_scripts.Count >= _capacity)
            {
                _scripts.Clear();
            }

            return _scripts.GetOrAdd(source, script);
        }
    }
}
