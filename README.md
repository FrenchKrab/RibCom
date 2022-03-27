# RibCom

A messy library to easily serialize and deserialize Google's protobufs to/from byte arrays (and send/receive them). Also provides a barebone functional [ENet](https://github.com/nxrighthere/ENet-CSharp) client/server implementation that natively sends/receives Protobuf messages objects.

It is still very barebone and lacks lots of basic stuff, and it will probably more or less stay this way (as long as it works for me).

## Quickstart

Let's say we have an assembly with a generated protobuf type `MyCustomMessage`.

```C#
using RibCom;
using RibCom.Enet;
using RibCom.ProtoHelper;
using RibCom.Tools;

public class FooClass
{
  [MessageListener]
  private void OnCustomMessage(MyCustomMessage msg)
  {
    // Do stuff
  }
}

private void Setup()
{
  // ---------- Basic setup --------------
  MessageSolver solver = new ();
  solver.AddScannedAssembly(typeof(MyCustomMessage));

  MessageDispatcher dispatcher = new MessageDispatcher(solver);
  FooClass foo = new FooClass();
  dispatcher.RegisterListener(foo); // now, every [MessageListener] tagged methods of foo will be called when message are dispatched
  
  // ---------- If we are a server ------------
  ENet.Library.Initialize();
  IServer baseServer = new EnetServer("127.0.0.1", 25565, 100);
  ProtoServer server = new ProtoServer(baseServer, solver);
  baseServer.StartListening();
  
  while(!Console.KeyAvailable)
  {
    if (server != null)
    {
      while (server.TryDequeue(out ProtoMessage msg))
      {
        if (msg.Type == MessageContentType.Data)
          OnServerMessageReceived(msg.Content, msg.Source);
        else if (msg.Type == MessageContentType.Connected)
          OnServerClientConnected(msg.Source);
        else if (msg.Type == MessageContentType.Disconnected || msg.Type == MessageContentType.Timeout)
          OnServerClientDisconnected(msg.Source);
      }
    }
  }
  ENet.Library.Deinitialize();
  
  
  // -------- If we are a client -----------
  IClient baseClient = new EnetClient();
  baseClient.Connect("127.0.0.1", 25565);
  baseClient.StartListening();
  ProtoClient client = new ProtoClient(client, solver);
  
  while(!Console.KeyAvailable)
  {
    while (client.TryDequeue(out ProtoMessage message))
    {
      if (message.Type == MessageContentType.Data)
        OnClientMessageReceived(message.Content);
    }
  }
}
```
