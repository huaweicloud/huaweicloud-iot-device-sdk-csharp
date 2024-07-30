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
using IoT.SDK.Device.Client;

namespace IoT.SDK.Device.Transport.Mqtt
{
    public class ExponentialBackoff
    {
        private readonly Random random = new Random();
        private int adverseEventCount = 0;
        public bool IsCleared => adverseEventCount == 0;
        public long MinBackoff { get; set; } = 1000;
        public long MaxBackoff { get; set; } = 4 * 60 * 1000; // 4 minutes
        public long DefaultBackoff { get; set; } = 1000;
        public double ExpBase { get; set; } = 2;
        public double JitterLowerBoundMultiplier { get; set; } = 0.8;
        public double JitterHigherBoundMultiplier { get; set; } = 1.2;

        public void Clear()
        {
            adverseEventCount = 0;
        }

        public long TimeDelay
        {
            get
            {
                var lowBound = (int)(DefaultBackoff * JitterLowerBoundMultiplier);
                var highBound = (int)(DefaultBackoff * JitterHigherBoundMultiplier);
                var backOffWithJitter =
                    (long)Math.Pow(ExpBase, adverseEventCount - 1) * random.Next(lowBound, highBound);
                var res = Math.Max(MinBackoff, Math.Min(MaxBackoff, backOffWithJitter));
                return res;
            }
        }


        public void IncAdverseEvent()
        {
            var waitTimeUtilNextRetry = TimeDelay;
            if (waitTimeUtilNextRetry < MaxBackoff)
            {
                adverseEventCount++;
            }
        }
    }
}