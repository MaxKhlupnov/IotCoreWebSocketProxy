using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace IotCoreWebSocketProxy.Hub
{
    public class ClientSender
    {
        private readonly IClientProxy _clientProxy;
        private readonly ILogger _logger;
        public ClientSender(IClientProxy clientProxy, SignalRConnection conn, ILogger logger)
        {
            _clientProxy = clientProxy;
            _logger = logger;
            Connection = conn;
        }

        public Task SendAsync(string message)
        {
            return _clientProxy.SendAsync("Trace", message);
        }

        public Task SendError(string message)
        {
            _logger.LogError(message);
            return _clientProxy.SendAsync("Error", message);         
        }

        public Task SendInfo(string message)
        {
            _logger.LogInformation(message);
            return _clientProxy.SendAsync("Info", message);           
        }

        public SignalRConnection Connection { get; }
    }
}
