/*
 * Copyright (c) 2022-2022 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using NLog;

namespace IoT.Bridge.Sample.Tcp.Session {
    class RequestIdCache {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly string SEPARATOR = ":";

        private static readonly RequestIdCache INSTANCE = new RequestIdCache();

        private readonly MemoryCache cache = new MemoryCache(new MemoryCacheOptions() {
            SizeLimit = 20000
        });

        public static RequestIdCache GetInstance()
        {
            return INSTANCE;
        }

        private string GetKey(string deviceId, string flowNo)
        {
            return deviceId + SEPARATOR + flowNo;
        }

        public void SetRequestId(string deviceId, string flowNo, string requestId)
        {
            cache.Set(GetKey(deviceId, flowNo), requestId, new MemoryCacheEntryOptions() {
                SlidingExpiration = TimeSpan.FromMinutes(3),
                Size = 1
            });
        }

        public string RemoveRequestId(string deviceId, string flowNo)
        {
            string key = GetKey(deviceId, flowNo);
            try {
                string value = cache.Get(key).ToString();
                cache.Remove(key);
                return value;
            } catch (Exception e) {
                Log.Warn("getRequestId error : {0} for key: {1}", e.StackTrace, key);
                return null;
            }
        }

    }
}
