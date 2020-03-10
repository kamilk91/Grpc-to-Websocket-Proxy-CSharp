# Grpc-to-Websocket-Proxy-CSharp
Proxy to use in .NET Core. Translate GRPC messages to Json Websocket messages. Streams allowed. 

#Usage

## 1. Create your grpc client instance, as always

```
 string addressServer = "your.grpc.host.com";
 int portServer = 8000;
 var ch = new Grpc.Core.Channel(addressServer, portServer, ChannelCredentials.Insecure);
 var cli = new YourClientType(ch,creditentials)
```

## 2. Create instance of WebSocket Server with your client, typeof `GrpcWebSocket` and add your client/clients.

```
 WebSocketServer socketServer = new WebSocketServer(50002, false);
 socketServer.AddWebSocketService<GrpcWebSocket>("/FirstService", () => new GrpcWebSocket(cli));
 socketServer.AddWebSocketService<GrpcWebSocket>("/SecondService", () => new GrpcWebSocket(cli2));
 socketServer.Start();

```

## 3. Use simply Websocket call on port (in our exapmple) 50002. Sample Message must contain Method name, and optionaly Body, and requestId

```
 //==> ws://127.0.0.1:50002
 //requestpayload:
 {"requestId":"xxxxXXXXxxxx","method":"getUserItems","id":"32"}

 //method getUser response is grpc stream with user Items

 //responses

 //1.
 {
  "requestId": "asd1d21",
  "currentMessage": "{"item":"pencil"}"
  }
  //2.
  {
  "requestId": "asd1d21",
  "currentMessage": "{"item":"notebook"}"
  }
  //3.
  {
  "requestId": "asd1d21",
  "currentMessage": "{"item":"microphone"}"
  }

```


## 4. TODO
 - wss
 - tests
 