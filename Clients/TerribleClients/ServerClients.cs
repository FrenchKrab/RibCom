using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Google.Protobuf;


namespace RibCom.TerribleClients
{

    /// <summary>
    /// Contains every connected client
    /// </summary>
    public class ServerClients
    {
        public delegate void ClientActionHandler(int clientId);
        public delegate void MessageReceivedHandler(CallbackClient client, byte[] packet);

        /// <summary>
        /// Raised when a message is received from a client
        /// </summary>
        public event MessageReceivedHandler MessageReceived;

        public event ClientActionHandler ClientDied;


        public int ClientCount { get { return _clients.Count; } }

        /// <summary>
        /// Keeps track of the last client ID to generate the next one
        /// (kinda dirty, TODO: change that someday, maybe a separate class for ID generation)
        /// </summary>
        private int lastClientId = 0;

        //tbh i can't remember why it's there, probably gonna delete this someday
        //private ManualResetEvent clientIdGenerationEnded = new ManualResetEvent(true);


        /// <summary>
        /// Dictionary where key is client ID and value is client.
        /// </summary>
        private ConcurrentDictionary<int, CallbackClient> _clients = new ConcurrentDictionary<int, CallbackClient>();




        /// <summary>
        /// Create a new client with a given socket
        /// </summary>
        /// <param name="socket"></param>
        public void CreateClient(Socket socket)
        {
            CallbackClient client = new CallbackClient(socket, GetNewId());
            _clients.TryAdd(client.Id, client);
            client.PacketReceived += (packet) => MessageReceivedFromClient(client, packet);
            client.SocketDied += () => RemoveClient(client);
            Console.WriteLine("Created client #" + client.Id);
            client.StartReceiving();
        }


        /// <summary>
        /// Retrieve a client from its ID
        /// </summary>
        /// <param name="id">The client's ID</param>
        /// <returns></returns>
        public CallbackClient GetClient(int id)
        {
            if(_clients.TryGetValue(id, out CallbackClient callbackClient))
            {
                return callbackClient;
            }
            else
            {
                return null;
            }
        }

        public void BroadcastMessage(IMessage msg)
        {
            foreach (Client c in _clients.Values)
            {
                Console.WriteLine($"sent {msg} to {c.Id} (size {msg.CalculateSize()})");
                c.SendMessage(msg);
            }
        }




        /// <summary>
        /// Function called by clients when a packet is received from them
        /// </summary>
        /// <param name="client"></param>
        /// <param name="type"></param>
        /// <param name="packet"></param>
        private void MessageReceivedFromClient(CallbackClient client, byte[] packet)
        {
            MessageReceived?.Invoke(client, packet);
        }

        /// <summary>
        /// Get a new unique (lol it isn't atm) ID for a client
        /// </summary>
        /// <returns></returns>
        private int GetNewId()
        {
            lastClientId = (lastClientId+1)%(int.MaxValue-1);
            return lastClientId;
        }


        /// <summary>
        /// Removes a client and dispose of it
        /// </summary>
        /// <param name="client">The client to remove</param>
        private void RemoveClient(CallbackClient client)
        {
            client.PacketReceived -= (packet) => MessageReceivedFromClient(client, packet);
            client.SocketDied -= () => RemoveClient(client);

            int id = client.Id;

            CallbackClient c = null;
            while(c==null && _clients.ContainsKey(id))
            {
                if (_clients.TryRemove(id, out c))
                {
                    Console.WriteLine($"Removed client #{id} ({_clients.Count} clients left)");
                }
                else
                {
                    c = null;
                }
            }
            client.Dispose();
            ClientDied?.Invoke(id);
        }
    }
}
