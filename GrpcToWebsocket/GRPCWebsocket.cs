using Grpc.Core;
using Newtonsoft.Json.Linq;
using System;
using WebSocketSharp;
using WebSocketSharp.Server;
using GrpctoWebsocketProxy.CallConverterNs;
namespace GrpctoWebsocketProxy.GrpcToWebsocket
{
    public class GrpcWebSocket : WebSocketBehavior
    {

        public GrpcWebSocket()
        {

        }

        public GrpcWebSocket(ClientBase cli)
        {

            this.cli = cli;
        }

        ClientBase cli;

        protected override void OnOpen()
        {

            base.OnOpen();
        }

        protected async override void OnMessage(MessageEventArgs e)
        {

            var jObj = JObject.Parse(e.Data);
            string method = "";
            string body = "";
            string requestId = "";

            try
            {
                requestId = jObj["requestId"].ToString();
            }
            catch (NullReferenceException ex)
            {
                requestId = "";
            }

            try
            {
                method = jObj["method"].ToString();
            }
            catch (NullReferenceException ex)
            {
                JObject methodNullError = new JObject();
                methodNullError.Add(
                    new JProperty(
                        "error", "\"method\" property can't be null")
                    );
                methodNullError.Add(
                   new JProperty(
                       "advice", "\"method\" is your Grpc Method name, \"requestId\" is id of request (use if want to make a lot of calls) and \"body\" is payload for your method.")
                   );
                methodNullError.Add(
                    new JProperty("requestId", requestId)
                    );
                Send(methodNullError.ToString());
                return;

            }

            try
            {
                body = jObj["body"].ToString();
            }
            catch (NullReferenceException ex)
            {
                body = "{}";
            }




            try
            {

                CallConverter converter = new CallConverter(cli, this.Context, requestId);
                await converter.CallMethod(method, body);
            }
            catch (RpcException er)
            {
                JObject rpcExceptionData = new JObject();
                rpcExceptionData.Add(
                    new JProperty("error", "RpcException")
                    );
                rpcExceptionData.Add(
                    new JProperty("statusCode", er.StatusCode)
                    );
                rpcExceptionData.Add(
                    new JProperty("details", er.Status.Detail)
                    );
                rpcExceptionData.Add(
                    new JProperty("requestId", requestId)
                    );





                Send(rpcExceptionData.ToString());
            }
            catch (Exception er)
            {

                JObject unknownException = new JObject();
                unknownException.Add(new JProperty("error", "AnotherException"));
                unknownException.Add(
                    new JProperty("statusCode", 500)
                    );
                unknownException.Add(new JProperty("details", er.InnerException.Message));
                unknownException.Add(
                    new JProperty("requestId", requestId)
                    );


                Send(er.Message);
            }
        }

        protected override void OnError(ErrorEventArgs e)
        {
            JObject socketException = new JObject();

            socketException.Add(new JProperty("error", "WebSocketException"));
            socketException.Add(
                    new JProperty("statusCode", 501)
                    );
            socketException.Add(new JProperty("details", e.Message));


            Send(e.Message);
        }


    }
}
