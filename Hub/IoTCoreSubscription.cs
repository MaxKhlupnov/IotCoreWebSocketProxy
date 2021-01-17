using System;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using IotCoreWebSocketProxy;
using MQTTnet.Protocol;

namespace IotCoreWebSocketProxy.Hub
{
    public class IoTCoreSubscription
    {

        private ClientSender _sender;
        private SignalRConnection _connection;
        
        private string _topic;
        private YaIoTClient _iotCoreClient;

        internal IoTCoreSubscription(SignalRConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException("IoTCoreSubscription called with null connection");

            this._connection = connection;

            this._topic = YaIoTClient.TopicName(connection.DeviceId, EntityType.Device, TopicType.Events);
            _iotCoreClient = new YaIoTClient();

        }
        public void RegisterMessageSender(ClientSender sender)
        {
            _sender = sender;

            if (!string.IsNullOrEmpty(sender.Connection.DeviceCert))
            {
                this._iotCoreClient.StartCert(sender.Connection.DeviceCert, sender.Connection.DevicePwd);
            }
            else
            {
                this._iotCoreClient.StartPwd(sender.Connection.DeviceId, sender.Connection.DevicePwd);
            }

            this._iotCoreClient.SubscribedData += _iotCoreClient_SubscribedData;
            this._iotCoreClient.Subscribe(this._topic, MqttQualityOfServiceLevel.AtLeastOnce);
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
