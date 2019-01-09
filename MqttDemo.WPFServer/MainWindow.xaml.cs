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
using MahApps.Metro.Controls;
using System.ComponentModel;
using MQTTnet.Server;
using MQTTnet.Adapter;
using MQTTnet.Protocol;
using MQTTnet;
using System.Collections.ObjectModel;

namespace MqttDemo.WPFServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private MainWindowModel _model;
        private IMqttServer server;

        public MainWindow()
        {
            InitializeComponent();
            _model = new MainWindowModel();
            this.DataContext = _model;
        }

        #region 启动按钮事件
        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            var optionBuilder = new MqttServerOptionsBuilder().WithDefaultEndpointBoundIPAddress(System.Net.IPAddress.Parse(_model.HostIP)).WithDefaultEndpointPort(_model.HostPort).WithDefaultCommunicationTimeout(TimeSpan.FromMilliseconds(_model.Timeout)).WithConnectionValidator(t =>
            {
                if (t.Username!=_model.UserName||t.Password!=_model.Password)
                {
                    t.ReturnCode = MqttConnectReturnCode.ConnectionRefusedBadUsernameOrPassword;
                }
                t.ReturnCode = MqttConnectReturnCode.ConnectionAccepted;
            });
            var option = optionBuilder.Build();

            server = new MqttFactory().CreateMqttServer();
            server.ApplicationMessageReceived += Server_ApplicationMessageReceived;//绑定消息接收事件
            server.ClientConnected += Server_ClientConnected;//绑定客户端连接事件
            server.ClientDisconnected += Server_ClientDisconnected;//绑定客户端断开事件
            server.ClientSubscribedTopic += Server_ClientSubscribedTopic;//绑定客户端订阅主题事件
            server.ClientUnsubscribedTopic += Server_ClientUnsubscribedTopic;//绑定客户端退订主题事件
            server.Started += Server_Started;//绑定服务端启动事件
            server.Stopped += Server_Stopped;//绑定服务端停止事件
            //启动
            await server.StartAsync(option);

        }


        #endregion

        #region 停止按钮事件
        private async void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (server != null)
            {
                await server.StopAsync();
            }
        }
        #endregion

        #region 服务端停止事件
        private void Server_Stopped(object sender, EventArgs e)
        {
            WriteToStatus("服务端已停止！");
        }
        #endregion

        #region 服务端启动事件
        private void Server_Started(object sender, EventArgs e)
        {
            WriteToStatus("服务端已启动！");
        }
        #endregion

        #region 客户端退订主题事件
        private void Server_ClientUnsubscribedTopic(object sender, MqttClientUnsubscribedTopicEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (_model.AllTopics.Any(t => t.Topic == e.TopicFilter))
                {
                    TopicModel model = _model.AllTopics.First(t => t.Topic == e.TopicFilter);
                    _model.AllTopics.Remove(model);
                    model.Clients.Remove(e.ClientId);
                    model.Count--;
                    if (model.Count > 0)
                    {
                        _model.AllTopics.Add(model);
                    }
                }
            });
            WriteToStatus("客户端" + e.ClientId + "退订主题" + e.TopicFilter);
        }
        #endregion

        #region 客户端订阅主题事件
        private void Server_ClientSubscribedTopic(object sender, MqttClientSubscribedTopicEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (_model.AllTopics.Any(t => t.Topic == e.TopicFilter.Topic))
                {
                    TopicModel model = _model.AllTopics.First(t => t.Topic == e.TopicFilter.Topic);
                    _model.AllTopics.Remove(model);
                    model.Clients.Add(e.ClientId);
                    model.Count++;
                    _model.AllTopics.Add(model);
                }
                else
                {
                    TopicModel model = new TopicModel(e.TopicFilter.Topic, e.TopicFilter.QualityOfServiceLevel)
                    {
                        Clients = new List<string> { e.ClientId },
                        Count = 1
                    };
                    _model.AllTopics.Add(model);
                }
            });

            WriteToStatus("客户端" + e.ClientId + "订阅主题" + e.TopicFilter.Topic);
        }
        #endregion

        #region 客户端断开事件
        private void Server_ClientDisconnected(object sender, MqttClientDisconnectedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                _model.AllClients.Remove(e.ClientId);
                var query = _model.AllTopics.Where(t => t.Clients.Contains(e.ClientId));
                if (query.Any())
                {
                    var tmp = query.ToList();
                    foreach (var model in tmp)
                    {
                        _model.AllTopics.Remove(model);
                        model.Clients.Remove(e.ClientId);
                        model.Count--;
                        _model.AllTopics.Add(model);
                    }
                }
            });


            WriteToStatus("客户端" + e.ClientId + "断开");
        }
        #endregion

        #region 客户端连接事件
        private void Server_ClientConnected(object sender, MqttClientConnectedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                _model.AllClients.Add(e.ClientId);
            });
            WriteToStatus("客户端" + e.ClientId + "连接");
        }
        #endregion

        #region 收到消息事件
        private void Server_ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            if (e.ApplicationMessage.Topic == "/environ/temp")
            {
                string str = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                double tmp;
                bool isdouble = double.TryParse(str, out tmp);
                if (isdouble)
                {
                    string result = "";
                    if (tmp > 40)
                    {
                        result = "温度过高！";
                    }
                    else if (tmp < 10)
                    {
                        result = "温度过低！";
                    }
                    else
                    {
                        result = "温度正常！";
                    }
                    MqttApplicationMessage message = new MqttApplicationMessage()
                    {
                        Topic = e.ApplicationMessage.Topic,
                        Payload = Encoding.UTF8.GetBytes(result),
                        QualityOfServiceLevel = e.ApplicationMessage.QualityOfServiceLevel,
                        Retain = e.ApplicationMessage.Retain
                    };
                    server.PublishAsync(message);
                }
            }
            WriteToStatus("收到消息" + e.ApplicationMessage.ConvertPayloadToString() + ",来自客户端" + e.ClientId + ",主题为" + e.ApplicationMessage.Topic);
        }
        #endregion

        #region 打开配置页面
        private void btnConfig_Click(object sender, RoutedEventArgs e)
        {
            this.flyConfig.IsOpen = !this.flyConfig.IsOpen;
        }
        #endregion

        #region 增加主题按钮事件
        private void btnAddTopic_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_model.AddTopic)&&server!=null)
            {
                TopicModel topic = new TopicModel(_model.AddTopic, MqttQualityOfServiceLevel.AtLeastOnce);
                foreach (string clientId in _model.AllClients)
                {
                    server.SubscribeAsync(clientId, new List<TopicFilter> { topic });
                }
                _model.AllTopics.Add(topic);
            }
        }
        #endregion

        #region 清理内容
        private void menuClear_Click(object sender, RoutedEventArgs e)
        {
            txtRich.Document.Blocks.Clear();
        }
        #endregion

        #region 状态输出
        /// <summary>
        /// 状态输出
        /// </summary>
        /// <param name="message"></param>
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

        }
        #endregion


    }

    public class MainWindowModel : INotifyPropertyChanged
    {
        public MainWindowModel()
        {
            hostIP = "127.0.0.1";//绑定的IP地址
            hostPort = 12345;//绑定的端口号
            timeout = 3000;//连接超时时间
            username = "admin";//用户名
            password = "password";//密码
            allTopics = new ObservableCollection<TopicModel>();//主题
            allClients = new ObservableCollection<string>();//客户端
            addTopic = "";
        }

        private ObservableCollection<TopicModel> allTopics;

        public ObservableCollection<TopicModel> AllTopics
        {
            get { return allTopics; }
            set
            {
                if (allTopics != value)
                {
                    allTopics = value;
                    this.OnPropertyChanged("AllTopics");
                }

            }
        }

        private ObservableCollection<string> allClients;

        public ObservableCollection<string> AllClients
        {
            get { return allClients; }
            set
            {
                if (allClients != value)
                {
                    allClients = value;
                    this.OnPropertyChanged("AllClients");
                }

            }
        }


        private string hostIP;

        public string HostIP
        {
            get { return hostIP; }
            set
            {
                if (hostIP != value)
                {
                    hostIP = value;
                    this.OnPropertyChanged("HostIP");
                }

            }
        }

        private int hostPort;

        public int HostPort
        {
            get { return hostPort; }
            set
            {
                if (hostPort != value)
                {
                    hostPort = value;
                    this.OnPropertyChanged("HostPort");
                }

            }
        }

        private int timeout;

        public int Timeout
        {
            get { return timeout; }
            set
            {
                if (timeout != value)
                {
                    timeout = value;
                    this.OnPropertyChanged("Timeout");
                }

            }
        }

        private string username;

        public string UserName
        {
            get { return username; }
            set
            {
                if (username != value)
                {
                    username = value;
                    this.OnPropertyChanged("UserName");
                }

            }
        }


        private string password;

        public string Password
        {
            get { return password; }
            set
            {
                if (password != value)
                {
                    password = value;
                    this.OnPropertyChanged("Password");
                }

            }
        }

        private string addTopic;

        public string AddTopic
        {
            get { return addTopic; }
            set
            {
                if (addTopic != value)
                {
                    addTopic = value;
                    this.OnPropertyChanged("AddTopic");
                }

            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TopicModel : TopicFilter,INotifyPropertyChanged
    {
        public TopicModel(string topic, MqttQualityOfServiceLevel qualityOfServiceLevel) : base(topic, qualityOfServiceLevel)
        {
            clients = new List<string>();
            count = 0;
        }

        private int count;
        /// <summary>
        /// 订阅此主题的客户端数量
        /// </summary>
        public int Count
        {
            get { return count; }
            set
            {
                if (count != value)
                {
                    count = value;
                    this.OnPropertyChanged("Count");
                }

            }
        }

        private List<string> clients;
        /// <summary>
        /// 订阅此主题的客户端
        /// </summary>
        public List<string> Clients
        {
            get { return clients; }
            set
            {
                if (clients != value)
                {
                    clients = value;
                    this.OnPropertyChanged("Clients");
                }

            }
        }


        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
