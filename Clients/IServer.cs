using System.Collections.Concurrent;

namespace RibCom
{
    public interface IServer
    {
        ConcurrentQueue<RibCom.Message> MessageQueue { get; }
        ConcurrentDictionary<uint, IPeer> Peers { get; }


        void StartListening();
        void Broadcast(byte[] data, PeerSendMode sendMode);
        void Broadcast(byte[] data, PeerSendMode sendMode, byte channel);
    }
}
