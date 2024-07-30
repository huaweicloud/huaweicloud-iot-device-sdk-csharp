/*
 * Copyright (c) 2020-2024 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without modification,
 * are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice, this list of
 *    conditions and the following disclaimer.
 *
 * 2. Redistributions in binary form must reproduce the above copyright notice, this list
 *    of conditions and the following disclaimer in the documentation and/or other materials
 *    provided with the distribution.
 *
 * 3. Neither the name of the copyright holder nor the names of its contributors may be used
 *    to endorse or promote products derived from this software without specific prior written
 *    permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
 * PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS;
 * OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
 * OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using IoT.SDK.Device.Exceptions;
using NLog;

namespace IoT.SDK.Device.Utils
{
    /// <summary>
    /// Provides an IoT utility class.
    /// </summary>
    public class IotUtil
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static Mutex mutex = new Mutex(false);

        /// <summary>
        /// Obtains the request ID from a topic.
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
                    throw new InternalException(BaseExceptionEnum.BASE_TOPIC_INVALID_NO_REQUEST_ID,
                        "topic is invalid, no request id.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "SDK.Error: Get request id failed, topic:{}", topic);
            }

            return requestId;
        }

        /// <summary>
        /// Obtains a certificate.
        /// </summary>
        /// <param name="path">Indicates the certificate path.</param>
        /// <returns></returns>
        public static X509Certificate GetCert(string path)
        {
            string certPath = Path.Join(GetRootDirectory(), path);
            X509Certificate2 crt = new X509Certificate2(certPath);
            return crt;
        }

        /// <summary>
        /// Obtains the current event time.
        /// </summary>
        /// <returns>Returns the event time.</returns>
        public static string GetEventTime()
        {
            DateTime dt = TimeZoneInfo.ConvertTimeToUtc(DateTime.Now, TimeZoneInfo.Local);
            return dt.ToString("yyyyMMdd'T'HHmmss'Z'");
        }

        /// <summary>
        /// Obtains the current timestamp.
        /// </summary>
        /// <returns>Returns a string reprensenting the timestamp.</returns>
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
        /// Obtains the node ID from a device ID.
        /// </summary>
        /// <param name="deviceId">Indicates the device ID.</param>
        /// <returns>Returns the node ID.</returns>
        public static string GetNodeIdFromDeviceId(string deviceId)
        {
            try
            {
                return deviceId.Substring(deviceId.IndexOf("_") + 1);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "SDK.Error: get node id from device id failed, device id:{}", deviceId);

                return null;
            }
        }

        /// <summary>
        /// Obtains the root directory of the SDK.
        /// </summary>
        /// <returns></returns>
        public static string GetRootDirectory()
        {
            return Directory.GetCurrentDirectory();
        }

        public static string ReadJsonFile(string path)
        {
            mutex.WaitOne();
            try
            {
                using var streamReader = new StreamReader(path);
                return streamReader.ReadToEnd();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "read json file fail");
            }
            finally
            {
                mutex.ReleaseMutex();
            }

            return string.Empty;
        }

        public static void WriteJsonFile(string path, string content)
        {
            mutex.WaitOne();
            try
            {
                var file = new FileInfo(path);
                file.Directory?.Create();
                File.WriteAllText(file.FullName, content);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "write json file fail");
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Obtains the device ID from a topic
        /// </summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        public static string GetDeviceId(string topic)
        {
            string deviceId = string.Empty;
            try
            {
                if (!string.IsNullOrEmpty(topic) && topic.Contains("/devices/"))
                {
                    string[] split = topic.Split(new string[] { "/devices/" }, StringSplitOptions.RemoveEmptyEntries);
                    int length = split[1].IndexOf('/');
                    if (length == 0)
                    {
                        throw new InternalException(BaseExceptionEnum.BASE_TOPIC_INVALID_NO_DEVICE_ID,
                            "topic is invalid, no device id.");
                    }

                    deviceId = split[1].Substring(0, length);
                }
                else
                {
                    throw new InternalException(BaseExceptionEnum.BASE_TOPIC_INVALID_NO_DEVICE_ID,
                        "topic is invalid, no device id.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "SDK.Error: Get device id failed, topic:{}", topic);
            }

            return deviceId;
        }
    }
}