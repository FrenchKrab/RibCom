
namespace RibCom
{
	public struct Message
	{
		public MessageContentType Type;
		public uint Source;
		public byte[] Data;
		public uint Channel;
	}
}