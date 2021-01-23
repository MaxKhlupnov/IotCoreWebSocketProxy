using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using IotCoreWebSocketProxy.Models;

namespace IotCoreWebSocketProxy.Hub
{
    public class IoTCoreTelemetryHub : Microsoft.AspNetCore.SignalR.Hub
    {

        private readonly ILogger<IoTCoreTelemetryHub> _logger;
        public IoTCoreTelemetryHub(ILogger<IoTCoreTelemetryHub> logger) : base()
        {
            this._logger = logger;
        }

        IoTCoreSubscription _iotCoreSubscription;

        public override Task OnDisconnectedAsync(Exception exception)
        {
            if (_iotCoreSubscription != null)
            {
                _iotCoreSubscription.DeleteSubscription(Context.ConnectionId);
            }
            
            return base.OnDisconnectedAsync(null);
        }
        /// <summary>
        /// Subscribe a registry to the topics of all devices
        /// </summary>
        /// <param name="registryId"></param>
        /// <param name="password">registry password or certificate password (for certificate-based authorization)</param>
        /// <param name="registryCert"></param>
        public void TraceRegistryMessages(string trace_type_radio, string registryId, string password, string registryCert)
        {
            TraceDeviceMessages(trace_type_radio, null, registryId, password, registryCert);
        }

        /// <summary>
        /// Subscribe a registry to a single device's topic
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="registryId"></param>
        /// <param name="password">registry password or certificate password (for certificate-based authorization)</param>
        /// <param name="registryCert"></param>
        public void TraceDeviceMessages(string trace_type_radio, string deviceId, string registryId, string password, string registryCert)
        {
            try
            {
                ConnectionModel conn = new ConnectionModel()
                {
                    ConnectionId = Context.ConnectionId,
                    TopicType = Enum.Parse<TopicType>(trace_type_radio),
                    DeviceId = deviceId,
                    RegistryId = registryId,
                    Password = password,
                    RegistryCert = registryCert
                };

               

                var clientProxy = Clients.Clients(Context.ConnectionId);

                var messageSender = new ClientSender(clientProxy, conn, this._logger);

                _iotCoreSubscription = new IoTCoreSubscription(conn);
                _iotCoreSubscription.RegisterMessageTrace(messageSender);
            }
            catch(Exception ex)
            {
                this._logger.LogError(ex.Message);
                throw new HubException(ex.Message);
            }
        }


    }

}
