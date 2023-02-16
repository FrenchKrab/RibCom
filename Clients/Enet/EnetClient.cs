

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ENet;

namespace RibCom.Enet
{
    public class EnetClient : IClient, IDisposable
    {
        public ConcurrentQueue<RibCom.Message> MessageQueue { get; } = new ConcurrentQueue<Message>();
        public IPeer Peer => new EnetPeer(_peer);

        private readonly Host _host = new Host();
        private Peer _peer;
        private Task _listeningTask;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private CancellationToken _token;

        private volatile bool _listening = false;


        public EnetClient()
        {
            _host.Create();
            _token = _tokenSource.Token;
        }


        public void Connect(string address, int port)
        {
            Connect(address, (ushort)port);
        }

        private void Connect(string address, ushort port)
        {
            Address a = new Address();
            a.SetHost(address);
            a.Port = port;

            _peer = _host.Connect(a);
        }

        public void Disconnect()
        {
            _peer.DisconnectNow(0);
        }

        public void StartListening()
        {
            if (_listening)
                return;

            _listeningTask = Task.Factory.StartNew(() => ListeningLoop(), TaskCreationOptions.LongRunning);
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
                        Console.WriteLine("Packet received from server - Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);
                        m.Type = MessageContentType.Data;
                        m.Data = new byte[netEvent.Packet.Length];
                        netEvent.Packet.CopyTo(m.Data);
                        netEvent.Packet.Dispose();
                    }
                    else if (netEvent.Type == EventType.Connect)
                    {
                        Console.WriteLine("Client connected to server");
                        m.Type = MessageContentType.Connected;
                    }
                    else if (netEvent.Type == EventType.Disconnect)
                    {
                        Console.WriteLine("Client disconnected from server");
                        m.Type = MessageContentType.Disconnected;
                    }
                    else if (netEvent.Type == EventType.Timeout)
                    {
                        Console.WriteLine("Client connection timeout");
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
