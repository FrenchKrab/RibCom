using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace RibCom.TerribleClients
{
    /// <summary>
    /// A queue based asynchronous client. Every message is added to a queue.
    /// </summary>
    public class QueueClient : AsyncClient
    {
        public class Message
        {
            public Message(byte[] message)
            {
                this.Content = message;
            }

            public byte[] Content;
        }

        public QueueClient(Socket socket, int id = DefaultId) : base(socket, id)
        { }

        public ConcurrentQueue<Message> receivedMessages = new ConcurrentQueue<Message>();

        protected override void OnMessageReceived(byte[] contentBytes)
        {
            receivedMessages.Enqueue(new Message(contentBytes));
        }

        protected override void OnSocketDied()
        {
            throw new NotImplementedException();
        }

        protected override void OnSaveBufferFull()
        {
            throw new NotImplementedException();
        }
    }
}
