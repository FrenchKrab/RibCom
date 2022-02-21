using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace RibCom.Tools
{
    public static class BareboneMessageMaker
    {
        public const int BYTE_COUNT_MESSAGE_LENGTH = 3;


        public static byte[] TryReadMessage(byte[] bytes, out int messageSize)
        {
            messageSize = 0;
            if (bytes.Length < BYTE_COUNT_MESSAGE_LENGTH)
            {
                return null; //Not enough bytes
            }

            //Get message content length
            //Expand byte array for same reason
            byte[] contentLengthBytes = ArrayTools.ExpandByteArray(bytes.SubArray(0, BYTE_COUNT_MESSAGE_LENGTH), 4);
            ArrayTools.LittleEndianToLocalEndian(contentLengthBytes);
            uint contentLength = BitConverter.ToUInt32(contentLengthBytes, 0);

            int totalLength = BYTE_COUNT_MESSAGE_LENGTH + (int)contentLength;
            if (bytes.Length >= totalLength)
            {
                messageSize = totalLength;

                return bytes.SubArray(BYTE_COUNT_MESSAGE_LENGTH, (int)contentLength);
            }

            return null;
        }


        /// <summary>
        /// Creates a byte array message for sending purpose. (has a type and length header)
        /// </summary>
        /// <param name="messageType">Type of the message to send</param>
        /// <param name="message">Message content</param>
        /// <returns></returns>
        public static byte[] CreateMessage(IMessage message)
        {
            byte[] contentBytes = message.ToByteArray();
            byte[] sizeBytes = BitConverter.GetBytes(contentBytes.Length);

            ArrayTools.LocalEndianToLittleEndian(contentBytes);
            ArrayTools.LocalEndianToLittleEndian(sizeBytes);


            byte[] output = new byte[BYTE_COUNT_MESSAGE_LENGTH + contentBytes.Length];
            //Console.WriteLine("Type " + messageType + " | sizebytes :" + contentBytes.Length);
            Buffer.BlockCopy(sizeBytes, 0, output, 0, BYTE_COUNT_MESSAGE_LENGTH);
            Buffer.BlockCopy(contentBytes, 0, output, BYTE_COUNT_MESSAGE_LENGTH, contentBytes.Length);

            return output;
        }

    }
}
