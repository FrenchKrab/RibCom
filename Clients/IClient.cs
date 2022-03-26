using System.Collections.Concurrent;

namespace RibCom
{
    public interface IClient
    {
        ConcurrentQueue<RibCom.Message> MessageQueue { get; }
        IPeer Peer { get; }

        void Connect(string address, int port);
        void Disconnect();
        void StartListening();
    }
}
