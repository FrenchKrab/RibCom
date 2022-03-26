

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;
using ENet;

namespace RibCom.Enet
{
    public class EnetServer : IServer, IDisposable
    {
        public ConcurrentQueue<RibCom.Message> MessageQueue { get; } = new ConcurrentQueue<Message>();
        public ConcurrentDictionary<uint, IPeer> Peers { get; } = new ConcurrentDictionary<uint, IPeer>();

        public ConcurrentQueue<uint> ConnectedPeersEvents { get; } = new ConcurrentQueue<uint>();
        public ConcurrentQueue<uint> DisconnectedPeersEvents { get; } = new ConcurrentQueue<uint>();


        private Host _host = new Host();
        private Task _listeningTask;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private CancellationToken _token;

        private volatile bool _listening = false;


        public EnetServer(string address, ushort port, int maxClients)
        {
            Address a = new Address();
            a.SetHost(address);
            a.Port = port;

            _host.Create(a, maxClients);
            _token = _tokenSource.Token;
        }


        public void StartListening()
        {
            if (_listening)
                return;

            _listeningTask = Task.Factory.StartNew(() => ListeningLoop(), TaskCreationOptions.LongRunning);
        }

        public void Broadcast(byte[] data, PeerSendMode sendMode)
        {
            Broadcast(data, sendMode, 0);
        }

        public void Broadcast(byte[] data, PeerSendMode sendMode, byte channel)
        {
            foreach (var p in Peers.Values)
            {
                p.Send(data, sendMode, channel);
            }
        }


        private void ListeningLoop()
        {
            _listening = true;
            Event netEvent;

            while (_listening && !_token.IsCancellationRequested)
            {
                bool polled = false;

                while (!polled)
                {
                    if (_host.CheckEvents(out netEvent) <= 0)
                    {
                        if (_host.Service(15, out netEvent) <= 0)
                            break;

                        polled = true;
                    }

                    RibCom.Message m = new Message()
                    {
                        Type = MessageContentType.None,
                        Source = netEvent.Peer.ID,
                        Channel = netEvent.ChannelID
                    };
                    if (netEvent.Type == EventType.Receive)
                    {
                        Console.WriteLine("Packet received from - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP + ", Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);
                        m.Type = MessageContentType.Data;
                        m.Data = new byte[netEvent.Packet.Length];
                        netEvent.Packet.CopyTo(m.Data);
                        netEvent.Packet.Dispose();
                    }
                    else if (netEvent.Type == EventType.Connect)
                    {
                        Console.WriteLine("Client connected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                        Peers.TryAdd(netEvent.Peer.ID, new EnetPeer(netEvent.Peer));
                        m.Type = MessageContentType.Connected;
                    }
                    else if (netEvent.Type == EventType.Disconnect)
                    {
                        Console.WriteLine("Client disconnected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                        Peers.TryRemove(netEvent.Peer.ID, out _);
                        m.Type = MessageContentType.Disconnected;
                    }
                    else if (netEvent.Type == EventType.Timeout)
                    {
                        Console.WriteLine("Client timeout - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                        Peers.TryRemove(netEvent.Peer.ID, out _);
                        m.Type = MessageContentType.Timeout;
                    }

                    if (m.Type != MessageContentType.None)
                        MessageQueue.Enqueue(m);
                }
            }

            _host.Flush();
        }

        public void Dispose()
        {
            _tokenSource.Cancel();
            _host.Flush();
            _host.Dispose();
        }
    }
}
