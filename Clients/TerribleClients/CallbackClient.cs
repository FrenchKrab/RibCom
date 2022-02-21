using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RibCom.TerribleClients
{
    /// <summary>
    /// A callback based asynchronous client. Raise event on every message received
    /// </summary>
    public class CallbackClient : AsyncClient
    {
        public CallbackClient(Socket socket, int id = DefaultId) : base(socket, id)
        {
            State.SaveBufferFull += OnSaveBufferFull;
        }
        
        public delegate void PacketReceivedHandler(byte[] contentBytes);
        public event PacketReceivedHandler PacketReceived;
        public event Action SocketDied;


        protected override void OnMessageReceived(byte[] contentBytes)
        {
            PacketReceived?.Invoke(contentBytes);
        }

        protected override void OnSocketDied()
        {
            SocketDied?.Invoke();
        }

        protected override void OnSaveBufferFull()
        {
            Console.WriteLine("----FULL-----"); 
            KillClient();
        }
    }
}
