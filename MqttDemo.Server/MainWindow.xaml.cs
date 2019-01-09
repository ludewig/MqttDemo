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
using MQTTnet.Server;

namespace MqttDemo.Server
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        IMqttServer _server;
        public MainWindow()
        {
            InitializeComponent();
        }

        #region MQTT事件

        private void InitServer(string url = "127.0.0.1")
        {
            if (_server != null)
            {
                return;
            }
            var optionBuilder = new MqttServerOptionsBuilder().WithConnectionBacklog(1000);

            if (!string.IsNullOrEmpty(url))
            {
                optionBuilder.WithDefaultEndpointBoundIPAddress(System.Net.IPAddress.Parse(url));
                
            }

            MqttServerOptions options = optionBuilder.Build() as MqttServerOptions;
            options.ConnectionValidator += (context) =>
            {

            };

            _server = new MQTTnet.MqttFactory().CreateMqttServer() as MqttServer;
            _server.ClientConnected += _server_ClientConnected ;
            _server.ClientDisconnected += _server_ClientDisconnected ;
            _server.ApplicationMessageReceived += _server_ApplicationMessageReceived ;
            _server.ClientSubscribedTopic += _server_ClientSubscribedTopic ;
            _server.ClientUnsubscribedTopic += _server_ClientUnsubscribedTopic ;
            _server.Started += _server_Started ;
            _server.Stopped += _server_Stopped ;
            _server.StartAsync(options);
        }

        /// <summary>
        /// 服务端停止
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _server_Stopped(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// 服务端启动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _server_Started(object sender, EventArgs e)
        {

        }

        private void _server_ClientUnsubscribedTopic(object sender, MqttClientUnsubscribedTopicEventArgs e)
        {

        }

        private void _server_ClientSubscribedTopic(object sender, MqttClientSubscribedTopicEventArgs e)
        {

        }

        /// <summary>
        /// 客户端接收消息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _server_ApplicationMessageReceived(object sender, MQTTnet.MqttApplicationMessageReceivedEventArgs e)
        {

        }

        /// <summary>
        /// 客户端与服务端断开连接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _server_ClientDisconnected(object sender, MqttClientDisconnectedEventArgs e)
        {

        }

        /// <summary>
        /// 客户端与服务端建立连接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _server_ClientConnected(object sender, MqttClientConnectedEventArgs e)
        {

        }
        #endregion

        #region 启动/停止
        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUrl.Text))
            {
                WriteToStatus("地址不能为空！");
            }
            else
            {
                InitServer(txtUrl.Text);
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (_server != null)
            {
                _server.StopAsync();
            }
        } 
        #endregion

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

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {

        }

    }
}
