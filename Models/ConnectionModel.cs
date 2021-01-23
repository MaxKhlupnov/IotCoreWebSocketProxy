using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IotCoreWebSocketProxy.Models
{
    public class ConnectionModel
    {
        public string ConnectionId { get; set; }
        public string DeviceId { get; set; }
        public string RegistryId { get; set; }
        /// <summary>
        /// This can be registry password, device password or certificate password
        /// </summary>
        public string Password { get; set; }
        public string RegistryCert { get; set; }

        public TopicType TopicType { get; set; }

        public byte[]CertificateBytes
        {
            get
            {
                if (string.IsNullOrEmpty(RegistryCert))
                    throw new ArgumentNullException("RegistryCert is null");
                else
                    return System.Convert.FromBase64String(RegistryCert);
            }
            
        }
    }
}
