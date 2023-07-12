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

using System;
using System.IO;
using System.Net;
using System.Net.Security;
using IoT.SDK.Device;
using IoT.SDK.Device.OTA;
using IoT.SDK.Device.Utils;
using NLog;

namespace IoT.Device.Demo
{
    public class OTAUpgrade : OTAListener
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private OTAService otaService;
        private IoTDevice device;

        private string version; // Version
        private string packageSavePath; // Path where the upgrade package is stored
        private OTAPackage otaPackage;
        private OTAPackageV2 otaPackageV2;

        public OTAUpgrade(IoTDevice device, string packageSavePath)
        {
            this.device = device;
            this.otaService = device.otaService;
            otaService.SetOtaListener(this);
            this.packageSavePath = packageSavePath;
            this.version = "v0.0.1"; // Change to the actual value.
        }

        public int Init()
        {
            return device.Init();
        }
        
        public void OnNewPackage(OTAPackage otaPackage)
        {
            this.otaPackage = otaPackage;
            Log.Info("otaPackage = " + otaPackage.ToString());

            if (PreCheck(otaPackage) != 0)
            {
                Log.Error("preCheck failed");
                return;
            }

            DownloadPackage();
        }

        public void OnNewPackageV2(OTAPackageV2 otaPackageV2)
        {
            this.otaPackageV2 = otaPackageV2;
            Log.Info("otaPackageV2 = " + otaPackageV2.ToString());

            if (PreCheckV2(otaPackageV2) != 0)
            {
                Log.Error("preCheckV2 failed");
                return;
            }

            DownloadPackageV2();
        }

        public void OnQueryVersion()
        {
            otaService.ReportVersion(version);
        }

        /// <summary>
        /// Verifies the upgrade package.
        /// </summary>
        /// <param name="strSHA256">Indicates a string after encapsulated using SHA256.</param>
        /// <returns>Returns 0 if the verification is passed; returns -1 otherwise.</returns>
        public int CheckPackage(string strSHA256)
        {
            if (strSHA256 != otaPackage.sign)
            {
                Log.Error("SHA256 check fail");

                // otaService.reportOtaStatus(OTAService.OTA_CODE_CHECK_FAIL, 0, version, "SHA256 check fail");
                return -1;
            }

            // TODO Add other verifications.
            return 0;
        }

        /// <summary>
        /// Installs the upgrade package. You must implement this method.
        /// </summary>
        /// <returns>Returns 0 if the package is installed.</returns>
        public int InstallPackage()
        {
            // TODO
            Log.Info("installPackage ok");

            // Reports OTA_CODE_INSTALL_FAIL if the installation fails.
            // otaService.reportOtaStatus(OTAService.OTA_CODE_INSTALL_FAIL, 0, version,null);
            return 0;
        }

        /// <summary>
        /// Performs an pre-upgrade check. You must implement this method.
        /// </summary>
        /// <param name="otaPackage">Indicates an upgrade package.</param>
        /// <returns>Returns 0 if the upgrade is allowed; returns other values if the upgrade is not allowed.</returns>
        public int PreCheck(OTAPackage otaPackage)
        {
            // todo Check the version number, remaining space, remaining battery, and signal quality. If the upgrade is not allowed, return –1 and report the error code defined in OTAService or a custom error code.

            ////otaService.reportOtaStatus(OTAService.OTA_CODE_NO_NEED, 0, null);

            return 0;
        }

        /// <summary>
        /// Performs an pre-upgrade check. You must implement this method.
        /// </summary>
        /// <param name="otaPackageV2">Indicates an upgrade package.</param>
        /// <returns>Returns 0 if the upgrade is allowed; returns other values if the upgrade is not allowed.</returns>
        public int PreCheckV2(OTAPackageV2 otaPackageV2)
        {
            // todo Check the version number, remaining space, remaining battery, and signal quality. If the upgrade is not allowed, return –1 and report the error code defined in OTAService or a custom error code.

            ////otaService.reportOtaStatus(OTAService.OTA_CODE_NO_NEED, 0, null);

            return 0;
        }

        public void OnUpgradeSuccess(string strSHA256)
        {
            Log.Info("downloadPackage success");

            // Verifies the downloaded upgrade package.
            if (CheckPackage(strSHA256) != 0)
            {
                return;
            }

            // Installs the upgrade package.
            if (InstallPackage() != 0)
            {
                return;
            }

            // Reports an upgrade success message. The updated version number must be contained in the message. Otherwise, the platform considers that the upgrade fails.
            version = otaPackage.version;
            otaService.ReportOtaStatus(OTAService.OTA_CODE_SUCCESS, 100, version, "upgrade success");
            Log.Info("ota upgrade ok");
        }

        public void OnUpgradeSuccessV2()
        {
            Log.Info("downloadPackage success");

            // Installs the upgrade package.
            if (InstallPackage() != 0)
            {
                return;
            }

            // Reports an upgrade success message. The updated version number must be contained in the message. Otherwise, the platform considers that the upgrade fails.
            version = otaPackageV2.version;
            otaService.ReportOtaStatus(OTAService.OTA_CODE_SUCCESS, 100, version, "upgrade success");
            Log.Info("ota upgrade ok");
        }

        public void OnUpgradeFailure()
        {
            Log.Error("download failed");
        }

        private void DownloadPackage()
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                // Declares an HTTP request.
                HttpWebRequest myRequest = (HttpWebRequest)HttpWebRequest.Create(new Uri(otaPackage.url));

                // SSL security channel authentication certificate
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback((a, b, c, d) => { return true; });

                myRequest.ClientCertificates.Add(IotUtil.GetCert(@"\certificate\DigiCertGlobalRootCA.crt.pem"));

                WebHeaderCollection wc = new WebHeaderCollection();
                wc.Add("Authorization", "Bearer " + otaPackage.token);
                myRequest.Headers = wc;

                int nfileSize = 0;
                using (WebResponse webResponse = myRequest.GetResponse())
                {
                    using (Stream myStream = webResponse.GetResponseStream())
                    {
                        using (FileStream fs = new FileStream(packageSavePath, FileMode.Create))
                        {
                            using (BinaryWriter bw = new BinaryWriter(fs))
                            {
                                using (BinaryReader br = new BinaryReader(myStream))
                                {
                                    // Send a request to the server to obtain the response data flow of the server.
                                    byte[] nbytes = new byte[1024 * 10];
                                    int nReadSize = 0;
                                    nReadSize = br.Read(nbytes, 0, 1024 * 10);
                                    nfileSize = nReadSize;
                                    while (nReadSize > 0)
                                    {
                                        bw.Write(nbytes, 0, nReadSize);
                                        nReadSize = br.Read(nbytes, 0, 1024 * 10);
                                    }
                                }
                            }
                        }
                    }
                }

                if (nfileSize == otaPackage.fileSize)
                {
                    string strSHA256 = IotUtil.GetSHA256HashFromFile(packageSavePath);
                    Log.Info("SHA256 = " + strSHA256);

                    otaService.ReportOtaStatus(OTAService.OTA_CODE_SUCCESS, 100, version, null);
                    OnUpgradeSuccess(strSHA256);
                }
            }
            catch (WebException exp)
            {
                otaService.ReportOtaStatus(OTAService.OTA_CODE_DOWNLOAD_TIMEOUT, 0, version, exp.Message);
                OnUpgradeFailure();
            }
            catch (Exception ex)
            {
                otaService.ReportOtaStatus(OTAService.OTA_CODE_INNER_ERROR, 0, version, ex.Message);
                OnUpgradeFailure();
            }
        }

        private void DownloadPackageV2()
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                // Declares an HTTP request.
                HttpWebRequest myRequest = (HttpWebRequest)HttpWebRequest.Create(new Uri(otaPackageV2.url));

                // SSL security channel authentication certificate
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback((a, b, c, d) => { return true; });

                myRequest.ClientCertificates.Add(IotUtil.GetCert(@"\certificate\DigiCertGlobalRootCA.crt.pem"));
                /*
                WebHeaderCollection wc = new WebHeaderCollection();
                wc.Add("Authorization", "Bearer " + otaPackage.token);
                myRequest.Headers = wc;
                */
                int nfileSize = 0;
                using (WebResponse webResponse = myRequest.GetResponse())
                {
                    using (Stream myStream = webResponse.GetResponseStream())
                    {
                        using (FileStream fs = new FileStream(packageSavePath, FileMode.Create))
                        {
                            using (BinaryWriter bw = new BinaryWriter(fs))
                            {
                                using (BinaryReader br = new BinaryReader(myStream))
                                {
                                    // Send a request to the server to obtain the response data flow of the server.
                                    byte[] nbytes = new byte[1024 * 10];
                                    int nReadSize = 0;
                                    nReadSize = br.Read(nbytes, 0, 1024 * 10);
                                    nfileSize = nReadSize;
                                    while (nReadSize > 0)
                                    {
                                        bw.Write(nbytes, 0, nReadSize);
                                        nReadSize = br.Read(nbytes, 0, 1024 * 10);
                                    }
                                }
                            }
                        }
                    }
                }

                otaService.ReportOtaStatus(OTAService.OTA_CODE_SUCCESS, 100, version, null);
                OnUpgradeSuccessV2();

            }
            catch (WebException exp)
            {
                otaService.ReportOtaStatus(OTAService.OTA_CODE_DOWNLOAD_TIMEOUT, 0, version, exp.Message);
                OnUpgradeFailure();
            }
            catch (Exception ex)
            {
                otaService.ReportOtaStatus(OTAService.OTA_CODE_INNER_ERROR, 0, version, ex.Message);
                OnUpgradeFailure();
            }
        }
    }
}
