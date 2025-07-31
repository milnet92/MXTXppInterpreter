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
    public static class EventHandlerSubscriptionHelper
    {
        public static bool IsSubscribed(EventHandler handler)
        {
            if (handler.Delegate == null) return false;

            MulticastDelegate multicastDelegate = (MulticastDelegate)handler.GetField().GetValue(null);

            if (multicastDelegate is null) return false;

            return multicastDelegate.GetInvocationList().Contains(handler.Delegate);
        }

        public static void Subscribe(EventHandler handler)
        {
            Unsubscribe(handler);

            FieldInfo field = handler.GetField();
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
                FieldInfo field = eventHandler.GetField();
                MulticastDelegate multicastDelegate = (MulticastDelegate)field.GetValue(null);

                if (multicastDelegate is null) return;

                Delegate combined = Delegate.Remove(multicastDelegate, handler.Delegate);
                field.SetValue(null, combined);
            }
        }
    }
}
