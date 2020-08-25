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

using System;
using System.Collections.Generic;
using System.Threading;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Service;
using IoT.SDK.Device.Utils;
using NLog;

namespace IoT.SDK.Device.OTA
{
    /// <summary>
    /// OTA服务类，提供设备升级相关接口，使用方法：
    /// </summary>
    public class OTAService : AbstractService
    {
        // 升级上报的错误码，用户也可以扩展自己的错误码
        public static readonly int OTA_CODE_SUCCESS = 0; // 成功
        public static readonly int OTA_CODE_BUSY = 1; // 设备使用中
        public static readonly int OTA_CODE_SIGNAL_BAD = 2; // 信号质量差
        public static readonly int OTA_CODE_NO_NEED = 3; // 已经是最新版本
        public static readonly int OTA_CODE_LOW_POWER = 4; // 电量不足
        public static readonly int OTA_CODE_LOW_SPACE = 5; // 剩余空间不足
        public static readonly int OTA_CODE_DOWNLOAD_TIMEOUT = 6; // 下载超时
        public static readonly int OTA_CODE_CHECK_FAIL = 7; // 升级包校验失败
        public static readonly int OTA_CODE_UNKNOWN_TYPE = 8; // 升级包类型不支持
        public static readonly int OTA_CODE_LOW_MEMORY = 9; // 内存不足
        public static readonly int OTA_CODE_INSTALL_FAIL = 10; // 安装升级包失败
        public static readonly int OTA_CODE_INNER_ERROR = 255; // 内部异常

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private OTAListener otaListener;

        /// <summary>
        /// 设置OTA监听器
        /// </summary>
        /// <param name="otaListener">OTA监听器</param>
        public void SetOtaListener(OTAListener otaListener)
        {
            this.otaListener = otaListener;
        }

        /// <summary>
        /// 接收OTA事件处理
        /// </summary>
        /// <param name="deviceEvent">服务事件</param>
        public override void OnEvent(DeviceEvent deviceEvent)
        {
            if (otaListener == null)
            {
                Log.Info("otaListener is null");
                return;
            }

            if (deviceEvent.eventType == "version_query")
            {
                otaListener.OnQueryVersion();
            }
            else if (deviceEvent.eventType == "firmware_upgrade" || deviceEvent.eventType == "software_upgrade")
            {
                OTAPackage pkg = JsonUtil.ConvertDicToObject<OTAPackage>(deviceEvent.paras);

                // OTA单独起一个线程处理
                new Thread(new ThreadStart(new Action(() =>
                {
                    // to do 启动新线程要执行的代码
                    otaListener.OnNewPackage(pkg);
                }))).Start();
            }
        }

        /// <summary>
        /// 上报固件版本信息
        /// </summary>
        /// <param name="version">固件版本</param>
        public void ReportVersion(string version)
        {
            Dictionary<string, object> node = new Dictionary<string, object>();
            
            node.Add("fw_version", version);
            node.Add("sw_version", version);

            DeviceEvent deviceEvent = new DeviceEvent();
            deviceEvent.eventType = "version_report";
            deviceEvent.paras = node;
            deviceEvent.serviceId = "$ota";
            deviceEvent.eventTime = IotUtil.GetEventTime();

            iotDevice.GetClient().ReportEvent(deviceEvent);
        }

        /// <summary>
        /// 上报升级状态
        /// </summary>
        /// <param name="result">升级结果</param>
        /// <param name="progress">升级进度0-100</param>
        /// <param name="version">当前版本</param>
        /// <param name="description">具体失败的原因，可选参数</param>
        public void ReportOtaStatus(int result, int progress, string version, string description)
        {
            Dictionary<string, object> node = new Dictionary<string, object>();
            node.Add("result_code", result);
            node.Add("progress", progress);
            if (description != null)
            {
                node.Add("description", description);
            }

            node.Add("version", version);

            DeviceEvent deviceEvent = new DeviceEvent();
            deviceEvent.eventType = "upgrade_progress_report";
            deviceEvent.paras = node;
            deviceEvent.serviceId = "$ota";
            deviceEvent.eventTime = IotUtil.GetEventTime();

            iotDevice.GetClient().ReportEvent(deviceEvent);
        }   
    }
}
