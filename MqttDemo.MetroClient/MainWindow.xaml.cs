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
using MQTTnet;
using MQTTnet.Client;
using System.ComponentModel;
using MahApps.Metro.Controls.Dialogs;

namespace MqttDemo.MetroClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private MainWindowModel _model;
        private IMqttClient _client;
        public MainWindow()
        {
            InitializeComponent();
            _model = new MainWindowModel()
            {
                AllTopics = InitTopics(),
                SelectedTopics = new List<TopicFilter>(),
                ServerUri = "127.0.0.1",
                CurrentTopic = null,
                ServerPort=61613,
                ClientID = Guid.NewGuid().ToString("N")
            };
            this.DataContext = _model;
        }

        #region 订阅主题面板
        /// <summary>
        /// 打开订阅主题面板
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSub_Click(object sender, RoutedEventArgs e)
        {
            this.flySub.IsOpen = !this.flySub.IsOpen;
        }
        #endregion

        #region 用户配置面板
        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            this.flyLogin.IsOpen = !this.flyLogin.IsOpen;
        }
        #endregion

        #region 数据初始化
        /// <summary>
        /// 数据初始化
        /// </summary>
        /// <returns></returns>
        private List<TopicModel> InitTopics()
        {
            List<TopicModel> topics = new List<TopicModel>();
            topics.Add(new TopicModel("/environ/temp", "环境-温度"));
            topics.Add(new TopicModel("/environ/hum", "环境-湿度"));
            //topics.Add(new TopicModel("/environ/pm25", "环境-PM2.5"));
            //topics.Add(new TopicModel("/environ/CO2", "环境-二氧化碳"));
            //topics.Add(new TopicModel("/energy/electric", "能耗-电"));
            //topics.Add(new TopicModel("/energy/water", "环境-水"));
            //topics.Add(new TopicModel("/energy/gas", "环境-电"));
            topics.Add(new TopicModel("/data/alarm", "数据-报警"));
            topics.Add(new TopicModel("/data/message", "数据-消息"));
            topics.Add(new TopicModel("/data/notify", "数据-通知"));
            return topics;
        }

        /// <summary>
        /// 数据模型转换
        /// </summary>
        /// <param name="topics"></param>
        /// <returns></returns>
        private List<TopicFilter> ConvertTopics(List<TopicModel> topics)
        {
            //MQTTnet.TopicFilter
            List<TopicFilter> filters = new List<TopicFilter>();
            foreach (TopicModel model in topics)
            {
                TopicFilter filter = new TopicFilter(model.Topic,MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce);
                filters.Add(filter);

            }
            return filters;
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
            if (txtRich.ExtentHeight > 200)
            {
                txtRich.Document.Blocks.Clear();
            }
        }
        #endregion

        #region 更改订阅的主题
        /// <summary>
        /// 保存订阅的主题
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            List<TopicModel> topics = _model.AllTopics.Where(t => t.IsSelected == true).ToList();

            _model.SelectedTopics = ConvertTopics(topics);
            this.flySub.IsOpen = !this.flySub.IsOpen;
            SubscribeTopics(_model.SelectedTopics);
        }
        #endregion

        #region 订阅主题
        private void SubscribeTopics(List<TopicFilter> filters)
        {
            if (_client!=null)
            {
                _client.SubscribeAsync(filters);
                string tmp = "";
                foreach (var filter in filters)
                {
                    tmp += filter.Topic;
                    tmp += ",";
                }
                if (tmp.Length>1)
                {
                    tmp = tmp.Substring(0, tmp.Length - 1);
                }
                WriteToStatus("成功订阅主题："+tmp);
            }
            else
            {
                ShowDialog("提示", "请连接服务端后订阅主题！");
            }
        }
        #endregion

        #region 连接/断开服务端
        /// <summary>
        /// 连接服务端
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (_model.ServerUri!=null&&_model.ServerPort>0)
            {
                InitClient(_model.ClientID, _model.ServerUri, _model.ServerPort);
            }
            else
            {
                ShowDialog("提示", "服务端地址或端口号不能为空！");
            }
        }
        /// <summary>
        /// 断开服务端
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (_client != null)
            {
                _client.DisconnectAsync();
            }
        }
        #endregion

        #region MQTT方法
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="id"></param>
        /// <param name="url"></param>
        /// <param name="port"></param>
        private void InitClient(string id,string url = "127.0.0.1", int port = 1883)
        {
            var options = new MqttClientOptions()
            {
                ClientId = id
            };
            options.ChannelOptions = new MqttClientTcpOptions()
            {
                Server = url,
                Port = port
            };
            options.Credentials = new MqttClientCredentials()
            {
                Username=_model.UserName,
                Password=_model.Password
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
            _model.IsConnected = false;
            _model.IsDisConnected = true;
            WriteToStatus("与服务端断开连接！");
        }

        /// <summary>
        /// 客户端与服务端建立连接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void _client_Connected(object sender, MqttClientConnectedEventArgs e)
        {
            _model.IsConnected = true;
            _model.IsDisConnected = false;
            WriteToStatus("与服务端建立连接");
        }

        /// <summary>
        /// 客户端收到消息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _client_ApplicationMessageReceived(object sender, MQTTnet.MqttApplicationMessageReceivedEventArgs e)
        {
            WriteToStatus("收到来自客户端" + e.ClientId + "，主题为" + e.ApplicationMessage.Topic + "的消息：" + Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
        }
        #endregion

        #region 发布消息
        /// <summary>
        /// 发布主题
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPublish_Click(object sender, RoutedEventArgs e)
        {
            if (_client!=null)
            {
                if (this.comboTopics.SelectedIndex<0)
                {
                    ShowDialog("提示", "请选择要发布的主题！");
                    return;
                }
                if (string.IsNullOrEmpty(txtContent.Text))
                {
                    ShowDialog("提示", "消息内容不能为空！");
                    return;
                }
                string topic = comboTopics.SelectedValue as string;
                string content = txtContent.Text;
                MqttApplicationMessage msg = new MqttApplicationMessage
                {
                    Topic=topic,
                    Payload=Encoding.UTF8.GetBytes(content),
                    QualityOfServiceLevel=MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce,
                    Retain=false
                };
                _client.PublishAsync(msg);
                WriteToStatus("成功发布主题为" + topic+"的消息！");
            }
            else
            {
                ShowDialog("提示", "请连接服务端后发布消息！");
                return;
            }
        }
        #endregion

        #region 提示框
        public void ShowDialog(string title, string content)
        {
            _showDialog(title, content);
        }
        /// <summary>
        /// 提示框
        /// </summary>
        /// <param name="title"></param>
        /// <param name="content"></param>
        private void _showDialog(string title, string content)
        {
            var mySetting = new MetroDialogSettings()
            {
                AffirmativeButtonText = "确定",
                //NegativeButtonText = "Go away!",
                //FirstAuxiliaryButtonText = "Cancel",
                ColorScheme = this.MetroDialogOptions.ColorScheme
            };

            MessageDialogResult result = this.ShowModalMessageExternal(title, content, MessageDialogStyle.Affirmative, mySetting);
        }



        #endregion

        private void btnSaveConfig_Click(object sender, RoutedEventArgs e)
        {

        }
    }

    public class MainWindowModel: INotifyPropertyChanged
    {
        private List<TopicModel> _allTopics;

        public List<TopicModel> AllTopics
        {
            get { return _allTopics; }
            set
            {
                if (_allTopics!=value)
                {
                    _allTopics = value;
                    OnPropertyChanged("AllTopics");
                }
            }
        }

        private List<TopicFilter> _selectedTopics;

        public List<TopicFilter> SelectedTopics
        {
            get { return _selectedTopics; }
            set
            {
                if (_selectedTopics!=value)
                {
                    _selectedTopics = value;
                    OnPropertyChanged("SelectedTopics");
                }
            }
        }

        private string _serverUri;

        public string ServerUri
        {
            get { return _serverUri; }
            set
            {
                if (_serverUri!=value)
                {
                    _serverUri = value;
                    OnPropertyChanged("ServerUri");
                }
            }
        }

        private int _serverPort;

        public int ServerPort
        {
            get { return _serverPort; }
            set
            {
                if (_serverPort!=value)
                {
                    _serverPort = value;
                    OnPropertyChanged("ServerPort");
                }
            }
        }


        private string _clientId;

        public string ClientID
        {
            get { return _clientId; }
            set
            {
                if (_clientId!=value)
                {
                    _clientId = value;
                    OnPropertyChanged("ClientID");
                }
            }
        }

        private TopicFilter _currentTopic;

        public TopicFilter CurrentTopic
        {
            get { return _currentTopic; }
            set
            {
                if (_currentTopic!=value)
                {
                    _currentTopic = value;
                    OnPropertyChanged("CurrentTopic");
                }
            }
        }

        private bool? _isConnected=false;

        public bool? IsConnected
        {
            get { return _isConnected; }
            set
            {
                if (_isConnected!=value)
                {
                    _isConnected = value;
                    OnPropertyChanged("IsConnected");
                }
            }
        }

        private bool _isDisConnected=true;

        public bool IsDisConnected
        {
            get { return _isDisConnected; }
            set
            {
                if (_isDisConnected != value)
                {
                    _isDisConnected = value;
                    this.OnPropertyChanged("IsDisConnected");
                }
            }
        }

        private string _userName="admin";

        public string UserName
        {
            get { return _userName; }
            set
            {
                if (_userName != value)
                {
                    _userName = value;
                    this.OnPropertyChanged("UserName");
                }

            }
        }

        private string _password="password";

        public string Password
        {
            get { return _password; }
            set
            {
                if (_password != value)
                {
                    _password = value;
                    this.OnPropertyChanged("Password");
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

    public class TopicModel: INotifyPropertyChanged
    {
        public TopicModel()
        {

        }
        public TopicModel(string topic,string describe)
        {
            _isSelected = false;
            _topic = topic;
            _describe = describe;
        }
        private bool? _isSelected;

        public bool? IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected!=value)
                {
                    _isSelected = value;
                    OnPropertyChanged("IsSelected");
                }
            }
        }

        private string _topic;


        public string Topic
        {
            get { return _topic; }
            set
            {
                if (_topic!=value)
                {
                    _topic = value;
                    OnPropertyChanged("Topic");
                }
            }
        }

        private string _describe;

        public string Describe
        {
            get { return _describe; }
            set
            {
                if (_describe!=value)
                {
                    _describe = value;
                    OnPropertyChanged("Describe");
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

}
