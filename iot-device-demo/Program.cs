/*
 * Copyright (c) 2023-2024 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using IoT.Device.Demo;
using IoT.Device.Demo.Core;
using IoT.Device.Demo.Gateway;
using IoT.Device.Demo.HubDevice.Bridge;
using Newtonsoft.Json;

namespace IoT.Device.Feature.Test
{
    class Program
    {
        private static readonly ManualResetEvent MRE = new ManualResetEvent(false);

        public static readonly Dictionary<string, AbstractFeatureTestSample> DEVICE_DEMOS =
            new Dictionary<string, AbstractFeatureTestSample>
            {
                { "Command", new CommandSample() },
                { "DeviceRule", new DeviceRuleSample() },
                { "DeviceInfoReport", new DeviceInfoReportSample() },
                { "DeviceReportLog", new DeviceReportLogSample() },
                { "DeviceShadow", new DeviceShadowSample() },
                { "FileUploadDownload", new FileUploadDownloadSample() },
                { "Message", new MessageReportReceiveSample() },
                { "OTA", new OtaSample() },
                { "PropertyGetAndSet", new PropertyGetAndSetSample() },
                { "PropertyReport", new PropertyReportSample() },
                { "TimeSync", new TimeSyncSample() },
                { "SecurityDetection", new SecurityDetectionSample() },
                { "SmokeDetector", new SmokeDetector() },
                { "Gateway", new GatewayTcpServer() },
                { "Bootstrap", new BootstrapSample() },
                { "DeviceConfig", new DeviceConfigSample() },
                { "Bridge", new BridgeTcpServer() },
                { "Reconnect", new ReconnectSample() },
                { "MessageRetransmit", new MessageRetransmitSample() }
            };

        public static void RunDemo(DemoConfig demoConfig)
        {
            if (demoConfig.DemoName != null && DEVICE_DEMOS.TryGetValue(demoConfig.DemoName, out var demo))
            {
                demo.Start(demoConfig);
            }
            else
            {
                throw new Exception(
                    $"no such demo named \"{demoConfig.DemoName}\", supported demo names are:{string.Join(',', DEVICE_DEMOS.Keys)}");
            }
        }

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("    dotnet run --project csproj config");
                Console.WriteLine("");
                Console.WriteLine("Example:");
                Console.WriteLine(
                    "    dotnet run --project .\\iot-device-feature-test.csproj config\\DemoConfigDefault.json");
                Console.WriteLine(
                    "    dotnet run --project .\\iot-device-feature-test.csproj config\\DemoBoostrapConfig.json");
                Console.WriteLine("");
                Console.WriteLine("Available Demonstration name are: ");
                Console.WriteLine($"    {string.Join(", ", DEVICE_DEMOS.Keys)}");
            }
            else
            {
                DemoConfig demoConfig;
                using (var r = new StreamReader(args[0]))
                {
                    var json = r.ReadToEnd();
                    demoConfig = JsonConvert.DeserializeObject<DemoConfig>(json);
                }

                RunDemo(demoConfig);

                MRE.WaitOne();
            }
        }
    }
}