﻿/*
 * Copyright (c) 2020-2022 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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

using IoT.SDK.Device.Utils;
using Newtonsoft.Json;

namespace IoT.SDK.Device.Client
{
    /// <summary>
    /// Provides APIs related to the processing result.
    /// </summary>
    public class IotResult
    {
        public static readonly IotResult SUCCESS = new IotResult(0, "Success");
        public static readonly IotResult FAIL = new IotResult(1, "Fail");
        public static readonly IotResult TIMEOUT = new IotResult(2, "Timeout");

        /// <summary>
        /// Obtains a processing result.
        /// </summary>
        /// <param name="resultCode">Indicates the result code.</param>
        /// <param name="resultDesc">Indicates the result description.</param>
        public IotResult(int resultCode, string resultDesc)
        {
            this.resultCode = resultCode;
            this.resultDesc = resultDesc;
        }

        /// <summary>
        /// Indicates a result code. The value 0 indicates a success, and other values indicate a failure.
        /// </summary>
        [JsonProperty("result_code")]
        public int resultCode { get; set; }

        /// <summary>
        /// Indicates a result description.
        /// </summary>
        [JsonProperty("result_desc")]
        public string resultDesc { get; set; }

        public override string ToString()
        {
            return JsonUtil.ConvertObjectToJsonString(this);
        }
    }
}
