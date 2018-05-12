using System;
using System.Collections.Generic;
using Microsoft.Owin;
using System.Threading.Tasks;
using System.Threading;

namespace SignalRPlayground
{
    // http://owin.org/spec/extensions/owin-WebSocket-Extension-v0.4.0.htm
    using WebSocketAccept = Action<IDictionary<string, object>, // options
        Func<IDictionary<string, object>, Task>>; // callback

    using WebSocketCloseAsync =
        Func<int /* closeStatus */,
            string /* closeDescription */,
            CancellationToken /* cancel */,
            Task>;
    using WebSocketReceiveAsync =
        Func<ArraySegment<byte> /* data */,
            CancellationToken /* cancel */,
            Task<Tuple<int /* messageType */,
                bool /* endOfMessage */,
                int /* count */>>>;
    using WebSocketSendAsync =
        Func<ArraySegment<byte> /* data */,
            int /* messageType */,
            bool /* endOfMessage */,
            CancellationToken /* cancel */,
            Task>;
    using WebSocketReceiveResult = Tuple<int, // type
        bool, // end of message?
        int>; // count

    class WebSocketMiddleware : OwinMiddleware
    {
        public WebSocketMiddleware(OwinMiddleware next) : base(next)
        {
        }

        public async sealed override Task Invoke(IOwinContext context)
        {
            WebSocketAccept accept = context.Get<WebSocketAccept>("websocket.Accept");

            if (accept == null)
            {
                await Next.Invoke(context).ConfigureAwait(false);
            }
            else
            {
                accept(null, WebSocketBody);
            }
        }

        private async Task WebSocketBody(IDictionary<string, object> environment)
        {
            var sendAsync = (WebSocketSendAsync)environment["websocket.SendAsync"];
            var receiveAsync = (WebSocketReceiveAsync)environment["websocket.ReceiveAsync"];
            var closeAsync = (WebSocketCloseAsync)environment["websocket.CloseAsync"];
            var callCancelled = (CancellationToken)environment["websocket.CallCancelled"];

            byte[] buffer = new byte[1024];
            WebSocketReceiveResult received = await receiveAsync(new ArraySegment<byte>(buffer), callCancelled);

            Console.WriteLine($"{received.Item1}:{received.Item2}:{received.Item3}");
        }
    }
}
