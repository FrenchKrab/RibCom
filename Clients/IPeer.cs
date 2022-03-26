namespace RibCom
{
    public interface IPeer
    {
        void Send(byte[] data, PeerSendMode sendMode);
        void Send(byte[] data, PeerSendMode sendMode, byte channel);
    }
}
