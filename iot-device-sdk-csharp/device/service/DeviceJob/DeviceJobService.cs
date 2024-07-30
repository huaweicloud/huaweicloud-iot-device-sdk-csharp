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

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IoT.SDK.Device.Service.DeviceJob
{
    public class DeviceJobStatus
    {
        public const string Sent = "SENT";
        public const string InProgress = "IN_PROGRESS";
        public const string Succeed = "SUCCEED";
        public const string Failed = "FAILED";
        public const string Removed = "REMOVED";
        public const string Rejected = "REJECTED";
        public const string Canceled = "CANCELED";
    }

    public class DeviceJobNotify
    {
        [JsonProperty("job_id")]
        public string JobId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("job_document")]
        public JObject JobDocument { get; set; }
    }


    public class DeviceJobErrorDetail
    {
        [JsonProperty("error_code")]
        public string ErrorCode { get; set; }

        [JsonProperty("error_msg")]
        public string ErrorMessage { get; set; }
    }

    public class DeviceJobUpdate
    {
        [JsonProperty("job_id")]
        public string JobId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("progress")]
        public int Progress { get; set; }

        [JsonProperty("status_details")]
        public JObject StatusDetails { get; set; }
    }

    public class DeviceJobUpdateResponse
    {
        [JsonProperty("job_id")]
        public string JobId { get; set; }

        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("progress")]
        public int Progress { get; set; }

        [JsonProperty("error_detail")]
        public DeviceJobErrorDetail DeviceJobErrorDetail { get; set; }
    }


    public class DeviceJobInfo
    {
        [JsonProperty("job_id")]
        public string JobId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("error_detail")]
        public DeviceJobErrorDetail DeviceJobErrorDetail { get; set; }
    }

    public class DeviceJobDetail : DeviceJobInfo
    {
        [JsonProperty("job_document")]
        public string JobDocument { get; set; }
    }

    public interface DeviceJobNotifyListener
    {
        void OnJobNotify(DeviceJobNotify notify);
    }

    public interface DeviceJobListListener
    {
        void OnJobNotify(List<DeviceJobInfo> jobs);
    }

    public interface DeviceJobUpdateResponseListener
    {
        void OnJobUpdateResponse(DeviceJobUpdateResponse detail);
    }

    public interface DeviceJobDetailListener
    {
        void OnJobNotify(DeviceJobDetail detail);
    }

    public interface DeviceJobNextListener
    {
        void OnJobNext(DeviceJobDetail detail);
    }


    public interface DeviceJobService
    {
        public DeviceJobNotifyListener DeviceJobNotifyListener { get; set; }

        public DeviceJobListListener DeviceJobListListener { get; set; }
        public void ReportGetJobList(string marker = null);

        public DeviceJobDetailListener DeviceJobDetailListener { get; set; }
        public void ReportGetJobDetail(string jobId);

        public DeviceJobUpdateResponseListener DeviceJobUpdateResponseListener { get; set; }
        public void ReportJobUpdate(DeviceJobUpdate update);

        public DeviceJobNextListener DeviceJobNextListener { get; set; }
        public void ReportGetJobNext();
    }
}