using Google.Protobuf;
using RibCom.Tools;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace RibCom.TerribleClients
{

    /// <summary>
    /// Asynchronous client, does all the heavy work with messages and stuff. 
    /// Honestly doesn't make much sense to separate it from Client, but whatever, it works atm, might need 
    /// refactoring some day
    /// </summary>
    public abstract class AsyncClient : Client, IDisposable
    {
        /// <summary>
        /// Nice lil' class that sends messages for you.
        /// </summary>
        protected class MessageSender
        {
            public MessageSender(Socket socket)
            {
                this.socket = socket;
            }

            /// <summary>
            /// Event invoked when sending a packet returns an error
            /// </summary>
            public event Action FailedMessage;

            private readonly Socket socket;

            private readonly Queue<IMessage> messagesToSend = new Queue<IMessage>();
            private byte[] currentSendingBytes;
            private int sentByteCount = 0;

            /// <summary>
            /// Send a message to this client
            /// </summary>
            /// <param name="messageType">Type of the message to send</param>
            /// <param name="message">Message content</param>
            public void SendMessage(IMessage message)
            {
                messagesToSend.Enqueue(message);
                if (messagesToSend.Count == 1)
                {
                    BeginSendNextChunk();
                }
            }

            private void BeginSendNextChunk()
            {
                if (currentSendingBytes == null)
                    currentSendingBytes = BareboneMessageMaker.CreateMessage(messagesToSend.Peek());

                try
                {
                    socket.BeginSend(currentSendingBytes, sentByteCount, currentSendingBytes.Length - sentByteCount, 0, new AsyncCallback(BytesSentCallback), null);
                }
                catch(Exception e)
                {
                    FailedMessage?.Invoke();
                }

            }

            private void BytesSentCallback(IAsyncResult ar)
            {
                try
                {
                    int bytesSent = socket.EndSend(ar);
                    sentByteCount += bytesSent;
                    Console.WriteLine("Sent " + bytesSent + " bytes");
                    //If finished sending the packet
                    if (sentByteCount >= currentSendingBytes.Length)
                    {
                        messagesToSend.Dequeue();
                        sentByteCount = 0;
                        currentSendingBytes = null;
                        if (messagesToSend.Count != 0)
                        {
                            BeginSendNextChunk();
                        }
                    }
                    else
                    {
                        BeginSendNextChunk();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error sending packet chunk: " + e.ToString() + "\nRetrying ...");
                    BeginSendNextChunk();
                }
            }

        }

        public AsyncClient (Socket socket, int id = DefaultId) : base(socket, id)
        {
            State = new ConnectionState();
            messageSender = new MessageSender(socket);
            messageSender.FailedMessage += KillClient;
        }


        /// <summary>
        /// 
        /// </summary>
        protected MessageSender messageSender;
        protected ConnectionState State;

        /// <summary>
        /// Is a thread currently trying to make messages accessing the state ?
        /// </summary>
        private bool readingMessageFromState = false;

        #region Protected abstract methods

        /// <summary>
        /// Called when a message has been received.
        /// </summary>
        /// <param name="messageType">Type of the received message</param>
        /// <param name="contentBytes">content of the received message</param>
        protected abstract void OnMessageReceived(byte[] contentBytes);

        /// <summary>
        /// Called when the socket dies
        /// </summary>
        protected abstract void OnSocketDied();

        /// <summary>
        /// Called when the SaveBuffer is full
        /// </summary>
        protected abstract void OnSaveBufferFull();

        #endregion


        #region Socket reception

        public void StartReceiving()
        {
            BeginReceiveNext();
        }

        private void BeginReceiveNext()
        {
            if (!Dead)
                Socket.BeginReceive(State.WorkingBuffer, 0, ConnectionState.WorkingBufferSize, 0, new AsyncCallback(ReceivedCallback), null);
        }

        /// <summary>
        /// Callback called when data is received from the socket.
        /// Check for socket death, and if everything is alright, process the data received in the buffer.
        /// </summary>
        /// <param name="ar"></param>
        private void ReceivedCallback(IAsyncResult ar)
        {
            if(!Dead)
            {
                // Retrieve the state object and the handler socket
                // from the asynchronous state object.
                // Read data from the client socket.   
                try
                {
                    int receivedBytes = Socket.EndReceive(ar);
                    if (receivedBytes <= 0) //Error, kill the client
                    {
                        Console.WriteLine("0 bytes package :( " + Id);
                        KillClient();
                    }
                    else
                    {
                        //Console.WriteLine("[" + Id + "]RECV: received " + receivedBytes + " bytes. New save buffer : " + State.SaveBuffer.Length /* + BitConverter.ToString(State.SaveBuffer)*/);

                        //Append just received working buffer to the save buffer and try to make a message from the save buffer
                        State.AppendWorkingBufferToSaveBuffer(receivedBytes);
                        CreateMessagesFromSaveBuffer();


                        //Once all this is done, begin receiving the next
                        this.BeginReceiveNext();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("End received glitched client #" + Id + ". Error : " + e);
                    KillClient();
                }

            }
        }


        private void CreateMessagesFromSaveBuffer()
        {
            if (readingMessageFromState) //A thread is already reading packets from buffer
                return;

            bool cantCreate = false;
            readingMessageFromState = true;

            while (!cantCreate && !Dead)
            {
                byte[] createdPacket = BareboneMessageMaker.TryReadMessage(State.SaveBuffer, out int usedBytes);

                if (createdPacket != null)
                {
                    //Emit signal
                    Console.WriteLine("RECV: packet complete ! size: " + usedBytes + " bytes");

                    //Remove bytes used to create this packet
                    State.RemoveSaveBufferFirstBytes(usedBytes);
                    Console.WriteLine("new savebuffer : " /*+ State.SaveBuffer*/ + ", length" + State.SaveBuffer.Length);

                    OnMessageReceived(createdPacket);
                }
                else
                {
                    cantCreate = true;
                }
            }
            readingMessageFromState = false;
        }

        #endregion


        #region Message emission

        /// <summary>
        /// Send a message to this client
        /// </summary>
        /// <param name="messageType">Type of the message to send</param>
        /// <param name="message">Message content</param>
        public override void SendMessage(IMessage message)
        {
            // optimize Any message
            if (message is Google.Protobuf.WellKnownTypes.Any any)
            {
                any.TypeUrl = any.TypeUrl.Remove(0, "type.googleapis.com/".Length);
            }

            messageSender.SendMessage(message);
        }

        //TODO: remove this maybe
        private void FailedPacketHandler()
        {
            /*
            failedPackets++;
            if (failedPackets >= FailedPacketsDeath)
            {
                if (SocketDied != null)
                    SocketDied.Invoke();
            }*/
        }


        #endregion

        #region Socket life management

        public override void Dispose()
        {
            Socket.Close();
            Socket = null;

            State.WorkingBuffer = null;
            State = null;
        }

        protected void KillClient()
        {
            if (!Dead)
            {
                Dead = true;
                OnSocketDied();
            }
        }

        #endregion

    }
}
