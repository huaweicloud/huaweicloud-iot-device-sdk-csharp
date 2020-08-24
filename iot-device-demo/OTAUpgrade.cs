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

        private string version; // 当前版本号
        private string packageSavePath; // 升级包保存路径
        private OTAPackage otaPackage;

        public OTAUpgrade(IoTDevice device, string packageSavePath)
        {
            this.device = device;
            this.otaService = device.otaService;
            otaService.setOtaListener(this);
            this.packageSavePath = packageSavePath;
            this.version = "v0.0.1"; // 修改为实际值
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

        public void OnQueryVersion()
        {
            otaService.reportVersion(version);
        }

        /// <summary>
        /// 校验升级包
        /// </summary>
        /// <param name="strSHA256">SHA256算法加密字符串</param>
        /// <returns>通过返回0，校验不通过返回-1</returns>
        public int CheckPackage(string strSHA256)
        {
            if (strSHA256 != otaPackage.sign)
            {
                Log.Error("SHA256 check fail");

                // otaService.reportOtaStatus(OTAService.OTA_CODE_CHECK_FAIL, 0, version, "SHA256 check fail");
                return -1;
            }

            // TODO 增加其他校验
            return 0;
        }

        /// <summary>
        /// 安装升级包，需要用户实现
        /// </summary>
        /// <returns>安装成功返回0</returns>
        public int InstallPackage()
        {
            // TODO
            Log.Info("installPackage ok");

            // 如果安装失败，上报OTA_CODE_INSTALL_FAIL
            // otaService.reportOtaStatus(OTAService.OTA_CODE_INSTALL_FAIL, 0, version,null);
            return 0;
        }

        /// <summary>
        /// 升级前检查，需要用户实现
        /// </summary>
        /// <param name="otaPackage">升级包</param>
        /// <returns>如果允许升级，返回0；返回非0表示不允许升级</returns>
        public int PreCheck(OTAPackage otaPackage)
        {
            // todo 对版本号、剩余空间、剩余电量、信号质量等进行检查，如果不允许升级，上报OTAService中定义的错误码或者自定义错误码，返回-1

            ////otaService.reportOtaStatus(OTAService.OTA_CODE_NO_NEED, 0, null);

            return 0;
        }

        public void OnUpgradeSuccess(string strSHA256)
        {
            Log.Info("downloadPackage success");

            // 校验下载的升级包
            if (CheckPackage(strSHA256) != 0)
            {
                return;
            }

            // 安装升级包
            if (InstallPackage() != 0)
            {
                return;
            }

            // 上报升级成功，注意版本号要携带更新后的版本号，否则平台会认为升级失败
            version = otaPackage.version;
            otaService.reportOtaStatus(OTAService.OTA_CODE_SUCCESS, 100, version, "upgrade success");
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

                // 声明HTTP请求
                HttpWebRequest myRequest = (HttpWebRequest)HttpWebRequest.Create(new Uri(otaPackage.url));

                // SSL安全通道认证证书
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
                                    // 向服务器请求,获得服务器的回应数据流
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

                    otaService.reportOtaStatus(OTAService.OTA_CODE_SUCCESS, 100, version, null);
                    OnUpgradeSuccess(strSHA256);
                }
            }
            catch (WebException exp)
            {
                otaService.reportOtaStatus(OTAService.OTA_CODE_DOWNLOAD_TIMEOUT, 0, version, exp.Message);
                OnUpgradeFailure();
            }
            catch (Exception ex)
            {
                otaService.reportOtaStatus(OTAService.OTA_CODE_INNER_ERROR, 0, version, ex.Message);
                OnUpgradeFailure();
            }
        }
    }
}
