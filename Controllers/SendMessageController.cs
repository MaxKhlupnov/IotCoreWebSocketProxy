using IotCoreWebSocketProxy.Hub;
using IotCoreWebSocketProxy.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace IotCoreWebSocketProxy.Controllers
{
    [ApiController]
    public class MessageController : ControllerBase
    {

        [Route("api/message/send")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]

        public async Task<IActionResult> SendMessage(SendMessageModel model) {


            ConnectionModel connection = new ConnectionModel
            {
                ConnectionId = HttpContext.Connection.RemoteIpAddress.ToString(),
                TopicType = Enum.Parse<TopicType>(model.TopicType),
                DeviceId = model.DeviceId,
                RegistryId = model.RegistryId,
                Password = model.Password,
                RegistryCert = model.RegistryCert
            };
            SyncClientSender clientSender = new SyncClientSender();

            if (string.IsNullOrEmpty(connection.DeviceId) || string.IsNullOrEmpty(connection.Password) || string.IsNullOrEmpty(model.Message))
            {
                await clientSender.SendError("DeviceId, Password and Model are required fields");
                return BadRequest(clientSender);
            }
            if (TopicType.Commands == connection.TopicType && string.IsNullOrEmpty(connection.RegistryId))
            {
                await clientSender.SendError("RegistryId must be specified for Commands");
                return BadRequest(clientSender);
            }

           
            try
            {
                await this.MqttSend(connection, clientSender, model.Message);
            }catch(Exception ex)
            {
                await clientSender.SendError(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,clientSender);
            }
                        
            return Ok(clientSender);
        }

        private async Task MqttSend(ConnectionModel connection, SyncClientSender clientSender, string payload)
        {
            string topic;
            if (string.IsNullOrEmpty(connection.RegistryId))
            {
                // Send data from a device to a registry topic
                topic = YaIoTClient.TopicName(connection.DeviceId, EntityType.Device, connection.TopicType);
                
            }
            else
            {
                //Send data from a device to a device topic
                topic = YaIoTClient.TopicName(connection.RegistryId, EntityType.Registry, connection.TopicType);
            }

            
            YaIoTClient iotCoreClient = new YaIoTClient(clientSender);
            if (!string.IsNullOrEmpty(connection.RegistryCert))
            // certificate-based authorization
            {
                iotCoreClient.StartCert(connection.ConnectionId, connection.CertificateBytes, connection.Password);
            }
            else
            // username and password authorization
            {
                iotCoreClient.StartPwd(connection.ConnectionId, connection.DeviceId, connection.Password);
            }

            if (iotCoreClient.WaitConnected())
            {
               await clientSender.SendInfo($"Device {connection.RegistryId} connected successfully. Topic: {topic}");
            }
            else
            {
                throw new ApplicationException($"Device {connection.RegistryId} connection error. Topic: {topic}");
            }

            await iotCoreClient.Publish(topic, payload, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce);

            await clientSender.SendInfo($"Event sucessfully sent");
        }
        
    }

    public class SyncClientSender : IClientSender
    {
        private List<string> errors = new List<string>();
        private List<string> info = new List<string>();
       
        [JsonProperty("errors")]
        public ICollection<string> Errors { get { return this.errors; } }

        [JsonProperty("info")]
        public ICollection<string> Info { get { return this.info; } }

        public Task SendError(string message)
        {
            this.errors.Add(message);
            return Task.CompletedTask;
        }

        public Task SendInfo(string message)
        {
            this.info.Add(message);
            return Task.CompletedTask;
        }
    }
}
