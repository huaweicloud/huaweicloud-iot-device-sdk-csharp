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
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using IoT.SDK.Device.Exceptions;
using NLog;

namespace IoT.SDK.Device.Utils
{
    /// <summary>
    /// IOT工具类
    /// </summary>
    public class IotUtil
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 从topic里解析出requestId
        /// </summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        public static string GetRequestId(string topic)
        {
            string requestId = string.Empty;
            try
            {
                if (!string.IsNullOrEmpty(topic) && topic.Contains("=") && (topic.Length - 1) != topic.IndexOf('='))
                {
                    requestId = topic.Substring(topic.IndexOf('=') + 1);
                }
                else
                {
                    throw new InternalException(BaseExceptionEnum.BASE_TOPIC_INVALID_NO_REQUEST_ID, "topic is invalid, no request id.");
                }
            }
            catch (Exception ex)
            {
                Log.Error("SDK.Error: Get request id failed, the topic is " + topic);
            }

            return requestId;
        }
        
        /// <summary>
        /// 获取证书
        /// </summary>
        /// <param name="path">证书路径</param>
        /// <returns></returns>
        public static X509Certificate GetCert(string path)
        {
            string certPath = GetRootDirectory() + path;
            X509Certificate2 crt = new X509Certificate2(certPath);
            return crt;
        }

        /// <summary>
        /// 获取当前事件时间
        /// </summary>
        /// <returns>当前事件时间</returns>
        public static string GetEventTime()
        {
            DateTime dt = TimeZoneInfo.ConvertTimeToUtc(DateTime.Now, TimeZoneInfo.Local);
            return dt.ToString("yyyyMMdd'T'HHmmss'Z'");
        }

        /// <summary>
        /// 获取当前时间戳
        /// </summary>
        /// <returns>时间戳字符串</returns>
        public static string GetTimeStamp()
        {
            TimeSpan timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(timeSpan.TotalMilliseconds).ToString();
        }

        public static string GetSHA256HashFromFile(string file)
        {
            byte[] checksum = null;
            using (FileStream stream = File.OpenRead(file))
            {
                var sha = new SHA256Managed();
                checksum = sha.ComputeHash(stream);
            }

            return BitConverter.ToString(checksum).Replace("-", string.Empty).ToLower();
        }

        /// <summary>
        /// 从deviceid解析nodeId
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <returns>设备物理标识</returns>
        public static String GetNodeIdFromDeviceId(string deviceId)
        {
            try
            {
                return deviceId.Substring(deviceId.IndexOf("_") + 1);
            }
            catch (Exception ex)
            {
                Log.Error("SDK.Error: get node id from device id failed, the device id is " + deviceId);

                return null;
            }
        }

        /// <summary>
        /// 获取程序启动路径
        /// </summary>
        /// <returns></returns>
        public static string GetRootDirectory()
        {
            return Directory.GetCurrentDirectory();
        }
    }
}
