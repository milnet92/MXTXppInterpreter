using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Interpreter.Bytecode;

namespace XppInterpreter.Core.Events
{
    public static class EventHandlerStore
    {
        private static readonly ConcurrentDictionary<string, EventHandler> _store = new ConcurrentDictionary<string, EventHandler>();
        public static EventHandler Get(string key)
        {
            _store.TryGetValue(key, out EventHandler handler);
            return handler;
        }

        public static void Set(string key, EventHandler handler)
        {
            _store[key] = handler;
        }

        public static EventHandler Remove(string key)
        {
            _store.TryRemove(key, out EventHandler handler);
            return handler;
        }
    }
}
