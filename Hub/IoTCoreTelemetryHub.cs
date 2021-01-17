using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace IotCoreWebSocketProxy.Hub
{
    public class IoTCoreTelemetryHub : Microsoft.AspNetCore.SignalR.Hub
    {
       
        IoTCoreSubscription _iotCoreSubscription;

        public override Task OnDisconnectedAsync(Exception exception)
        {
            if (_iotCoreSubscription != null)
            {
                _iotCoreSubscription.DeleteSubscription(Context.ConnectionId);
            }
            
            return base.OnDisconnectedAsync(null);
        }

        public void TraceDeviceMessages(string deviceId, string devicePwd, string deviceCert)
        {
            SignalRConnection conn = new SignalRConnection()
            {
                ConnectionId = Context.ConnectionId,
                DeviceId = deviceId,
                DevicePwd = devicePwd,
                DeviceCert = deviceCert
            };

            _iotCoreSubscription = new IoTCoreSubscription(conn);

            var clientProxy = Clients.Clients(Context.ConnectionId); 

            var messageSender = new ClientSender(clientProxy, conn);

            _iotCoreSubscription.RegisterMessageSender(messageSender);
        }


    }
}
