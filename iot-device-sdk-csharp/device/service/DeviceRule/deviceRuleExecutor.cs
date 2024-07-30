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
using System.Globalization;
using System.Linq;
using System.Threading;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Gateway.Requests;
using MQTTnet.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NLog;

namespace IoT.SDK.Device.Service.DeviceRule
{
    public class DeviceRuleExecutor : Disposable
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();
        private readonly Dictionary<string, Rule> rules = new Dictionary<string, Rule>();

        private readonly Dictionary<string, Dictionary<string, JValue>> deviceDataCache =
            new Dictionary<string, Dictionary<string, JValue>>();

        private readonly ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim();
        private readonly ReaderWriterLockSlim ruleLock = new ReaderWriterLockSlim();

        public Action<string> RuleUpdatedAction { get; set; }
        public Action<Command> CommandAction { get; set; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ruleLock.EnterWriteLock();
                try
                {
                    foreach (var item in rules.Values)
                    {
                        item.Dispose();
                    }

                    rules.Clear();
                }
                finally
                {
                    ruleLock.ExitWriteLock();
                }
            }

            base.Dispose(disposing);
        }

        private void NotifyRuleUpdated()
        {
            if (RuleUpdatedAction == null) return;

            string json;
            ruleLock.EnterWriteLock();
            try
            {
                json = JsonConvert.SerializeObject(rules.Values, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    NullValueHandling = NullValueHandling.Ignore
                });
            }
            finally
            {
                ruleLock.ExitWriteLock();
            }

            RuleUpdatedAction(json);
        }

        public void DeleteOrGetLackingRules(Dictionary<string, int> newRuleVersion, out HashSet<string> deletedRuleIds,
            out HashSet<string> lackingRuleIds)
        {
            lackingRuleIds = new HashSet<string>();
            deletedRuleIds = new HashSet<string>();
            ruleLock.EnterWriteLock();
            try
            {
                foreach (var (ruleId, newVersion) in newRuleVersion)
                {
                    rules.TryGetValue(ruleId, out var oldRule);
                    if (newVersion == -1)
                    {
                        if (oldRule != null)
                        {
                            oldRule.Dispose();
                            rules.Remove(oldRule.RuleId);
                        }

                        deletedRuleIds.Add(ruleId);
                    }
                    else if (oldRule == null || newVersion > oldRule.RuleVersionInShadow)
                    {
                        lackingRuleIds.Add(ruleId);
                    }
                    else
                    {
                        LOG.Warn("rule {} new version {} <= existing one's {}", ruleId, newVersion,
                            oldRule.RuleVersionInShadow);
                    }
                }
            }
            finally
            {
                ruleLock.ExitWriteLock();
            }

            if (deletedRuleIds.Count > 0)
            {
                NotifyRuleUpdated();
            }
        }

        public List<Rule> UpdateRules(List<Rule> newRules)
        {
            var addRules = new List<Rule>();
            ruleLock.EnterWriteLock();
            try
            {
                newRules.ForEach(newRule =>
                {
                    if (UpdateRuleWhenHoldLock(newRule))
                    {
                        addRules.Add(newRule);
                    }
                });
            }
            finally
            {
                ruleLock.ExitWriteLock();
            }

            if (addRules.Count > 0)
            {
                NotifyRuleUpdated();
            }

            return addRules;
        }


        private bool UpdateRuleWhenHoldLock(Rule newRule)
        {
            if (rules.TryGetValue(newRule.RuleId, out var oldRule))
            {
                if (newRule.RuleVersionInShadow > oldRule.RuleVersionInShadow)
                {
                    oldRule.Dispose();
                    rules.Remove(oldRule.RuleId);
                }
                else
                {
                    LOG.Warn("rule {} new version {} == existing one's {}", newRule.RuleId, newRule.RuleVersionInShadow,
                        oldRule.RuleVersionInShadow);
                    return false;
                }
            }

            rules[newRule.RuleId] = newRule;
            newRule.Conditions.ForEach(condition => condition.TimerDueCallback = EvaluateRules);
            newRule.Init();
            return true;
        }


        public void ReportProperties(List<DeviceProperty> properties)
        {
            bool propertiesSet = false;
            cacheLock.EnterWriteLock();
            try
            {
                foreach (var deviceProperty in properties)
                {
                    var deviceId = deviceProperty.deviceId;
                    foreach (var property in deviceProperty.services.FindAll(v => !v.serviceId.StartsWith("$")))
                    {
                        if (!deviceDataCache.TryGetValue(deviceId, out var devicePropertiesCache))
                        {
                            devicePropertiesCache = new Dictionary<string, JValue>();
                            deviceDataCache.Add(deviceId, devicePropertiesCache);
                        }

                        foreach (var kvp in property.properties)
                        {
                            devicePropertiesCache[$"{property.serviceId}/{kvp.Key}"] = new JValue(kvp.Value);
                            propertiesSet = true;
                        }
                    }
                }
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }

            if (propertiesSet)
            {
                EvaluateRules(null);
            }
        }

        private void EvaluateRules(Condition triggeredCondition)
        {
            ruleLock.EnterReadLock();
            try
            {
                foreach (var rule in rules.Values.Where(r =>
                             r.Status == "active" &&
                             (triggeredCondition == null || r.Conditions.Contains(triggeredCondition))))
                {
                    if (!IsRuleActive(rule.TimeRange)) continue;
                    var isConditionMet = rule.Logic == "and";

                    foreach (var condition in rule.Conditions)
                    {
                        var result = EvaluateCondition(condition, triggeredCondition);
                        if (rule.Logic == "and" && !result)
                        {
                            isConditionMet = false;
                            break;
                        }

                        if (rule.Logic == "or" && result)
                        {
                            isConditionMet = true;
                            break;
                        }
                    }

                    if (isConditionMet)
                    {
                        ExecuteActions(rule.Actions);
                    }
                }
            }
            finally
            {
                ruleLock.ExitReadLock();
            }
        }

        private bool IsRuleActive(TimeRange timeRange)
        {
            if (timeRange == null)
                return true;

            var now = DateTime.UtcNow;
            var daysOfWeek = timeRange.DaysOfWeek;


            var startTime = TimeSpan.Parse(timeRange.StartTime);
            var endTime = TimeSpan.Parse(timeRange.EndTime);
            var timeOfDayMinutes = TimeSpan.FromMinutes(Math.Floor(now.TimeOfDay.TotalMinutes));
            if (endTime < startTime)
            {
                return (timeOfDayMinutes >= startTime && daysOfWeek.ContainDayOfWeek(now.DayOfWeek)) ||
                       (timeOfDayMinutes <= endTime && daysOfWeek.ContainDayOfWeek(now.AddDays(-1).DayOfWeek));
            }
            else
            {
                return (timeOfDayMinutes >= startTime && timeOfDayMinutes <= endTime) &&
                       daysOfWeek.ContainDayOfWeek(now.DayOfWeek);
            }
        }

        private bool EvaluateCondition(Condition condition, Condition triggeredTimeConditionId)
        {
            switch (condition.Type)
            {
                case "DEVICE_DATA":
                    return EvaluateDeviceDataCondition(condition);
                case "DAILY_TIMER":
                case "SIMPLE_TIMER":
                    return condition == triggeredTimeConditionId;
                default:
                    throw new NotSupportedException($"Condition type {condition.Type} not supported");
            }
        }

        private bool EvaluateDeviceDataCondition(Condition condition)
        {
            cacheLock.EnterReadLock();
            try
            {
                if (!deviceDataCache.TryGetValue(condition.DeviceInfo.DeviceId, out var deviceProperties))
                {
                    return false;
                }

                if (!deviceProperties.TryGetValue(condition.DeviceInfo.Path, out var value))
                {
                    return false;
                }

                switch (condition.Operator)
                {
                    case "=":
                    case "<":
                    case ">=":
                    case ">":
                    case "<=":
                        return CompareValues(value, condition.Value, condition.Operator);
                    case "between":
                        var range = condition.Value.Value<string>().Split(',').Select(v => decimal.Parse(v)).ToArray();
                        if (range.Length < 2)
                        {
                            return false;
                        }

                        var lowerBound = new JValue(range[0]);
                        var upperBound = new JValue(range[1]);
                        return CompareValues(value, lowerBound, ">=") && CompareValues(value, upperBound, "<=");
                    case "in":
                        return condition.InValues
                            .OfType<JValue>()
                            .Any(val => val.Value<string>() == value.ToString(CultureInfo.InvariantCulture));
                    default:
                        throw new NotSupportedException($"Operator {condition.Operator} not supported");
                }
            }
            finally
            {
                cacheLock.ExitReadLock();
            }
        }

        private bool CompareValues(JValue value1, JValue value2, string comparisonOperator)
        {
            try
            {
                int comparisonResult = value1.CompareTo(value2);
                switch (comparisonOperator)
                {
                    case "=":
                        return comparisonResult == 0;
                    case "<":
                        return comparisonResult < 0;
                    case ">=":
                        return comparisonResult >= 0;
                    case ">":
                        return comparisonResult > 0;
                    case "<=":
                        return comparisonResult <= 0;
                    default:
                        LOG.Error($"Comparison operator {comparisonOperator} not supported");
                        return false;
                }
            }
            catch (FormatException e)
            {
                return false;
            }
        }


        private void ExecuteActions(List<Action> actions)
        {
            foreach (var action in actions.Where(a => a.Status == "enable"))
            {
                if (action.Type != "DEVICE_CMD")
                {
                    LOG.Warn("unknown action type: {}", action.Type);
                    return;
                }

                var command = new Command
                {
                    serviceId = action.Command.ServiceId,
                    commandName = action.Command.CommandName,
                    deviceId = action.DeviceId,
                    paras = action.Command.CommandBody
                };
                // Simulate sending the command
                LOG.Debug($"Executing command: {command.commandName} on device: {command.deviceId}");
                CommandAction(command);
            }
        }
    }
}