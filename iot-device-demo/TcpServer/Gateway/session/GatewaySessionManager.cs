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
using System.Threading;
using DotNetty.Transport.Channels;
using IoT.Device.Demo.HubDevice;
using IoT.SDK.Device.Gateway;
using NLog;

namespace IoT.Device.Demo.Gateway
{
    public class GatewaySessionManager : ISessionManager
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();
        private readonly SubDevicesPersistence subDevicesPersistence;
        private readonly Dictionary<string, Session> nodeIdToSessionDic = new Dictionary<string, Session>();
        private readonly Mutex sessionManagementMutex = new Mutex(false);

        public GatewaySessionManager(SubDevicesPersistence subDevicesPersistence)
        {
            this.subDevicesPersistence = subDevicesPersistence;
        }

        public bool RemoveSession(string nodeId, out Session session)
        {
            session = null;
            sessionManagementMutex.WaitOne();
            try
            {
                var res = nodeIdToSessionDic.Remove(nodeId, out session);
                LOG.Info("session removed {}", session);
                return res;
            }
            finally
            {
                sessionManagementMutex.ReleaseMutex();
            }
        }

        public Session TryCreateSession(string nodeId, IChannel channel, out bool isNewSession)
        {
            isNewSession = false;
            var deviceInfo = subDevicesPersistence.GetSubDevice(nodeId);
            // The child device has been added by calling a northbound API.
            if (deviceInfo == null)
            {
                LOG.Info(" gateway doesn't manage such node id  {}", nodeId);
                return null;
            }

            Session session = new Session
            {
                channel = channel,
                nodeId = nodeId,
                deviceId = deviceInfo.deviceId
            };

            sessionManagementMutex.WaitOne();
            try
            {
                if (nodeIdToSessionDic.ContainsKey(nodeId))
                {
                    LOG.Info("not allowed node id {} already have session", nodeId);
                    return null;
                }

                nodeIdToSessionDic.Add(nodeId, session);
                LOG.Info("create new session ok {}", session.ToString());
                isNewSession = true;
                return session;
            }
            finally
            {
                sessionManagementMutex.ReleaseMutex();
            }
        }

        public bool GetSession(string nodeId, out Session session)
        {
            sessionManagementMutex.WaitOne();
            try
            {
                return nodeIdToSessionDic.TryGetValue(nodeId, out session);
            }
            finally
            {
                sessionManagementMutex.ReleaseMutex();
            }
        }
    }
}