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
using System.IO;
using System.Net;
using IoT.SDK.Device;
using IoT.SDK.Device.OTA;
using IoT.SDK.Device.Utils;
using NLog;

namespace IoT.Device.Demo
{
    public class OtaSample : DeviceSample
    {
        /// <summary>
        /// Demonstrates how to upgrade devices.
        /// Usage: After creating an upgrade task on the platform, modify the device parameters in the main function and start this sample.
        /// The device receives the upgrade notification, downloads the upgrade package, and reports the upgrade result.
        /// The upgrade result is displayed on the platform.
        /// Prerequisites: \download\ The root directory must contain the download folder (which can be customized as required).
        /// </summary>
        protected override void RunDemo()
        {
            // The package path must contain the software or firmware package name and extension.
            string packageSavePath = IotUtil.GetRootDirectory() + @"\download";
            if (!Directory.Exists(packageSavePath))
            {
                Directory.CreateDirectory(packageSavePath);
            }

            var otaSample = new OTAUpgrade(Device, packageSavePath);
            otaSample.Init();
        }
    }

    public class OTAUpgrade : OTAListener
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        private readonly OTAService otaService;
        private readonly IoTDevice device;

        private string version; // Version
        private readonly string packageSavePath; // Path where the upgrade Package is stored

        public OTAUpgrade(IoTDevice device, string packageSavePath)
        {
            this.device = device;
            otaService = device.otaService;
            otaService.SetOtaListener(this);
            this.packageSavePath = packageSavePath;
            version = "v0.0.1"; // Change to the actual value.
        }

        public int Init()
        {
            return 0;
        }

        public void OnNewPackage(OTAPackage otaPackage)
        {
            LOG.Info("otaPackage = {}", otaPackage.ToString());
            version = new PackageHandler
            {
                AbstractPackage = new PackageV1ToAbstractPackage
                {
                    Package = otaPackage,
                },
                OtaService = otaService,
                PackageSavePath = packageSavePath
            }.Start() ?? version;
        }

        public void OnNewPackageV2(OTAPackageV2 otaPackageV2)
        {
            LOG.Info("otaPackageV2 = {}", otaPackageV2.ToString());
            version = new PackageHandler
            {
                AbstractPackage = new PackageV2ToAbstractPackage
                {
                    Package = otaPackageV2,
                },
                OtaService = otaService,
                PackageSavePath = packageSavePath
            }.Start() ?? version;
        }

        public void OnQueryVersion(OTAQueryInfo queryInfo)
        {
            if (queryInfo != null)
            {
                LOG.Info("queryInfo = {}", queryInfo.ToString());
            }

            otaService.ReportVersion(version);
        }


        private interface AbstractPackage
        {
            void PreCheck();
            WebRequest GetWebRequest();
            string GetFileName();
            string GetVersion();
            string GetSign();
        }

        private class OtaException : Exception
        {
            public int Result { get; set; }
            public int Progress { get; set; }
            public string Version { get; set; }
            public string Description { get; set; }
        }

        private class PackageHandler
        {
            private string packagePath;
            public AbstractPackage AbstractPackage { get; set; }
            public string PackageSavePath { get; set; }
            public OTAService OtaService { get; set; }


            private void DownloadPackage()
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                // Declares an HTTP request.
                var myRequest = AbstractPackage.GetWebRequest();

                // SSL security channel authentication certificate
                ServicePointManager.ServerCertificateValidationCallback = (a, b, c, d) => true;
                using var webResponse = myRequest.GetResponse();
                using var myStream = webResponse.GetResponseStream();
                packagePath = Path.Combine(PackageSavePath, AbstractPackage.GetFileName());
                using var file = File.Open(packagePath, FileMode.Create);
                myStream.CopyTo(file);
                myStream.Flush();
            }

            private void VerifyPackageSign()
            {
                if (AbstractPackage.GetSign() == null)
                {
                    LOG.Warn("sign is empty");
                    return;
                }

                var strSha256 = IotUtil.GetSHA256HashFromFile(packagePath);
                LOG.Info("SHA256 = {}", strSha256);

                if (strSha256 != AbstractPackage.GetSign())
                {
                    throw new OtaException
                    {
                        Result = OTAService.OTA_CODE_NO_NEED,
                        Progress = 0,
                        Version = AbstractPackage.GetVersion(),
                        Description = "sign verify failed"
                    };
                }

                LOG.Info("sign check passed");
            }

            private void InstallPackage()
            {
                LOG.Info("install package ok");
                // throw new OtaException if the installation fails.
            }

            public string Start()
            {
                var version = AbstractPackage.GetVersion();
                try
                {
                    AbstractPackage.PreCheck();
                    DownloadPackage();
                    VerifyPackageSign();
                    InstallPackage();
                    OtaService.ReportOtaStatus(OTAService.OTA_CODE_SUCCESS, 100, version, "upgrade success");
                    LOG.Info("ota upgrade ok");
                    return AbstractPackage.GetVersion();
                }
                catch (OtaException ex)
                {
                    OtaService.ReportOtaStatus(ex.Result, ex.Progress, ex.Version, ex.Description);
                    LOG.Error("{}", ex.Description);
                }
                catch (WebException exp)
                {
                    OtaService.ReportOtaStatus(OTAService.OTA_CODE_DOWNLOAD_TIMEOUT, 0, version,
                        exp.GetBaseException().Message);
                    LOG.Error("download failed");
                }
                catch (Exception ex)
                {
                    OtaService.ReportOtaStatus(OTAService.OTA_CODE_INNER_ERROR, 0, version,
                        ex.GetBaseException().Message);
                    LOG.Error("download failed");
                }

                return null;
            }
        }

        private class PackageV1ToAbstractPackage : AbstractPackage
        {
            public OTAPackage Package { get; set; }

            public WebRequest GetWebRequest()
            {
                var myRequest = WebRequest.Create(new Uri(Package.url));
                myRequest.Headers = new WebHeaderCollection { { "Authorization", "Bearer " + Package.token } };
                return myRequest;
            }

            public string GetFileName()
            {
                return Package.fileName;
            }

            public string GetVersion()
            {
                return Package.version;
            }

            public string GetSign()
            {
                return Package.sign;
            }

            public void PreCheck()
            {
                // todo Check the version number, remaining space, remaining battery, and signal quality.
                // If the upgrade is not allowed, throw new OtaException  with error code defined in OTAService
                // or a custom error code.
            }
        }

        private class PackageV2ToAbstractPackage : AbstractPackage
        {
            public OTAPackageV2 Package { get; set; }

            public WebRequest GetWebRequest()
            {
                // Declares an HTTP request.
                var myRequest = WebRequest.Create(new Uri(Package.url));
                return myRequest;
            }

            public string GetFileName()
            {
                return Package.fileName;
            }

            public string GetVersion()
            {
                return Package.version;
            }

            public string GetSign()
            {
                return Package.sign;
            }

            public void PreCheck()
            {
                // todo Check the version number, remaining space, remaining battery, and signal quality.
                // If the upgrade is not allowed, throw new OtaException  with error code defined in OTAService
                // or a custom error code.
            }
        }
    }
}