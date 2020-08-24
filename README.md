# iot-device-sdk-cSharp
# 目录
# 版本说明
# 前言
本文通过实例讲述iot-device-sdk-cSharp（以下简称SDK）帮助设备用MQTT协议快速连接到华为物联网平台。
# SDK简介
SDK面向运算、存储能力较强的嵌入式终端设备，开发者通过调用SDK接口，便可实现设备与物联网平台的上下行通讯。SDK当前支持的功能有：
*  支持设备消息、属性上报、属性读写、命令下发
*  支持OTA升级
*  支持密码认证和证书认证两种设备接入方式
*  支持自定义topic
*  支持设备影子查询

**SDK目录结构**

iot-device-sdk-java：sdk代码

iot-device-demo：普通直连设备的demo代码

iot-gateway-demo：网关设备的demo代码（功能开发中）

iot-device-feature-test：调用demo程序的入口工程

**第三方类库使用版本**

MQTTnet：v3.0.11

MQTTnet.Extensions.ManagedClient：v3.0.11

Newtonsoft.Json：v12.0.3

NLog：v4.7

# 准备工作

*  已安装Microsoft Visual Studio 2017

*  .NET Framework 版本：4.5.2

# 上传产品模型并注册设备

为了方便体验，我们提供了一个烟感的产品模型，烟感会上报烟雾值、温度、湿度、烟雾报警、还支持响铃报警命令。以烟感例，体验消息上报、属性上报等功能。

1. 访问[设备接入服务](https://www.huaweicloud.com/product/iothub.html)，单击“立即使用”进入设备接入控制台。

3. 访问管理控制台，查看MQTTS设备接入地址，保存该地址。![](D:\LFD\HUAWEI\Code\Gitlab\C# SDK\iot-device-sdk-cSharp\doc\upload_profile_1.png)

4. 在设备接入控制台选择“产品”，单击右上角的“创建产品”，在弹出的页面中，填写“产品名称”、“协议类型”、“数据格式”、“厂商名称”、“所属行业”、“设备类型”等信息，然后点击右下角“立即创建”。

   - 协议类型选择“MQTT”；

   - 数据格式选择“JSON”。![](D:\LFD\HUAWEI\Code\Gitlab\C# SDK\iot-device-sdk-cSharp\doc\upload_profile_2.png)

5. 产品创建成功后，单击“详情”进入产品详情，在功能定义页面，单击“上传模型文件”，上传烟感产品模型[smokeDetector](https://support.huaweicloud.com/devg-iothub/resource/smokeDetector_cb097d20d77b4240adf1f33d36b3c278_smokeDetector.zip)。

6. 在左侧导航栏，选择“ 设备 > 所有设备”，单击右上角“注册设备”，在弹出的页面中，填写注册设备参数，然后单击“确定”。![](D:\LFD\HUAWEI\Code\Gitlab\C# SDK\iot-device-sdk-cSharp\doc\upload_profile_3.png)

7. 设备注册成功后保存设备标识码、设备ID、密钥。

# 设备初始化

1. 创建设备。

   设备接入平台时，物联网平台提供密钥和证书两种鉴权方式。
*  如果您使用1883端口接入平台，需要写入获取的设备ID、密钥。

   ```c#
IoTDevice device = new IoTDevice("iot-mqtts.cn-north-4.myhuaweicloud.com", 1883, "5eb4cd4049a5ab087d7d4861_demo", "secret");
   ```
   
   如果使用8883端口接入，需要把平台证书（DigiCertGlobalRootCA.crt.pem）放在根目录，并写入获取的设备ID、密钥。

   ```c#
IoTDevice device = new IoTDevice("iot-mqtts.cn-north-4.myhuaweicloud.com", 8883, "5eb4cd4049a5ab087d7d4861_demo", "secret");
   ```
   **注：为安全起见，推荐使用8883端口接入平台。**

*  证书模式接入。

   华为物联网平台支持设备使用自己的X.509证书接入鉴权。在SDK中使用X.509证书接入时，需自行制作设备证书，并放到调用程序根目录下。SDK调用证书的根目录为\iot-device-feature-test\bin\Debug\certificate。

   接入步骤请参考：

   - 制作设备CA调测证书，详细指导请参考<a href="https://support.huaweicloud.com/usermanual-iothub/iot_01_0055.html" target="_blank">注册X.509证书认证的设备</a>。

   - 制作完调测证书后，参考以下命令转换成C#能接入的设备证书格式：

     ```c#
     openssl x509 -in deviceCert.pem -out deviceCert.crt //先生成crt格式的证书；
     openssl pkcs12 -export -out deviceCert.pfx - inkey deviceCert.key -in deviceCert.crt - certfile rootCA.pem；
     
     X509Certificate2 clientCert = new X509Certificate2(@"\\Test01\\deviceCert.pfx", "123456");//必须使用X.509Certificate2
     ```

   - 参考以下命令，创建设备。

     ```c#
     string deviceCertPath = Environment.CurrentDirectory + @"\certificate\deviceCert.pfx";
     if (!File.Exists(deviceCertPath))
     {
         Log.Error("请将设备证书放到根目录！");
     
         return;
     }
     
     X509Certificate2 deviceCert = new X509Certificate2(deviceCertPath, "123456");
     
     // 使用证书创建设备
     IoTDevice device = new IoTDevice("iot-mqtts.cn-north-4.myhuaweicloud.com", 8883, "5eb4cd4049a5ab087d7d4861_demo", deviceCert);
     ```

3. 调用init接口，建立连接。该接口是阻塞调用，如果建立连接成功会返回0。

   ```c#
   if (device.Init() != 0)
   {
   	return;
   }
   ```

4. 连接成功后，设备和平台之间开始通讯。调用IoT Device 的GetClient接口获取设备客户端，客户端提供了消息、属性、命令等通讯接口。

# 属性上报

打开PropertySample类，这个例子中会上报alarm、temperature、humidity、smokeConcentration这四个属性。

```c#
public void FunPropertySample()
{
    // 创建设备
    IoTDevice device = new IoTDevice("iot-mqtts.cn-north-4.myhuaweicloud.com", 1883, "5eb4cd4049a5ab087d7d4861_demo", "secret");

    if (device.Init() != 0)
    {
    	return;
    }

    Dictionary<string, object> json = new Dictionary<string, object>();
    Random rand = new Random();

    // 按照物模型设置属性
    json["alarm"] = 1;
    json["temperature"] = (float)rand.NextDouble() * 100.0f;
    json["humidity"] = (float)rand.NextDouble() * 100.0f;
    json["smokeConcentration"] = (float)rand.NextDouble() * 100.0f;

    ServiceProperty serviceProperty = new ServiceProperty();
    serviceProperty.properties = json;
    serviceProperty.serviceId = "smokeDetector"; // serviceId要和物模型一致

    List<ServiceProperty> properties = new List<ServiceProperty>();
    properties.Add(serviceProperty);

    device.GetClient().messagePublishListener = this;
    device.GetClient().Report(new PubMessage(properties));
}

public void OnMessagePublished(RawMessage message)
{
	Console.WriteLine("pubSuccessMessage:" + message.Payload);
	Console.WriteLine();
}

public void OnMessageUnPublished(RawMessage message)
{
	Console.WriteLine("pubFailMessage:" + message.Payload);
	Console.WriteLine();
}
```
修改PropertySample的FunPropertySample函数后直接运行iot-device-feature-test工程，调用FunPropertySample函数上报属性。

# 消息上报

消息上报是指设备向平台上报消息，本例还包含自定义Topic消息上报，以及自定义Topic命令下发功能。

1. 调用IoTDevice的GetClient接口获取客户端。

2. 调用客户端的Report接口上报设备消息。

   在MessageSample这个例子中上报消息，如果消息上报成功或者失败会进行函数回调：

   ```c#
   public void FunMessageSample()
   {
       // 创建设备
       IoTDevice device = new IoTDevice("iot-mqtts.cn-north-4.myhuaweicloud.com", 1883, "5eb4cd4049a5ab087d7d4861_demo", "secret");
   
       if (device.Init() != 0)
       {
           return;
       }
   
       device.GetClient().Report(new PubMessage(new DeviceMessage("hello")));
       device.GetClient().deviceCustomMessageListener = this;
       device.GetClient().messagePublishListener = this;
   
       // 上报自定义topic消息，注意需要先在平台配置自定义topic,并且topic的前缀已经规定好，固定为：$oc/devices/{device_id}/user/，通过Postman模拟应用侧使用自定义Topic进行命令下发。
       string suf_topic = "wpy";
       device.GetClient().SubscribeTopic(suf_topic);
   
       device.GetClient().Report(new PubMessage(CommonTopic.PRE_TOPIC + suf_topic, "hello raw message "));
   }
   
   public void OnMessagePublished(RawMessage message)
   {
       Console.WriteLine("pubSucessMessage:" + message.Payload);
       Console.WriteLine();
   }
   
   public void OnMessageUnPublished(RawMessage message)
   {
       Console.WriteLine("pubFailMessage:" + message.Payload);
       Console.WriteLine();
   }
   
   public void OnCustomMessageCommand(string message)
   {
       Console.WriteLine("onCustomMessageCommand , message = " + message);
   }
   ```

3. 选择对应设备，点击“查看”，在设备详情页面启动设备消息跟踪。

4. 修改MessageSample类的FunMessageSample函数，替换自己的设备参数后启动iot-device-feature-test工程调用MessageSample类。

5. 在设备接入控制台，选择“设备 > 所有设备”查看设备是否在线。![](D:\LFD\HUAWEI\Code\Gitlab\C# SDK\iot-device-sdk-cSharp\doc\upload_profile_4.png)

6. 平台收到设备上报的消息。![](D:\LFD\HUAWEI\Code\Gitlab\C# SDK\iot-device-sdk-cSharp\doc\upload_profile_5.png)

# 属性读写

调用客户端的propertyListener方法来设置属性回调接口。在PropertiesGetAndSetSample这个例子中，我们实现了属性读写接口。

- 写属性处理：实现了属性的写操作，SDK收到属性值；

- 读属性处理：将本地属性值按照接口格式进行拼装；
- 属性读写接口需要调用Report接口来上报操作结果；
- 如果设备不支持平台主动到设备读，OnPropertiesGet接口可以空实现；

```c#
private IoTDevice device;

/// <summary>
/// 通过Postman查询和设置平台属性
/// </summary>
public void FunPropertiesSample()
{
    // 创建设备
    device = new IoTDevice("iot-mqtts.cn-north-4.myhuaweicloud.com", 8883, "5eb4cd4049a5ab087d7d4861_demo", "secret");

    if (device.Init() != 0)
    {
        return;
    }

    device.GetClient().propertyListener = this;
}

public void OnPropertiesSet(string requestId, string services)
{
    Console.WriteLine("requestId Set:" + requestId);
    Console.WriteLine("services Set:" + services);

    device.GetClient().Report(new PubMessage(CommonTopic.TOPIC_SYS_PROPERTIES_SET_RESPONSE + "=" + requestId, "{\"result_code\": 0,\"result_desc\": \"success\"}"));
}

public void OnPropertiesGet(string requestId, string serviceId)
{
    Console.WriteLine("requestId Get:" + requestId);
    Console.WriteLine("serviceId Get:" + serviceId);

    Dictionary<string, object> json = new Dictionary<string, object>();
    Random rand = new Random();

    // 按照物模型设置属性
    json["alarm"] = 1;
    json["temperature"] = (float)rand.NextDouble() * 100.0f;
    json["humidity"] = (float)rand.NextDouble() * 100.0f;
    json["smokeConcentration"] = (float)rand.NextDouble() * 100.0f;

    ServiceProperty serviceProperty = new ServiceProperty();
    serviceProperty.properties = json;
    serviceProperty.serviceId = serviceId; // serviceId要和物模型一致

    List<ServiceProperty> properties = new List<ServiceProperty>();
    properties.Add(serviceProperty);

    device.GetClient().Report(new PubMessage(CommonTopic.TOPIC_SYS_PROPERTIES_GET_RESPONSE + "=" + requestId, properties));
}
```
# 命令下发

设置命令监听器用来接收平台下发的命令，在回调接口里，需要对命令进行处理，并上报响应。

在CommandSample例子中实现了命令的处理，收到命令后仅进行控制台显示，然后调用Report上报响应。

```c#
private IoTDevice device;

public void FunCommandSample()
{
	// 创建设备
	device = new IoTDevice("iot-mqtts.cn-north-4.myhuaweicloud.com", 8883, "5eb4cd4049a5ab087d7d4861_demo", "secret");

if (device.Init() != 0)
{
	return;
}

	device.GetClient().commandListener = this;
}

public void OnCommand(string requestId, string serviceId, string commandName, Dictionary<string, object> paras)
{
	Console.WriteLine("onCommand, serviceId = " + serviceId);
	Console.WriteLine("onCommand, name = " + commandName);
	Console.WriteLine("onCommand, paras =  " + JsonUtil.ConvertObjectToJsonString(paras));

	////处理命令

	Dictionary<string, string> dic = new Dictionary<string, string>();
dic.Add("result", "success");

	// 发送命令响应
	device.GetClient().Report(new PubMessage(requestId, new CommandRsp(0, dic)));
}
```
# 设备影子

1. 设备请求获取平台的设备影子数据，用于设备向平台获取设备影子数据。

   ```c#
   device.GetClient().deviceShadowListener = this;
   
   string guid = Guid.NewGuid().ToString();
   
   Console.WriteLine(guid);
   
   string topic = CommonTopic.TOPIC_SYS_SHADOW_GET + "=" + guid;
   
   device.GetClient().Report(new PubMessage(topic, string.Empty));
   ```

2. 设备接收平台返回的设备影子数据，用于接收平台返回的设备影子数据。

   ```c#
   public void OnShadowCommand(string requestId, string message)
   {
   	Console.WriteLine(requestId);
   	Console.WriteLine(message);
   }
   ```

# OTA升级

1. 软件升级。参考<a href=" https://support.huaweicloud.com/usermanual-iothub/iot_01_0047.html#section3 " target="_blank">软件升级指导</a>检查软件升级能力并上传软件包。

2. 固件升级。参考<a href=" https://support.huaweicloud.com/usermanual-iothub/iot_01_0027.html#section3 " target="_blank">固件升级</a>检查固件升级能力并上传固件包。

3. 平台下发获取版本信息通知

   ```c#
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
   }
   ```

4. 设备上报软固件版本。

   ```C#
   /// <summary>
   /// 上报固件版本信息
   /// </summary>
   /// <param name="version">固件版本</param>
   public void reportVersion(string version)
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
   ```

5. 平台下发升级通知。

   ```c#
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
   ```

6. 设备请求下载包。

   ```c#
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
   ```

7. 设备上报升级状态。

   ```c#
   /// <summary>
   /// 上报升级状态
   /// </summary>
   /// <param name="result">升级结果</param>
   /// <param name="progress">升级进度0-100</param>
   /// <param name="version">当前版本</param>
   /// <param name="description">具体失败的原因，可选参数</param>
   public void reportOtaStatus(int result, int progress, string version, string description)
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
   ```

# 设备时间同步

1. 设备向平台发起时间同步请求。  

   ```c#
   public void RequestTimeSync()
   {
       Dictionary<string, object> node = new Dictionary<string, object>();
       node.Add("device_send_time", IotUtil.GetTimeStamp());
   
       DeviceEvent deviceEvent = new DeviceEvent();
       deviceEvent.eventType = "time_sync_request";
       deviceEvent.paras = node;
       deviceEvent.serviceId = "$time_sync";
       deviceEvent.eventTime = IotUtil.GetEventTime();
   
       iotDevice.GetClient().messagePublishListener = this;
       iotDevice.GetClient().ReportEvent(deviceEvent);
   }
   ```

2. 平台向设备发送时间同步响应，携带设备发送时间参数device_send_time。当平台收到时间server_recv_time 后，向设备发送时间server_send_time 。

   假设设备收到的设备侧时间为device_recv_time ，则设备计算自己的准确时间为：

   (server_recv_time + server_send_time + device_recv_time - device_send_time) / 2

   ```c#
   public override void OnEvent(DeviceEvent deviceEvent)
   {
       if (listener == null)
       {
       	return;
       }
   
       if (deviceEvent.eventType == "time_sync_response")
       {
           Dictionary<string, object> node = deviceEvent.paras;
           long device_send_time = Convert.ToInt64(node["device_send_time"]);
           long server_recv_time = Convert.ToInt64(node["server_recv_time"]);
           long server_send_time = Convert.ToInt64(node["server_send_time"]);
   
           listener.OnTimeSyncResponse(device_send_time, server_recv_time, server_send_time);
       }
   }
           
   public void OnTimeSyncResponse(long device_send_time, long server_recv_time, long server_send_time)
   {
       long device_recv_time = Convert.ToInt64(IotUtil.GetTimeStamp());
       long now = (server_recv_time + server_send_time + device_recv_time - device_send_time) / 2;
       Console.WriteLine("now is " + StampToDatetime(now));
   }
   ```

# 开源协议