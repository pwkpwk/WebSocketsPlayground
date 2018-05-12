namespace SignalRPlayground
{
    using Microsoft.Owin.Hosting;
    using Owin;
    using System;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;

    class Program
    {
        static void Main(string[] args)
        {
            StartOptions options = new StartOptions();

            options.Urls.Add("http://localhost:8080/");

            using (WebApp.Start(options, OwinStartup))
            {
                RunWebSocketClient("ws://localhost:8080/api/v1/socket").Wait();
                Console.WriteLine("Press Enter to exit.");
                Console.ReadLine();
            }
        }

        private static async Task RunWebSocketClient(string address)
        {
            using (ClientWebSocket socket = new ClientWebSocket())
            {
                await socket.ConnectAsync(new Uri(address), CancellationToken.None);
                await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("Don Pedro")), WebSocketMessageType.Text, true, CancellationToken.None);
                WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(new byte[100]), CancellationToken.None);
                await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Goodbye!", CancellationToken.None);
            }
        }

        private static void OwinStartup(IAppBuilder app)
        {
            HttpConfiguration config = new HttpConfiguration();
            config.MapHttpAttributeRoutes();
            //app.Use<WebSocketMiddleware>();
            app.UseWebApi(config);
        }
    }
}
