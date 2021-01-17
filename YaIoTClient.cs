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
    Commands = 1
  }

  class YaIoTClient : IDisposable
  {
    public const string MqttServer = "mqtt.cloud.yandex.net";
    public const int MqttPort = 8883;

    private static X509Certificate2 rootCrt = new X509Certificate2("Data/rootCA.crt");

    public static string TopicName(string entityId, EntityType entity, TopicType topic)
    {
      string result = (entity == EntityType.Registry) ? "$registries/" : "$devices/";
      result += entityId;
      result += (topic == TopicType.Events) ? "/events" : "/commands";
      return result;
    }

    public delegate void OnSubscribedData(string topic, byte[] payload);
    public event OnSubscribedData SubscribedData;

    private IMqttClient mqttClient = null;
    private ManualResetEvent oCloseEvent = new ManualResetEvent(false);
    private ManualResetEvent oConnectedEvent = new ManualResetEvent(false);
        private IMqttClientOptions connProps = null;
    public void StartCert(string certPath, string certPassword)
    {
      X509Certificate certificate;
      if (string.IsNullOrEmpty(certPassword)){
       certificate = new X509Certificate(certPath);
      }else{
        SecureString secPwd = new NetworkCredential("",certPassword).SecurePassword;        
        certificate = new X509Certificate(certPath, secPwd);
      }
       
      List <X509Certificate> certificates = new List<X509Certificate>();
      certificates.Add(certificate);

      //setup connection options
      MqttClientOptionsBuilderTlsParameters tlsOptions = new MqttClientOptionsBuilderTlsParameters
      {
        
        Certificates = certificates,
        SslProtocol = SslProtocols.Tls12,
        UseTls = true
      };
      tlsOptions.CertificateValidationCallback += CertificateValidationCallback;

      // Create TCP based options using the builder.
      var options = new MqttClientOptionsBuilder()
          .WithClientId($"Test_C#_Client_{Guid.NewGuid()}")
          .WithTcpServer(MqttServer, MqttPort)
          .WithTls(tlsOptions)
          .WithCleanSession()
          .WithCommunicationTimeout(TimeSpan.FromSeconds(90))
          .WithKeepAlivePeriod(TimeSpan.FromSeconds(90))
          .WithKeepAliveSendInterval(TimeSpan.FromSeconds(60))
          .Build();

      var factory = new MqttFactory();
      mqttClient = factory.CreateMqttClient();

      mqttClient.UseApplicationMessageReceivedHandler(DataHandler);
      mqttClient.UseConnectedHandler(ConnectedHandler);
      mqttClient.UseDisconnectedHandler(this.DisconnectedHandler);
      Console.WriteLine($"Connecting to mqtt.cloud.yandex.net...");
      this.connProps = options;
      mqttClient.ConnectAsync(this.connProps, CancellationToken.None);
    }

    public void StartPwd(string id, string password)
    {
      //setup connection options
      MqttClientOptionsBuilderTlsParameters tlsOptions = new MqttClientOptionsBuilderTlsParameters
      {
        SslProtocol = SslProtocols.Tls12,
        UseTls = true
      };
      tlsOptions.CertificateValidationCallback += CertificateValidationCallback;

      // Create TCP based options using the builder.
      var options = new MqttClientOptionsBuilder()
          .WithClientId($"Test_C#_Client_{Guid.NewGuid()}")
          .WithTcpServer(MqttServer, MqttPort)
          .WithTls(tlsOptions)
          .WithCleanSession()
          .WithCredentials(id, password)
          .WithKeepAlivePeriod(TimeSpan.FromSeconds(90))
          .WithKeepAliveSendInterval(TimeSpan.FromSeconds(60))
          .Build();

      var factory = new MqttFactory();
      mqttClient = factory.CreateMqttClient();

      mqttClient.UseApplicationMessageReceivedHandler(DataHandler);
      mqttClient.UseConnectedHandler(ConnectedHandler);
      this.connProps = options;
      mqttClient.UseDisconnectedHandler(this.DisconnectedHandler);
      Console.WriteLine($"Connecting to mqtt.cloud.yandex.net...");
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
        Console.WriteLine($"Error {arg.Exception.Message}");
           Thread.Sleep(5000); // Wait 5 sec before next connection attempt
            Console.WriteLine($"Trying reconnect");
            this.mqttClient.ConnectAsync(this.connProps, CancellationToken.None);  
      return Task.CompletedTask;
    }

    private Task DataHandler(MqttApplicationMessageReceivedEventArgs arg)
    {
      SubscribedData(arg.ApplicationMessage.Topic, arg.ApplicationMessage.Payload);
      return Task.CompletedTask;
    }

    private static bool CertificateValidationCallback(X509Certificate cert, X509Chain chain, SslPolicyErrors errors, IMqttClientOptions opts)
    {
      try
      {
        if (errors == SslPolicyErrors.None)
        {
          return true;
        }

        if (errors == SslPolicyErrors.RemoteCertificateChainErrors)
        {
          chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
          chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
          chain.ChainPolicy.ExtraStore.Add(rootCrt);

          chain.Build((X509Certificate2)rootCrt);
          var res = chain.ChainElements.Cast<X509ChainElement>().Any(a => a.Certificate.Thumbprint == rootCrt.Thumbprint);
          return res;
        }
      }
      catch { }

      return false;
    }

  }
}