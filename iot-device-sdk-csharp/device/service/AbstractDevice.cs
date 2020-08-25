/*Copyright (c) <2020>, <Huawei Technologies Co., Ltd>
 * All rights reserved.
 * &Redistribution and use in source and binary forms, with or without modification,
 * are permitted provided that the following conditions are met:
 * 1. Redistributions of source code must retain the above copyright notice, this list of
 * conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice, this list
 * of conditions and the following disclaimer in the documentation and/or other materials
 * provided with the distribution.
 * 3. Neither the name of the copyright holder nor the names of its contributors may be used
 * to endorse or promote products derived from this software without specific prior written permission.
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF
 * USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
 * OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 *
 * */

using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using IoT.SDK.Device.Client;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.OTA;
using IoT.SDK.Device.Timesync;
using IoT.SDK.Device.Transport;
using NLog;

namespace IoT.SDK.Device.Service
{
    /// <summary>
    /// 抽象设备类
    /// </summary>
    public class AbstractDevice
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private DeviceClient client;

        private Dictionary<string, AbstractService> services = new Dictionary<string, AbstractService>();

        private string deviceId;
        
        /// <summary>
        /// 构造函数，使用密码创建设备
        /// </summary>
        /// <param name="serverUri">平台访问地址，比如iot-mqtts.cn-north-4.myhuaweicloud.com</param>
        /// <param name="port">端口</param>
        /// <param name="deviceId">设备id</param>
        /// <param name="deviceSecret">设备密码</param>
        public AbstractDevice(string serverUri, int port, string deviceId, string deviceSecret)
        {
            ClientConf clientConf = new ClientConf();
            clientConf.ServerUri = serverUri;
            clientConf.Port = port;
            clientConf.DeviceId = deviceId;
            clientConf.Secret = deviceSecret;
            this.deviceId = deviceId;
            this.client = new DeviceClient(clientConf, this);
            InitSysServices();
            Log.Info("create device: " + clientConf.DeviceId);
        }

        /// <summary>
        /// 构造函数，使用证书创建设备
        /// </summary>
        /// <param name="serverUri"></param>
        /// <param name="port"></param>
        /// <param name="deviceId"></param>
        /// <param name="deviceCert"></param>
        public AbstractDevice(string serverUri, int port, string deviceId, X509Certificate deviceCert)
        {
            ClientConf clientConf = new ClientConf();
            clientConf.ServerUri = serverUri;
            clientConf.Port = port;
            clientConf.DeviceId = deviceId;
            clientConf.DeviceCert = deviceCert;
            this.deviceId = deviceId;
            this.client = new DeviceClient(clientConf, this);
            this.InitSysServices();
            Log.Info("create device: " + clientConf.DeviceId);
        }

        public OTAService otaService { get; set; }

        public TimeSyncService timeSyncService { get; set; }

        /// <summary>
        /// 添加服务。用户基于AbstractService定义自己的设备服务，并添加到设备
        /// </summary>
        /// <param name="serviceId">服务id，要和设备模型定义一致</param>
        /// <param name="deviceService">服务实例</param>
        public void AddService(string serviceId, AbstractService deviceService)
        {
            deviceService.iotDevice = this;
            deviceService.ServiceId = serviceId;
            services.Add(serviceId, deviceService);
        }

        /// <summary>
        /// 初始化，创建到平台的连接
        /// </summary>
        /// <returns>如果连接成功，返回0；否则返回-1</returns>
        public int Init()
        {
            return client.Connect();
        }

        /// <summary>
        /// 获取设备客户端。获取到设备客户端后，可以直接调用客户端提供的消息、属性、命令等接口
        /// </summary>
        /// <returns>设备客户端实例</returns>
        public DeviceClient GetClient()
        {
            return client;
        }

        /// <summary>
        /// 命令回调函数，由SDK自动调用
        /// </summary>
        /// <param name="requestId">请求ID</param>
        /// <param name="command">命令</param>
        public void OnCommand(string requestId, Command command)
        {
            IService service = this.GetService(command.serviceId);

            if (service != null)
            {
                CommandRsp rsp = service.OnCommand(command);
                client.Report(new PubMessage(requestId, rsp));
            }
        }

        /// <summary>
        /// 查询服务
        /// </summary>
        /// <param name="serviceId">服务ID</param>
        /// <returns>服务实例</returns>
        public AbstractService GetService(string serviceId)
        {
            return services[serviceId];
        }

        /// <summary>
        /// 事件回调，由SDK自动调用
        /// </summary>
        /// <param name="deviceEvents">设备事件</param>
        public void OnEvent(DeviceEvents deviceEvents)
        {
            // 子设备的
            if (deviceEvents.deviceId != null && !(deviceEvents.deviceId == this.deviceId))
            {
                return;
            }

            foreach (DeviceEvent evnt in deviceEvents.services)
            {
                IService deviceService = this.GetService(evnt.serviceId);
                if (deviceService != null)
                {
                    deviceService.OnEvent(evnt);
                }
            }
        }

        /// <summary>
        /// 初始化系统默认service，系统service以$作为开头
        /// </summary>
        private void InitSysServices()
        {
            this.otaService = new OTAService();
            this.AddService("$ota", this.otaService);

            this.timeSyncService = new TimeSyncService();
            this.AddService("$time_sync", timeSyncService);
        }
    }
}
