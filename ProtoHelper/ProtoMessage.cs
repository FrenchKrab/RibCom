using Google.Protobuf.WellKnownTypes;

namespace RibCom.ProtoHelper
{
    /// <summary>
    /// Contains a network packet/message that has been translated to a protobuf message if applicable.
    /// </summary>
    public struct ProtoMessage
    {
        public MessageContentType Type;
        public uint Source;
        public Any Content;
        public uint Channel;
    }
}
