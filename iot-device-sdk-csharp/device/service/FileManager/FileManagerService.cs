/*
 * Copyright (c) 2024-2024 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Utils;
using NLog;

namespace IoT.SDK.Device.Service.FileManager
{
    public class FileManagerService : AbstractService
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        private const string FILE_NAME = "file_name";

        private const string FILE_ATTRIBUTES = "file_attributes";

        private const string GET_UPLOAD_URL = "get_upload_url";

        private const string GET_UPLOAD_URL_RESPONSE = "get_upload_url_response";

        private const string OBJECT_NAME = "object_name";

        private const string RESULT_CODE = "result_code";

        private const string STATUS_CODE = "status_code";

        private const string STATUS_DESCRIPTION = "status_description";

        private const string UPLOAD_RESULT_REPORT = "upload_result_report";

        private const string GET_DOWNLOAD_URL = "get_download_url";

        private const string GET_DOWNLOAD_URL_RESPONSE = "get_download_url_response";

        private const string DOWNLOAD_RESULT_REPORT = "download_result_report";

        public FileManagerListener fileManagerListener { get; set; }
        public BridgeFileManagerListener bridgeFileManagerListener { get; set; }

        public override string GetServiceId()
        {
            return "$file_manager";
        }

        private DeviceEvent GenerateFileManagerEvent(string eventType, Dictionary<string, object> paras)
        {
            return new DeviceEvent
            {
                serviceId = GetServiceId(),
                eventType = eventType,
                eventTime = IotUtil.GetEventTime(),
                paras = paras
            };
        }

        private DeviceEvent GenerateUpOrDownUrlDeviceEvent(UrlRequest request, string eventType)
        {
            return GenerateFileManagerEvent(eventType,
                new Dictionary<string, object>
                {
                    { FILE_NAME, request.fileName },
                    { FILE_ATTRIBUTES, request.fileAttributes }
                });
        }

        public void GetUploadUrl(UrlRequest request)
        {
            DeviceEvent deviceEvent = GenerateUpOrDownUrlDeviceEvent(request, GET_UPLOAD_URL);
            iotDevice.GetClient().ReportEvent(deviceEvent);
        }

        public void GetUploadUrlOfBridge(string deviceId, UrlRequest request)
        {
            var deviceEvent = GenerateUpOrDownUrlDeviceEvent(request, GET_UPLOAD_URL);
            iotDevice.GetClient().ReportEvent(deviceId, deviceEvent);
        }


        private DeviceEvent GenerateUploadFileStatusEvent(OpFileStatusRequest uploadFileStatusRequest, string eventType)
        {
            return GenerateFileManagerEvent(eventType,
                new Dictionary<string, object>
                {
                    { OBJECT_NAME, uploadFileStatusRequest.objectName },
                    { RESULT_CODE, uploadFileStatusRequest.resultCode },
                    { STATUS_CODE, uploadFileStatusRequest.statusCode },
                    { STATUS_DESCRIPTION, uploadFileStatusRequest.statusDescription }
                });
        }

        public void ReportUploadFileStatus(OpFileStatusRequest request)
        {
            DeviceEvent deviceEvent = GenerateUploadFileStatusEvent(request, UPLOAD_RESULT_REPORT);
            iotDevice.GetClient().ReportEvent(deviceEvent);
        }

        public void ReportUploadFileStatusOfBridge(string deviceId, OpFileStatusRequest request)
        {
            DeviceEvent deviceEvent = GenerateUploadFileStatusEvent(request, UPLOAD_RESULT_REPORT);
            iotDevice.GetClient().ReportEvent(deviceId, deviceEvent);
        }


        public void GetDownloadUrl(UrlRequest request)
        {
            DeviceEvent deviceEvent = GenerateUpOrDownUrlDeviceEvent(request, GET_DOWNLOAD_URL);
            iotDevice.GetClient().ReportEvent(deviceEvent);
        }

        public void GetDownloadUrlOfBridge(string deviceId, UrlRequest request)
        {
            DeviceEvent deviceEvent = GenerateUpOrDownUrlDeviceEvent(request, GET_DOWNLOAD_URL);
            iotDevice.GetClient().ReportEvent(deviceId, deviceEvent);
        }

        public void ReportDownloadFileStatus(OpFileStatusRequest request)
        {
            DeviceEvent deviceEvent = GenerateUploadFileStatusEvent(request, DOWNLOAD_RESULT_REPORT);
            iotDevice.GetClient().ReportEvent(deviceEvent);
        }

        public void ReportDownloadFileStatusOfBridge(string deviceId, OpFileStatusRequest request)
        {
            DeviceEvent deviceEvent = GenerateUploadFileStatusEvent(request, DOWNLOAD_RESULT_REPORT);
            iotDevice.GetClient().ReportEvent(deviceId, deviceEvent);
        }

        /**
     * 接收文件处理事件
     *
     * @param deviceEvent 服务事件
     */
        public override void OnEvent(DeviceEvent deviceEvent)
        {
            if (fileManagerListener == null)
            {
                LOG.Info("fileManagerListener is null");
                return;
            }

            if (string.Equals(deviceEvent.eventType, GET_UPLOAD_URL_RESPONSE,
                    StringComparison.OrdinalIgnoreCase))
            {
                UrlResponse urlParam = JsonUtil.ConvertDicToObject<UrlResponse>(deviceEvent.paras);
                fileManagerListener.OnUploadUrl(urlParam);
            }
            else if (string.Equals(deviceEvent.eventType, GET_DOWNLOAD_URL_RESPONSE,
                         StringComparison.OrdinalIgnoreCase))
            {
                UrlResponse urlParam = JsonUtil.ConvertDicToObject<UrlResponse>(deviceEvent.paras);
                fileManagerListener.OnDownloadUrl(urlParam);
            }
            else
            {
                LOG.Error("invalid event type:{0}", deviceEvent.eventType);
            }
        }
    }
}