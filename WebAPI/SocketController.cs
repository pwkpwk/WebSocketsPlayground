using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SignalRPlayground.WebAPI
{
    using Microsoft.Owin;
    using System.Net;
    using System.Net.Http;
    using System.Web.Http;

    // http://owin.org/spec/extensions/owin-WebSocket-Extension-v0.4.0.htm
    using OwinWebSocketAccept = Action<IDictionary<string, object>, // options
        Func<IDictionary<string, object>, Task>>; // callback
    using OwinWebSocketCloseAsync =
        Func<int /* closeStatus */,
            string /* closeDescription */,
            CancellationToken /* cancel */,
            Task>;
    using OwinWebSocketReceiveAsync =
        Func<ArraySegment<byte> /* data */,
            CancellationToken /* cancel */,
            Task<Tuple<int /* messageType */,
                bool /* endOfMessage */,
                int /* count */>>>;
    using OwinWebSocketSendAsync =
        Func<ArraySegment<byte> /* data */,
            int /* messageType */,
            bool /* endOfMessage */,
            CancellationToken /* cancel */,
            Task>;
    using OwinWebSocketReceiveResult = Tuple<int, // type
            bool, // end of message?
            int>; // count
    using System.Diagnostics;

    [RoutePrefix("api/v1")]
    public sealed class SocketController : ApiController
    {
        public SocketController()
        {
        }

        [Route("socket"), HttpGet]
        public Task<HttpResponseMessage> Connect()
        {
            IOwinContext owinContext = Request.GetOwinContext();
            HttpResponseMessage response = null;

            if (owinContext != null)
            {
                OwinWebSocketAccept accept = owinContext.Get<OwinWebSocketAccept>("websocket.Accept");

                if (accept != null)
                {
                    accept(null, WebSocketBody);
                    response = Request.CreateResponse(HttpStatusCode.SwitchingProtocols);
                }
            }

            if (response == null)
            {
                response = Request.CreateErrorResponse(HttpStatusCode.BadRequest, new HttpError("Bad request."));
            }

            return Task.FromResult(response);
        }

        private async Task WebSocketBody(IDictionary<string, object> owinContext)
        {
            var sendAsync = (OwinWebSocketSendAsync)owinContext["websocket.SendAsync"];
            var receiveAsync = (OwinWebSocketReceiveAsync)owinContext["websocket.ReceiveAsync"];
            var closeAsync = (OwinWebSocketCloseAsync)owinContext["websocket.CloseAsync"];
            var callCancelled = (CancellationToken)owinContext["websocket.CallCancelled"];

            byte[] buffer = new byte[1024];
            object status;

            while (!owinContext.TryGetValue("websocket.ClientCloseStatus", out status) || (int)status == 0)
            {
                OwinWebSocketReceiveResult received = await receiveAsync(new ArraySegment<byte>(buffer), callCancelled);
                await sendAsync(new ArraySegment<byte>(buffer, 0, received.Item3), received.Item1, received.Item2, callCancelled);
                Trace.WriteLine($"{received.Item1}:{received.Item2}:{received.Item3}");
            }

            await closeAsync((int)owinContext["websocket.ClientCloseStatus"], (string)owinContext["websocket.ClientCloseDescription"], callCancelled);
        }
    }
}
