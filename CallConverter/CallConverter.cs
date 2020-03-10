using Grpc.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using WebSocketSharp.Net.WebSockets;

namespace GrpctoWebsocketProxy.CallConverterNs
{
    class CallConverter
    {
        private object client;
        WebSocketContext socketContext;
        private string requestId;
        public CallConverter(object client)
        {
            this.client = client;

        }

        public CallConverter(object client, WebSocketContext cnx, string requestId = "")
        {
            this.client = client;
            this.socketContext = cnx;
            this.requestId = requestId;
        }


        public async Task<object> CallMethod(string method, string jsonParameters)
        {
            MethodInfo CurrentMethod = CreateMethod(method);

            if (string.IsNullOrEmpty(jsonParameters))
            {
                jsonParameters = new JObject().ToString();
            }

            if (CurrentMethod == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Method does not exist. Remember its case sensitive."));
            }


            Type CurrentParameterType = CreateParameterType(CurrentMethod);

            object Deserialized = DeserializeFromJson(jsonParameters, CurrentParameterType);

            try
            {
                dynamic methodCall;


                if (CurrentMethod.Name.ToLower().Contains("async"))
                {
                    throw new RpcException(new Status(StatusCode.Unavailable, "Async grpc methods currently unavailable."));


                }
                else
                {
                    methodCall = InvokeMethod(CurrentMethod, Deserialized);
                }

                var responseStreamProperty = methodCall.GetType().GetProperty("ResponseStream");
                if (responseStreamProperty != null)
                {
                    List<object> listOfItems = new List<object>();
                    IAsyncStreamReader<object> asyncStreamReader = (IAsyncStreamReader<object>)responseStreamProperty.GetValue(methodCall);
                    try
                    {
                        while (await asyncStreamReader.MoveNext())
                        {
                            if (socketContext != null)
                            {
                                JObject messageClear = new JObject();
                                messageClear.Add(new JProperty("requestId", requestId));
                                messageClear.Add(new JProperty("currentMessage", asyncStreamReader.Current.ToString()));
                                socketContext.WebSocket.Send(messageClear.ToString());
                            }

                            listOfItems.Add(asyncStreamReader.Current);
                        }

                    }
                    catch (Exception e)
                    {
                        throw new RpcException(new Status(StatusCode.NotFound, "Stream was empty"));

                    }

                    return listOfItems;
                }
                else
                {
                    if (socketContext != null)
                    {
                        socketContext.WebSocket.Send(methodCall.ToString());

                    }
                    return methodCall;
                }
            }
            catch (Exception e)
            {
                new RpcException(new Status(StatusCode.Internal, e.Message));
            }
            throw new RpcException(new Status(StatusCode.Internal, "unknown"));

        }

        public JObject ImageBytesToBase64(string fieldName, JObject dataObject)
        {


            if (dataObject.ContainsKey(fieldName))
            {
                byte[] intArray = dataObject[fieldName].ToObject<byte[]>();
                string bytesString = Convert.ToBase64String(intArray);
                var byteString = Google.Protobuf.ByteString.CopyFrom(intArray);
                dataObject[fieldName] = byteString.ToBase64();
            }

            return dataObject;
        }
        private MethodInfo CreateMethod(string method)
        {
            MethodInfo[] methodInfos = client.GetType()
                           .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            var interestedMethod = methodInfos.Where(x => x.GetParameters().Length <= 2)
                .Where(x => x.Name.ToLower() == method.ToLower())
                .FirstOrDefault();


            return interestedMethod;

        }

        private Type CreateParameterType(MethodInfo interestedMethod)
        {

            ParameterInfo[] methodParameterInfos = interestedMethod
                .GetParameters();

            ParameterInfo methodParameterInfo = methodParameterInfos[0];

            Type methodParameterType = methodParameterInfo.ParameterType;



            return methodParameterType;
        }

        private object DeserializeFromJson(string json, Type methodParameterType)
        {
            return JsonConvert.DeserializeObject(json, methodParameterType);
        }


        private object InvokeMethod(MethodInfo method, object deserializedJson)
        {


            var result = method.Invoke(client, new object[] { deserializedJson, new Grpc.Core.CallOptions() });
            return result;

        }




    }
}
