using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IotCoreWebSocketProxy.Hub
{
    interface IClientSender
    {
        public Task SendError(string message);
        public Task SendInfo(string message);
    }
}
