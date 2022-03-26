using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Google.Protobuf;
using RibCom.Tools;

namespace RibCom.TerribleClients
{
    public abstract class Client : IDisposable
    {
        public class ConnectionState
        {
            /// <summary>
            /// Max size of the save buffer (TODO: do something about that absurdly large value lol)
            /// </summary>
            public const int MaxSaveBuffer = int.MaxValue;

            /// <summary>
            /// Size of the working buffer
            /// </summary>
            public const int WorkingBufferSize = 1024;


            /// <summary>
            /// Event raised when 
            /// </summary>
            public event Action SaveBufferFull;


            /// <summary>
            /// Buffer containing incomplete messages
            /// </summary>
            public byte[] SaveBuffer = new byte[0];

            /// <summary>
            /// Buffer used by the socket to write its content
            /// </summary>
            public byte[] WorkingBuffer = new byte[WorkingBufferSize];


            public void AppendWorkingBufferToSaveBuffer(int count)
            {
                bool full = false;
                //Create new buffer with enough space for ByteBuffer AND bytes
                int newSaveBufferSize = SaveBuffer.Length + count;
                if (newSaveBufferSize > MaxSaveBuffer)
                {
                    newSaveBufferSize = MaxSaveBuffer;
                    full = true;
                }
                byte[] newBuffer = new byte[newSaveBufferSize];
                //Copy SaveBuffer at start of newBuffer (index 0)
                Buffer.BlockCopy(SaveBuffer, 0, newBuffer, 0, SaveBuffer.Length);
                //"Append" bytes to newBuffer (index ByteBuffer.length)
                Buffer.BlockCopy(WorkingBuffer, 0, newBuffer, SaveBuffer.Length, Math.Min(count, newSaveBufferSize - SaveBuffer.Length));
                this.SaveBuffer = newBuffer;
                if (full)
                {
                    SaveBufferFull?.Invoke();
                }

            }

            /// <summary>
            /// Removes the specified first bytes from this ByteBuffer
            /// </summary>
            /// <param name="count"></param>
            public void RemoveSaveBufferFirstBytes(int count)
            {
                int newBufferSize = Math.Max(0, SaveBuffer.Length - count);
                byte[] newBuffer = new byte[newBufferSize];
                if (newBufferSize != 0)
                {
                    Buffer.BlockCopy(SaveBuffer, count, newBuffer, 0, newBufferSize);
                }
                this.SaveBuffer = newBuffer;
            }
        }



        public Client(Socket socket, int id = 0)
        {
            Id = id;
            Socket = socket;
            Dead = false;
        }

        public const int DefaultId = 0;

        /// <summary>
        /// This client's ID (should be unique)
        /// </summary>
        public int Id { get; protected set; }

        /// <summary>
        /// The socket this client is using
        /// </summary>
        public Socket Socket { get; protected set; }

        /// <summary>
        /// Is this client's socket dead
        /// </summary>
        public bool Dead { get; protected set; }



        /// <summary>
        /// Send a message to this client
        /// </summary>
        /// <param name="message">The message</param>
        public abstract void SendMessage(IMessage message);

        public abstract void Dispose();


    }
}
