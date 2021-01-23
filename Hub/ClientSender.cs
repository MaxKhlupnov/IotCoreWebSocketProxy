using System.Threading.Tasks;
using IotCoreWebSocketProxy.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace IotCoreWebSocketProxy.Hub
{
    public class ClientSender : IClientSender
    {
        private readonly IClientProxy _clientProxy;
        private readonly ILogger _logger;
        public ClientSender(IClientProxy clientProxy, ConnectionModel conn, ILogger logger)
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

        public ConnectionModel Connection { get; }
    }
}
