/*
 * Copyright (c) 2023-2023 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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

using System.Threading;
using IoT.Device.Demo;
using IoT.Gateway.Demo;

namespace IoT.Device.Feature.Test
{
    class Program
    {
        private static ManualResetEvent mre = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            string serverUri = "iot-mqtts.cn-north-4.myhuaweicloud.com";
            string deviceId = "5eb4cd4049a5ab087d7d4861_demo";
            string deviceX509Id = "5eb4cd4049a5ab087d7d4861_x509_demo";
            string deviceSecret = "secret";

            string bootstrapUri = "iot-bs.cn-north-4.myhuaweicloud.com";
            string bDeviceId = "5eb4cd4049a5ab087d7d4861_lfd_test1343";
            string bDeviceX509Id = "5eb4cd4049a5ab087d7d4861_lfd_test_x509_1121";
            string bSecret = "7fa51ddf6f1b2c99ee20";

            ////MessageSample ms = new MessageSample();
            ////ms.FunMessageSample(serverUri, 1883, deviceId, deviceSecret);

            ////PropertySample ps = new PropertySample();
            ////ps.FunPropertySample(serverUri, 1883, deviceId, deviceSecret);

            ////CommandSample cs = new CommandSample();
            ////cs.FunCommandSample(serverUri, 8883, deviceId, deviceSecret);

            ////X509CertificateDeviceSample cd = new X509CertificateDeviceSample();
            ////cd.FunCertificateSample(serverUri, 8883, deviceX509Id);

            ////DeviceShadowSample ds = new DeviceShadowSample();
            ////ds.FunDeviceShadowSample(serverUri, 8883, deviceX509Id);

            ////PropertiesGetAndSetSample pgss = new PropertiesGetAndSetSample();
            ////pgss.FunPropertiesSample(serverUri, 8883, deviceId, deviceSecret);

            ////OTASample os = new OTASample();
            ////os.FunOTASample(serverUri, 8883, deviceId, deviceSecret);

            ////TimeSyncSample ts = new TimeSyncSample();
            ////ts.FunTimeSyncSample(serverUri, 1883, deviceId, deviceSecret);

            ////SmokeDetector sd = new SmokeDetector();
            ////sd.FunSmokeDetector(serverUri, 8883, deviceId, deviceSecret);

            ////new StringTcpServer(serverUri, 8883, deviceId, deviceSecret);

            ////BootstrapSample bs = new BootstrapSample();
            ////bs.FunBootstrapSample(bootstrapUri, 8883, bDeviceId, bSecret);

            ////BootsrapSelfRegSample bsr = new BootsrapSelfRegSample();
            ////bsr.FunBootsrapSelfRegSample(bootstrapUri, 8883, bDeviceX509Id);

            ////BootsrapGroupRegSample bgrs = new BootsrapGroupRegSample();
            ////bgrs.FunBootsrapGroupRegSample(bootstrapUri, 8883, "yourDeviceId", "yourScopeId");
            
            mre.WaitOne();
        }
    }
}
