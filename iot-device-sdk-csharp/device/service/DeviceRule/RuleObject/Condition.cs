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
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace IoT.SDK.Device.Service.DeviceRule
{
    public class Condition : IDisposable
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        public void Dispose()
        {
            timer?.Dispose();
        }

        public void Init()
        {
            if (Type == "DAILY_TIMER")
            {
                InitializeDailyTimer();
            }
            else if (Type == "SIMPLE_TIMER")
            {
                InitializeSimpleTimer();
            }
        }


        private void InitializeDailyTimer()
        {
            var now = DateTime.UtcNow;
            var conditionTime = TimeSpan.Parse(Time);

            var timeUntilTrigger = conditionTime - now.TimeOfDay;
            if (timeUntilTrigger < TimeSpan.Zero)
            {
                timeUntilTrigger += TimeSpan.FromDays(1);
            }

            LOG.Info("daily timer will start after {}", timeUntilTrigger);
            timer = new Timer(DailyTimerCallback, null, timeUntilTrigger, TimeSpan.FromDays(1));
        }

        private void InitializeSimpleTimer()
        {
            if (IsSimpleTimerBeyondLastTriggeringTime())
            {
                LOG.Debug("simple timer won't be started because it already ends");
                return;
            }

            var startTime = DateTime.Parse(StartTime);
            var interval = TimeSpan.FromSeconds((double)RepeatInterval);
            var timeUntilTrigger = startTime - DateTime.UtcNow;
            if (timeUntilTrigger.TotalSeconds < 0)
            {
                var nextOccurrencesSeconds = interval.TotalSeconds *
                                             Math.Ceiling(-timeUntilTrigger.TotalSeconds / interval.TotalSeconds);
                timeUntilTrigger = startTime + TimeSpan.FromSeconds(nextOccurrencesSeconds) - DateTime.UtcNow;
            }

            LOG.Info("simple timer will start after {}", timeUntilTrigger);
            timer = new Timer(SimpleTimerCallback, null, timeUntilTrigger, interval);
        }

        private bool IsSimpleTimerBeyondLastTriggeringTime()
        {
            var startTime = DateTime.Parse(StartTime);
            var lastTriggeringTime =
                startTime + TimeSpan.FromSeconds(((double)RepeatCount) * (double)RepeatInterval);
            return (DateTime.UtcNow - lastTriggeringTime).TotalSeconds > 0;
        }


        private void DailyTimerCallback(object state)
        {
            var now = DateTime.UtcNow;
            // 判断当前日期是否在条件中指定的日期内
            if (DaysOfWeek.ContainDayOfWeek(now.DayOfWeek))
            {
                TimerDueCallback(this);
            }
        }

        private void SimpleTimerCallback(object state)
        {
            if (IsSimpleTimerBeyondLastTriggeringTime())
            {
                timer.Dispose();
                LOG.Debug("timer disposed");
            }
            else
            {
                TimerDueCallback(this);
            }
        }


        [JsonIgnore]
        private Timer timer;

        [JsonIgnore]
        public Action<Condition> TimerDueCallback;

        public RuleDaysOfWeek DaysOfWeek { get; set; }
        public string Type { get; set; }
        public string Operator { get; set; }
        public DeviceInfo DeviceInfo { get; set; }
        public JValue Value { get; set; }

        public JArray InValues { get; set; }

        public string Time { get; set; }

        public string StartTime { get; set; }
        public int? RepeatInterval { get; set; }
        public int? RepeatCount { get; set; }
    }
}