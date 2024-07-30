﻿/*
 * Copyright (c) 2022-2024 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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
using System.Threading.Tasks;
using NLog;

namespace IoT.SDK.Bridge.Request {
    public class RequestIdCache<T> {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private MemoryCache futureCache;

        private static readonly int MEMORY_CACHE_SIZE_LIMIT = 2000;

        public RequestIdCache()
        {
            futureCache = new MemoryCache(new MemoryCacheOptions() {
                SizeLimit = MEMORY_CACHE_SIZE_LIMIT
            });
        }

        public void SetRequestId2Cache(string requestId, TaskCompletionSource<T> future)
        {
            futureCache.Set(requestId, future, new MemoryCacheEntryOptions() {
                SlidingExpiration = TimeSpan.FromMinutes(3),
                Size = 1
            });
        }

        public void InvalidateCache(string key)
        {
            futureCache.Remove(key);
        }

        public TaskCompletionSource<T> GetFuture(string requestId)
        {
            try {
                TaskCompletionSource<T> value = (TaskCompletionSource<T>)futureCache.Get(requestId);
                InvalidateCache(requestId);
                return value;
            } catch (Exception e) {
                Log.Warn("getRequestId error : {0} for key: {1}", e, requestId);
                return null;
            }
        }
    }
}
