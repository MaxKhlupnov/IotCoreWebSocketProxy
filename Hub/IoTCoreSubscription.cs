using System;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using IotCoreWebSocketProxy;
using MQTTnet.Protocol;
using Microsoft.Extensions.Logging;

namespace IotCoreWebSocketProxy.Hub
{
    public class IoTCoreSubscription
    {

        private ClientSender _sender;
        private SignalRConnection _connection;
        
        private string _topic;
        private YaIoTClient _iotCoreClient;

        private ILogger _logger;

        internal IoTCoreSubscription(SignalRConnection connection, ILogger logger)
        {
            this._logger = logger;

            if (connection == null)
                throw new ArgumentNullException("IoTCoreSubscription called with null connection");

            this._connection = connection;

            if (string.IsNullOrEmpty(connection.DeviceId))
            {
                //Subscribe a registry to the topics of all devices added to it
                this._topic = YaIoTClient.TopicName(connection.RegistryId, EntityType.Registry, connection.TopicType);
            }
            else
            {
                // Subscribe a registry to a device's permanent topic 
                this._topic = YaIoTClient.TopicName(connection.DeviceId, EntityType.Device, connection.TopicType);
            }

            _iotCoreClient = new YaIoTClient(logger);

        }
        public void RegisterMessageTrace(ClientSender sender)
        {
            _sender = sender;

            if (!string.IsNullOrEmpty(sender.Connection.RegistryCert))
            // certificate-based authorization
            {
                this._iotCoreClient.StartCert(sender.Connection.ConnectionId, sender.Connection.CertificateBytes, sender.Connection.Password);
            }
            else
            // username and password authorization
            {                
                this._iotCoreClient.StartPwd(sender.Connection.ConnectionId, sender.Connection.RegistryId, sender.Connection.Password);
            }
            if (this._iotCoreClient.WaitConnected())
            {
                _logger.LogInformation($"Device {sender.Connection.RegistryId} connected successfully");
                this._iotCoreClient.Subscribe(this._topic, MqttQualityOfServiceLevel.AtLeastOnce);
                this._iotCoreClient.SubscribedData += _iotCoreClient_SubscribedData;
            }
            else
            {
                _logger.LogError($"Device {sender.Connection.RegistryId} connection error");
            }

           
             
        }

        private async void _iotCoreClient_SubscribedData(string topic, byte[] payload)
        {
            if (payload != null)
                await ProcessMessagesAsync(payload);

        }

        private async Task ProcessMessagesAsync(byte[] body)
        {
            string msgString = Encoding.UTF8.GetString(body);
            await _sender.SendAsync(msgString);
        }

        public void DeleteSubscription(string subscriptionId)
        {
            if (this._iotCoreClient != null)
            {
                this._iotCoreClient.Stop();
                this._iotCoreClient = null;
            }
        }
    }
}
