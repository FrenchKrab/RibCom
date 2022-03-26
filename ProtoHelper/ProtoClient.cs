
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
    /// Wrapper around an IClient to directly receive/link protobuf messages.
    /// </summary>
    public class ProtoClient
    {
        public readonly IClient Client;
        private MessageSolver _solver;
        private TypeUrlCompression _urlCompressor;


        public ProtoClient(IClient client, MessageSolver messageSolver)
        {
            Client = client;
            _solver = messageSolver;
            _urlCompressor = new TypeUrlCompression(_solver);
        }


        public bool TryDequeue(out ProtoMessage message)
        {
            message = new ProtoMessage() { };
            if (Client.MessageQueue.TryDequeue(out Message m))
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


        public void Send(IMessage message, PeerSendMode sendMode, byte channel = 0)
        {
            string compressedTypeUrl = _urlCompressor.GetCompressedTypeUrl(message);
            Any any = Any.Pack(message);
            any.TypeUrl = compressedTypeUrl;
            Client.Peer.Send(any.ToByteArray(), sendMode, channel);
        }

        public void Disconnect()
        {
            Client.Disconnect();
        }
    }
}
