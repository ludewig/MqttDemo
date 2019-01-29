using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MQTTnet.Server;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MqttDemo.ApiServer.Controllers
{
    public class RootController : Controller
    {
        protected MqttServer Messager { get; private set; }

        public RootController()
        {
            var service = ServiceLocator.Instance.GetService(typeof(MqttServer));
            Messager = (MqttServer)service;
            //typeof(MqttServer).IsInstanceOfType(server);
        }
    }

    public static class ServiceLocator
    {
        public static IServiceProvider Instance { get; set; }
    }
}
