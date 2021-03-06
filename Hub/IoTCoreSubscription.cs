﻿using System;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using IotCoreWebSocketProxy;
using MQTTnet.Protocol;
using Microsoft.Extensions.Logging;
using IotCoreWebSocketProxy.Models;

namespace IotCoreWebSocketProxy.Hub
{


    public class IoTCoreSubscription 
    {

        private ClientSender _sender;
        private ConnectionModel _connection;
        
        private string _topic;
        private YaIoTClient _iotCoreClient;

        internal IoTCoreSubscription(ConnectionModel connection)
        {
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
        }

        public void RegisterMessageTrace(ClientSender sender)
        {
            _sender = sender;
            _iotCoreClient = new YaIoTClient(sender);

            if (!string.IsNullOrEmpty(sender.Connection.RegistryCert))
            // certificate-based authorization
            {
                this._iotCoreClient.StartCert(sender.Connection.ConnectionId, sender.Connection.CertificateBytes, sender.Connection.Password);
            }
            else
            // username and password authorization
            {   
                string mqttUser = string.IsNullOrEmpty(sender.Connection.RegistryId) ? sender.Connection.DeviceId : sender.Connection.RegistryId;
                this._iotCoreClient.StartPwd(sender.Connection.ConnectionId, mqttUser, sender.Connection.Password);
            }
            if (this._iotCoreClient.WaitConnected())
            {
                _sender.SendInfo($"Device {sender.Connection.RegistryId} connected successfully. Topic: {this._topic}");
                this._iotCoreClient.Subscribe(this._topic, MqttQualityOfServiceLevel.AtLeastOnce);
                this._iotCoreClient.SubscribedData += _iotCoreClient_SubscribedData;
            }
            else
            {
                throw new ApplicationException($"Device {sender.Connection.RegistryId} connection error. Topic: {this._topic}");
            }

           
             
        }

        private async void _iotCoreClient_SubscribedData(string topic, byte[] payload)
        {
            if (payload != null)
            {
                string msgString = Encoding.UTF8.GetString(payload);
                await _sender.SendAsync(msgString);
            }

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
