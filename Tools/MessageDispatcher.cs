using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace RibCom.Tools
{
    public class MessageDispatcher
    {

        private struct Subscribing
        {
            public Object Target;
            public MethodInfo Method;
        }

        private readonly MessageSolver _solver = new MessageSolver();

        private Dictionary<System.Type, List<Subscribing>> _subscribedMethods = new Dictionary<System.Type, List<Subscribing>>();

        public MessageDispatcher(MessageSolver solver)
        {
            _solver = solver;
        }


        public void RegisterListener(Object o)
        {

            var methods = o.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var m in methods)
            {
                Console.WriteLine("method: " + m);
                MessageListener a = m.GetCustomAttribute<MessageListener>();
                if (a == null)
                    continue;

                ParameterInfo[] pi = m.GetParameters();
                if (pi.Length >= 1)
                {
                    AddToSubcribed(pi[0].ParameterType, o, m);
                }
                // foreach (System.Type t in a.ListenedTypes)
                // {
                // 	Console.WriteLine($"ok {t}; {o}; {m}");
                // 	AddToSubcribed(t, o, m);
                // }
            }
        }

        public void InvokeMessageListeners(Any message, uint clientId = 0, bool async = true)
        {
            var type = _solver.ResolveType(message);

            if (type == null)
            {
                Console.WriteLine($"Unknown type: {message.TypeUrl}");
                return;
            }

            IMessage unpackedMessage = UnpackMessage(message, type);
            if (_subscribedMethods.TryGetValue(type, out List<Subscribing> subs))
            {
                foreach (var s in subs)
                {
                    try
                    {
                        object[] parameters = GetInvokeParameters(unpackedMessage, clientId, s.Method);
                        if (parameters != null)
                        {
                            if (async)
                                Task.Factory.StartNew(() => s.Method.Invoke(s.Target, parameters));
                            else
                                s.Method.Invoke(s.Target, parameters);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"! c{clientId}] error invoking {s.Method} : {e}");
                    }
                }
            }
        }

        protected virtual object[] GetInvokeParameters(IMessage message, uint clientId, MethodInfo method)
        {
            int paramCount = method.GetParameters().Length;
            if (paramCount == 1)
                return new object[] { message };
            else if (paramCount == 2)
                return new object[] { message, clientId };
            else
                return null;
        }

        public IMessage UnpackMessage(Any message)
        {
            var type = _solver.ResolveType(message);

            return UnpackMessage(message, type);
        }

        protected IMessage UnpackMessage(Any message, System.Type type)
        {
            IMessage unpackedMessage;

            if (type == null)
            {
                Console.WriteLine("Unresolvable type in msg: " + message);
                return null;
            }
            try
            {
                MethodInfo m = typeof(Any).GetMethod(nameof(Any.Unpack));
                MethodInfo mGeneric = m.MakeGenericMethod(type);
                unpackedMessage = (IMessage)mGeneric.Invoke(message, null);
            }
            catch (Exception e)
            {
                Console.WriteLine("! error invoking msg : " + e);
                return null;
            }
            return unpackedMessage;
        }

        private void AddToSubcribed(System.Type msgType, Object target, MethodInfo method)
        {
            if (_subscribedMethods.TryGetValue(msgType, out List<Subscribing> subcribed)) { }
            else
            {
                subcribed = new List<Subscribing>();
                _subscribedMethods[msgType] = subcribed;
            }

            var sub = new Subscribing
            {
                Method = method,
                Target = target
            };
            subcribed.Add(sub);
        }
    }
}
