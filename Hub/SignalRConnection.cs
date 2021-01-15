using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IotCoreWebSocketProxy.Hub
{
    public class SignalRConnection
    {
        public string DeviceId { get; set; }
        public string DevicePwd { get; set; }
        public string DeviceCert { get; set; }
    }
}
