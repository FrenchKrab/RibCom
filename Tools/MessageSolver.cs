using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace RibCom.Tools
{
    public class MessageSolver
    {
        private List<Type> _messageTypes = new List<Type>();
        private Dictionary<Type, MessageDescriptor> _messagesDescriptors
            = new Dictionary<Type, MessageDescriptor>();

        private readonly HashSet<Assembly> _scannedAssemblies = new HashSet<Assembly>();

        public MessageSolver()
        {
        }

        public IEnumerable<KeyValuePair<Type, MessageDescriptor>> GetTypeMessageDescriptorMapping()
        {
            return _messagesDescriptors;
        }

        public IEnumerable<Type> GetMessageTypes()
        {
            return _messageTypes;
        }

        public IEnumerable<MessageDescriptor> GetDescriptors()
        {
            return _messagesDescriptors.Values;
        }

        public bool TryGetMessageDescriptor(Type t, out MessageDescriptor descriptor)
        {
            return _messagesDescriptors.TryGetValue(t, out descriptor);
        }

        public void AddScannedAssembly(Type belongingType)
        {
            AddScannedAssembly(Assembly.GetAssembly(belongingType));
        }

        public void AddScannedAssembly(Assembly assembly)
        {
            if (!_scannedAssemblies.Contains(assembly))
            {
                _scannedAssemblies.Add(assembly);
                ScanAssembly(assembly);
            }
        }

        public void RefreshMessageTypesCache()
        {
            _messageTypes.Clear();
            _messagesDescriptors.Clear();
            foreach (var assembly in _scannedAssemblies)
            {
                ScanAssembly(assembly);
            }
        }

        private void ScanAssembly(Assembly assembly)
        {
            var foundTypes = assembly.GetTypes()
                .Where(t => t.GetInterfaces().Contains(typeof(IMessage))).ToArray();

            // very unsafe but good enough for the time being
            foreach (Type t in foundTypes)
            {
                foreach (var prop in t.GetProperties())
                {
                    if (prop.Name == "Descriptor" && prop.GetGetMethod().IsStatic)
                    {
                        _messageTypes.Add(t);
                        _messagesDescriptors[t] = (MessageDescriptor)prop.GetValue(null);
                    }
                }
            }
        }

        public Type ResolveType(Google.Protobuf.WellKnownTypes.Any message)
        {
            foreach (Type t in _messageTypes)
            {
                if (message.Is(_messagesDescriptors[t]))
                {
                    return t;
                }
            }
            return null;
        }
    }
}
