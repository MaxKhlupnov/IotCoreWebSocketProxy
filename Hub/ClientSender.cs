using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace IotCoreWebSocketProxy.Hub
{
    public class ClientSender
    {
        private readonly IClientProxy _clientProxy;

        public ClientSender(IClientProxy clientProxy, SignalRConnection conn)
        {
            _clientProxy = clientProxy;
            Connection = conn;
        }

        public Task SendAsync(string message)
        {
            return _clientProxy.SendAsync("Trace", message);
        }

        public SignalRConnection Connection { get; }
    }
}
