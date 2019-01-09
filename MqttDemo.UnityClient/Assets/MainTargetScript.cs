using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Utility;
using uPLibrary.Networking.M2Mqtt.Exceptions;

public class MainTargetScript : MonoBehaviour {

    #region UI对象
    public InputField inputIP;//IP地址输入框
    public InputField inputPort;//端口号输入框
    public Button btnConnect;//连接按钮
    public Button btnDisconnect;//断开按钮
    public Toggle togTopic1;//主题选择框
    public Toggle togTopic2;//主题选择框
    public Toggle togTopic3;//主题选择框
    public Toggle togTopic4;//主题选择框
    public Toggle togTopic5;//主题选择框
    public Button btnSubscribe;//订阅按钮
    public Text txtResult;//结果输出文本框
    public Dropdown dropTopics;//发布主题选择框
    public InputField inputContent;//发布内容
    public Button btnPublish;//发布按钮
    public GameObject target;
    #endregion

    #region 变量
    private string[] allTopics = { "/data/alarm", "/data/message", "/data/notify", "/action/start", "/action/stop" };
    private List<string> selectedTopics;
    private string currentTopic;
    private MqttClient client;
    #endregion

    // Use this for initialization
    void Start () {
        selectedTopics = new List<string> { "/data/alarm", "/data/message", "/data/notify", "/action/start", "/action/stop" };
        btnConnect.onClick.AddListener(btnConnect_Click);
        btnDisconnect.onClick.AddListener(btnDisconnect_Click);
        btnSubscribe.onClick.AddListener(btnSubscribe_Click);
        btnPublish.onClick.AddListener(btnPublish_Click);
        dropTopics.onValueChanged.AddListener(dropTopics_ValueChange);
        togTopic1.onValueChanged.AddListener(togTopic_ValueChange);
        togTopic2.onValueChanged.AddListener(togTopic_ValueChange);
        togTopic3.onValueChanged.AddListener(togTopic_ValueChange);
        togTopic4.onValueChanged.AddListener(togTopic_ValueChange);
        togTopic5.onValueChanged.AddListener(togTopic_ValueChange);

    }

    #region 订阅主题选中事件
    private void togTopic_ValueChange(bool arg0)
    {
        var current = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
        string topic = current.GetComponentInChildren<Text>().text;
        if (arg0)//选中
        {
            selectedTopics.Add(topic);
        }
        else//未选中
        {
            selectedTopics.Remove(topic);
        }

    } 
    #endregion

    #region 发布主题选中事件
    private void dropTopics_ValueChange(int arg0)
    {
        currentTopic = allTopics[arg0];
    } 
    #endregion


    // Update is called once per frame
    void Update () {
		
	}


    #region 发布按钮点击事件
    private void btnPublish_Click()
    {
        string content = inputContent.text;
        if (client!=null&&!string.IsNullOrEmpty(currentTopic)&&!string.IsNullOrEmpty(content))
        {
            client.Publish(currentTopic, System.Text.Encoding.UTF8.GetBytes(content), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
        }
    } 
    #endregion

    #region 订阅按钮点击事件
    private void btnSubscribe_Click()
    {
        if (client!=null&&selectedTopics!=null)
        {
            //Debug.Log(selectedTopics.Count);
            client.Subscribe(new string[] { "/action/start" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }
    } 
    #endregion

    #region 断开按钮点击事件
    private void btnDisconnect_Click()
    {
        if (client!=null)
        {
            client.Disconnect();
        }
    } 
    #endregion

    #region 连接按钮点击事件
    private void btnConnect_Click()
    {
        string txtIP = inputIP.text;
        string txtPort = inputPort.text;
        string clientId = Guid.NewGuid().ToString();
        string username = "admin";
        string password = "password";
        client = new MqttClient(IPAddress.Parse(txtIP), int.Parse(txtPort),false,null);

        client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;
        client.MqttMsgSubscribed += Client_MqttMsgSubscribed;
        client.Connect(clientId, username, password);


    }

    private void Client_MqttMsgSubscribed(object sender, MqttMsgSubscribedEventArgs e)
    {
        Debug.Log("Subscribed"+e.MessageId);
    }

    private void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
    {
        Debug.Log("Topic:"+e.Topic);
        switch (e.Topic)
        {
            case "/data/alarm":
                break;
            case "/data/message":
                break;
            case "/data/notify":
                break;
            case "/action/start":
                target.transform.Rotate(Vector3.up * 30);
                break;
            case "/action/stop":
                target.transform.localEulerAngles = new Vector3(0, 0, 0);
                break;
        }
        string tmp = System.Text.Encoding.UTF8.GetString(e.Message);
        Debug.Log("Message" + tmp);
        //txtResult.text.Insert(0, tmp + "//n");
    }
    #endregion

}
