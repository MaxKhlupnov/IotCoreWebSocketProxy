using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using System.Security;
using Microsoft.Extensions.Logging;
using IotCoreWebSocketProxy.Hub;

namespace IotCoreWebSocketProxy
{
  public enum EntityType
  {
    Registry = 0,
    Device = 1
  }

  public enum TopicType
  {
    Events = 0,
    Commands = 1,
    State = 2
    }

  class YaIoTClient : IDisposable
  {
    public const string MqttServer = "mqtt.cloud.yandex.net";
    public const int MqttPort = 8883;

    private static X509Certificate2 rootCrt = new X509Certificate2("Data/rootCA.crt");

    private ClientSender _sender;

    public YaIoTClient(ClientSender sender)
    {
            this._sender = sender;
           
    }
    
    /**
        * Please see for details https://cloud.yandex.com/docs/iot-core/concepts/topic
    */
        public static string TopicName(string entityId, EntityType entity, TopicType topic)
    {
      string result = (entity == EntityType.Registry) ? "$registries/" : "$devices/";
      result += entityId;
      switch (topic)
      {
        case TopicType.Events:
            result += "/events";
            break;
        case TopicType.Commands:
            result += "/commands";
            break;
        case TopicType.State:
            result += "/state";
            break;
        default:
          throw new ArgumentException($"Incorrect TopicType {topic}");
       }
      
      return result;
    }

    public delegate void OnSubscribedData(string topic, byte[] payload);
    public event OnSubscribedData SubscribedData;

    private IMqttClient mqttClient = null;
    private ManualResetEvent oCloseEvent = new ManualResetEvent(false);
    private ManualResetEvent oConnectedEvent = new ManualResetEvent(false);
        private IMqttClientOptions connProps = null;
    public void StartCert(string connectionId, byte[] rowdata, string certPassword)
    {
      X509Certificate certificate;
      if (string.IsNullOrEmpty(certPassword)){
        certificate = new X509Certificate(rowdata);
      }else{
        SecureString secPwd = new NetworkCredential("",certPassword).SecurePassword;        
        certificate = new X509Certificate(rowdata, secPwd);
      }
       
      List <X509Certificate> certificates = new List<X509Certificate>();
      certificates.Add(certificate);

            //setup connection options
            MqttClientOptionsBuilderTlsParameters tlsOptions = new MqttClientOptionsBuilderTlsParameters
            {
                SslProtocol = SslProtocols.Tls12,
                CertificateValidationHandler = CertificateValidationHandler,
                UseTls = true
            };

            // Create TCP based options using the builder.
            var options = new MqttClientOptionsBuilder()
                .WithClientId(connectionId)
                .WithTcpServer(MqttServer, MqttPort)
                .WithTls(tlsOptions)
                .WithCleanSession()
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(300))
                .Build();

            var factory = new MqttFactory();
      mqttClient = factory.CreateMqttClient();

      mqttClient.UseApplicationMessageReceivedHandler(DataHandler);
      mqttClient.UseConnectedHandler(ConnectedHandler);
      mqttClient.UseDisconnectedHandler(this.DisconnectedHandler);
            _sender.SendInfo($"Connecting to mqtt.cloud.yandex.net...");
      this.connProps = options;
      mqttClient.ConnectAsync(this.connProps, CancellationToken.None);
    }

    public void StartPwd(string connectionId, string id, string password)
    {
            //setup connection options
            MqttClientOptionsBuilderTlsParameters tlsOptions = new MqttClientOptionsBuilderTlsParameters
            {
                SslProtocol = SslProtocols.Tls12,
                CertificateValidationHandler = CertificateValidationHandler,
                UseTls = true
            };

            // Create TCP based options using the builder.
            var options = new MqttClientOptionsBuilder()
                .WithClientId(connectionId)
                .WithTcpServer(MqttServer, MqttPort)
                .WithTls(tlsOptions)
                .WithCleanSession()
                .WithCredentials(id, password)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(300))
                .Build();

            var factory = new MqttFactory();
      mqttClient = factory.CreateMqttClient();

      mqttClient.UseApplicationMessageReceivedHandler(DataHandler);
      mqttClient.UseConnectedHandler(ConnectedHandler);
      this.connProps = options;
      mqttClient.UseDisconnectedHandler(this.DisconnectedHandler);
            _sender.SendInfo($"Connecting to mqtt.cloud.yandex.net...");
      mqttClient.ConnectAsync(options, CancellationToken.None);
    }

    public void Stop()
    {
      oCloseEvent.Set();
      mqttClient.DisconnectAsync();
    }

    public void Dispose()
    {
      Stop();
    }

    public bool WaitConnected()
    {
      WaitHandle[] waites = { oCloseEvent, oConnectedEvent };
      return WaitHandle.WaitAny(waites) == 1;
    }

    public Task Subscribe(string topic, MQTTnet.Protocol.MqttQualityOfServiceLevel qos)
    {
      return mqttClient.SubscribeAsync(topic, qos);
    }

    public Task Publish(string topic, string payload, MQTTnet.Protocol.MqttQualityOfServiceLevel qos)
    {
      return mqttClient.PublishAsync(topic, payload, qos);
    }
    private Task ConnectedHandler(MqttClientConnectedEventArgs arg)
    {
      oConnectedEvent.Set();
      return Task.CompletedTask;
    }

    private Task DisconnectedHandler(MqttClientDisconnectedEventArgs arg)
    {
      Console.WriteLine($"Disconnected mqtt.cloud.yandex.net.");
    if (arg.Exception != null && !string.IsNullOrEmpty(arg.Exception.Message))
                _sender.SendError($"Error {arg.Exception.Message}");
           Thread.Sleep(5000); // Wait 5 sec before next connection attempt
            _sender.SendInfo($"Trying reconnect");
            this.mqttClient.ConnectAsync(this.connProps, CancellationToken.None);  
      return Task.CompletedTask;
    }

    private Task DataHandler(MqttApplicationMessageReceivedEventArgs arg)
    {
      SubscribedData(arg.ApplicationMessage.Topic, arg.ApplicationMessage.Payload);
      return Task.CompletedTask;
    }

        private bool CertificateValidationHandler(MqttClientCertificateValidationCallbackContext arg)
        {
            try
            {
                if (arg.SslPolicyErrors == SslPolicyErrors.None)
                {
                    return true;
                }

                if (arg.SslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors)
                {
                    arg.Chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    arg.Chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
                    arg.Chain.ChainPolicy.ExtraStore.Add(rootCrt);

                    arg.Chain.Build((X509Certificate2)rootCrt);
                    var res = arg.Chain.ChainElements.Cast<X509ChainElement>().Any(a => a.Certificate.Thumbprint == rootCrt.Thumbprint);
                    return res;
                }
            }
            catch { }

            return false;
        }

    }
}