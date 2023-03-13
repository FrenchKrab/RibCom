using ENet;

namespace RibCom.Enet
{
    internal class EnetPeer : IPeer
    {
        protected readonly Peer _peer;

        public EnetPeer(Peer peer)
        {
            _peer = peer;
        }

        public void Send(byte[] data, PeerSendMode sendMode)
        {
            Send(data, sendMode, 0);
        }

        public void Send(byte[] data, PeerSendMode sendMode, byte channel)
        {
            Packet packet = new Packet();
            packet.Create(data, PeerSendModeToPacketFlags(sendMode));
            _peer.Send(channel, ref packet);
        }

        public void Timeout(uint timeoutLimit, uint timeoutMinimum, uint timeoutMaximum) => _peer.Timeout(timeoutLimit, timeoutMinimum, timeoutMaximum);

        private PacketFlags PeerSendModeToPacketFlags(PeerSendMode mode)
        {
            switch (mode)
            {
                case PeerSendMode.Reliable:
                    return PacketFlags.Reliable;
                case PeerSendMode.Unreliable:
                    return PacketFlags.None;
                case PeerSendMode.UnreliableUnsequenced:
                    return PacketFlags.Unsequenced;
                case PeerSendMode.Instant:
                    return PacketFlags.Reliable | PacketFlags.Instant;
                default:
                    return PacketFlags.Reliable;
            }
        }
    }
}
