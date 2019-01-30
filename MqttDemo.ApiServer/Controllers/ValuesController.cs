using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace MqttDemo.ApiServer.Controllers
{
    [Route("v1/api/values")]
    [ApiController]
    public class ValuesController : RootController
    {

        public string Test()
        {
            string result = "";
            //从服务集合中获取MQTT服务
            var service = ServiceLocator.Instance.GetService(typeof(MQTTnet.Server.MqttServer));
            var messager = (MQTTnet.Server.MqttServer)service;
            
            //这里你可以构建消息并发布

            return result;
        }
    }
}
