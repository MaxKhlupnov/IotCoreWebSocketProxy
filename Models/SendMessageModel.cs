using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IotCoreWebSocketProxy.Models
{
    public class SendMessageModel
    {
        public string DeviceId { get; set; }
        public string RegistryId { get; set; }
        /// <summary>
        /// This can be registry password, device password or certificate password
        /// </summary>
        public string Password { get; set; }
        public string RegistryCert { get; set; }

        public string TopicType { get; set; }
        public string Message { get; set; }
    }
}
