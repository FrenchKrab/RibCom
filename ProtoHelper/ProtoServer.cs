
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using RibCom.Tools;

namespace RibCom.ProtoHelper
{
    /// <summary>
    /// Wrapper around an IServer to directly receive/link protobuf messages.
    /// </summary>
    public class ProtoServer
    {
        private IServer _server;
        private MessageSolver _solver;
        private TypeUrlCompression _urlCompressor;


        public ProtoServer(IServer server, MessageSolver messageSolver)
        {
            _server = server;
            _solver = messageSolver;
            _urlCompressor = new TypeUrlCompression(_solver);
        }

        public bool TryDequeue(out ProtoMessage message)
        {
            message = new ProtoMessage() { };
            if (_server.MessageQueue.TryDequeue(out Message m))
            {
                message.Channel = m.Channel;
                message.Type = m.Type;
                message.Source = m.Source;
                if (message.Type == MessageContentType.Data)
                {
                    message.Content = Google.Protobuf.WellKnownTypes.Any.Parser.ParseFrom(m.Data);
                    message.Content.TypeUrl = _urlCompressor.GetUncompressedTypeUrl(message.Content.TypeUrl);
                    Console.WriteLine("parsed to any : " + message.Content.ToString());
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Send(uint peerId, IMessage message, PeerSendMode sendMode, byte channel = 0)
        {
            Parallel.Invoke(() =>
            {
                if (_server.Peers.TryGetValue(peerId, out IPeer peer))
                {
                    string compressedTypeUrl = _urlCompressor.GetCompressedTypeUrl(message);
                    Any any = Any.Pack(message);
                    any.TypeUrl = compressedTypeUrl;
                    peer.Send(any.ToByteArray(), sendMode, channel);
                }
            });
        }

        public void Broadcast(IMessage message, PeerSendMode sendMode, byte channel = 0)
        {
            Parallel.Invoke(() =>
            {
                foreach (var peer in _server.Peers.Values)
                {
                    string compressedTypeUrl = _urlCompressor.GetCompressedTypeUrl(message);
                    Any any = Any.Pack(message);
                    any.TypeUrl = compressedTypeUrl;
                    peer.Send(any.ToByteArray(), sendMode, channel);
                }
            });
        }
    }
}
