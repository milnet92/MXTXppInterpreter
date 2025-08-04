using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using XppInterpreter.Parser;

namespace XppInterpreter.Core.Events
{
    public static class MethodEventHandlerHelper
    {
        public static FieldInfo GetField(EventHandler handler)
        {
            return handler.ClassType.GetField(handler.EventHandlerName, BindingFlags.Static | BindingFlags.Public);
        }

        public static bool IsSubscribed(EventHandler handler)
        {
            if (handler.Delegate == null) return false;

            MulticastDelegate multicastDelegate = (MulticastDelegate)GetField(handler).GetValue(null);

            if (multicastDelegate is null) return false;

            return multicastDelegate.GetInvocationList().Contains(handler.Delegate);
        }

        public static void Subscribe(EventHandler handler)
        {
            // Ensure the delegate is created
            handler.InitializeDelegate();

            Unsubscribe(handler);

            FieldInfo field = GetField(handler);
            MulticastDelegate multicastDelegate = (MulticastDelegate)field.GetValue(null);
            Delegate combined = Delegate.Combine(multicastDelegate, handler.Delegate);
            
            field.SetValue(null, combined);

            EventHandlerStore.Set(handler.Key, handler);
        }

        public static void Unsubscribe(EventHandler eventHandler)
        {
            EventHandler handler = EventHandlerStore.Remove(eventHandler.Key);

            if (handler != null && IsSubscribed(handler))
            {
                FieldInfo field = GetField(handler);
                MulticastDelegate multicastDelegate = (MulticastDelegate)field.GetValue(null);

                if (multicastDelegate is null) return;

                Delegate combined = Delegate.Remove(multicastDelegate, handler.Delegate);
                field.SetValue(null, combined);
            }
        }
    }
}
