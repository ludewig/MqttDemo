using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MQTTnet.Client;

namespace MqttDemo.Client
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        IMqttClient _client;
        public MainWindow()
        {
            InitializeComponent();
        }

        #region MQTT事件
        private void InitClient(string url = "127.0.0.1", int port = 1883)
        {
            var options = new MqttClientOptions()
            {
                ClientId = Guid.NewGuid().ToString("N")
            };
            options.ChannelOptions = new MqttClientTcpOptions()
            {
                Server = url,
                Port = port
            };
            options.Credentials = new MqttClientCredentials()
            {

            };
            options.CleanSession = true;
            options.KeepAlivePeriod = TimeSpan.FromSeconds(100);
            options.KeepAliveSendInterval = TimeSpan.FromSeconds(10000);
            if (_client != null)
            {
                _client.DisconnectAsync();
                _client = null;
            }
            _client = new MQTTnet.MqttFactory().CreateMqttClient();
            _client.ApplicationMessageReceived += _client_ApplicationMessageReceived;
            _client.Connected += _client_Connected;
            _client.Disconnected += _client_Disconnected;
            _client.ConnectAsync(options);
        }

        /// <summary>
        /// 客户端与服务端断开连接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _client_Disconnected(object sender, MqttClientDisconnectedEventArgs e)
        {

        }

        /// <summary>
        /// 客户端与服务端建立连接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void _client_Connected(object sender, MqttClientConnectedEventArgs e)
        {

        }

        /// <summary>
        /// 客户端收到消息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _client_ApplicationMessageReceived(object sender, MQTTnet.MqttApplicationMessageReceivedEventArgs e)
        {
            WriteToStatus("收到来自客户端"+e.ClientId+"，主题为"+e.ApplicationMessage.Topic+"的消息："+Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
        } 
        #endregion

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtUrl.Text)||string.IsNullOrEmpty(txtPort.Text))
            {
                return;
            }
            else
            {
                InitClient(txtUrl.Text,int.Parse(txtPort.Text));
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (_client!=null)
            {
                _client.DisconnectAsync();
            }
        }

        #region 状态输出
        public void WriteToStatus(string message)
        {
            if (!(txtRich.CheckAccess()))
            {
                this.Dispatcher.Invoke(() =>
                    WriteToStatus(message)
                    );
                return;
            }
            string strTime = "[" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] ";
            txtRich.AppendText(strTime + message + "\r");
            if (txtRich.ExtentHeight > 200)
            {
                txtRich.Document.Blocks.Clear();
            }
        }
        #endregion

        #region 发布消息
        private void btnPublish_Click(object sender, RoutedEventArgs e)
        {
            if (_client!=null)
            {
                MQTTnet.MqttApplicationMessage msg = new MQTTnet.MqttApplicationMessage()
                {
                    Topic="/data/temp",
                    Payload=Encoding.UTF8.GetBytes(txtContent.Text),
                    QualityOfServiceLevel=MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce,
                    Retain=false
                };
                _client.PublishAsync(msg);
                WriteToStatus("发布消息至主题"+msg.Topic+"成功！");
            }
        }
        #endregion

        #region 订阅消息
        private void btnSub_Click(object sender, RoutedEventArgs e)
        {
            if (_client!=null)
            {
                List<MQTTnet.TopicFilter> filters = new List<MQTTnet.TopicFilter>()
                {
                    new MQTTnet.TopicFilter("/data/temp",MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                };
                _client.SubscribeAsync(filters);
                
                WriteToStatus("订阅主题/data/temp成功！");
            }
        } 
        #endregion
    }
}
