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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IoT.SDK.Device.Client.Requests;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using Renci.SshNet;

namespace IoT.SDK.Device.Service.TunnelManager
{
    internal class RemoteLoginConnectionProcessor
    {
        private const string OPERATION_TYPE = "operation_type";
        private const string TUNNEL_SERVICE_TYPE = "tunnel_service_type";
        private const string REQUEST_ID = "request_id";
        private const string DATA = "data";


        private const int TUNNEL_TERMINAL_COLUMNS = 128;
        private const int TUNNEL_TERMINAL_ROWS = 30;
        private const int TUNNEL_TERMINAL_WIDTH = TUNNEL_TERMINAL_COLUMNS * 10;
        private const int TUNNEL_TERMINAL_HEIGHT = TUNNEL_TERMINAL_ROWS * 25;
        private const int TUNNEL_TERMINAL_BUFFER_SIZE = 1024;

        internal class RemoteLoginOperation
        {
            [JsonProperty(OPERATION_TYPE)]
            public string OperationType { get; set; }

            [JsonProperty(TUNNEL_SERVICE_TYPE)]
            public string TunnelServiceType { get; set; }

            [JsonProperty(REQUEST_ID)]
            public string RequestId { get; set; }

            [JsonProperty(DATA)]
            public JToken Data { get; set; }
        }

        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        private readonly TunnelManagerService tunnelManagerService;
        private SshClient sshClient;
        private ShellStream sshShell;
        private ClientWebSocket wssClient;
        private readonly CancellationTokenSource cts;

        public RemoteLoginConnectionProcessor(TunnelManagerService tunnelManagerService, string tunnelUri,
            string tunnelAccessToken)
        {
            this.tunnelManagerService = tunnelManagerService;
            cts = new CancellationTokenSource();
            sshClient = null;
            wssClient = null;
#pragma warning disable CS4014
            ConnectWebSocket(tunnelUri, tunnelAccessToken);
#pragma warning restore CS4014
        }


        private async Task ConnectWebSocket(string tunnelUri, string tunnelAccessToken)
        {
            try
            {
                wssClient = new ClientWebSocket
                {
                    Options =
                    {
                        KeepAliveInterval =
                            tunnelManagerService.TunnelWssClientPingDelay // interval to send pong frames 
                    }
                };
                string file =
                    @"C:\Users\a30038357\AppData\Roaming\WeLink_Desktop\appdata\IM\a30038357\ReceiveFiles\plt-device-ca(1).crt"; // Contains name of certificate file
                X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
                wssClient.Options.SetRequestHeader("tunnel_access_token", tunnelAccessToken);
                wssClient.Options.RemoteCertificateValidationCallback = (sender, certificate, chain, errors) => true;
                await wssClient.ConnectAsync(new Uri(tunnelUri), cts.Token);
                LOG.Info("web socket connected");

                while (wssClient.State == WebSocketState.Open)
                {
                    LOG.Debug("start to read message from web socket");
                    await WsGetOneMessage();
                }
            }
            catch (Exception e)
            {
                LOG.Error(e, "remote closed unexpectedly");
                if (wssClient != null)
                {
                    await wssClient.CloseAsync(WebSocketCloseStatus.InternalServerError, e.GetBaseException().Message,
                        cts.Token);
                }
            }
            finally
            {
                LOG.Info("release remote login session");
                if (wssClient != null && wssClient.State != WebSocketState.Closed)
                {
                    await wssClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cts.Token);
                }

                wssClient?.Dispose();
                sshShell?.Close();
                sshClient?.Disconnect();
                sshClient?.Dispose();
            }
        }

        private async Task WsGetOneMessage()
        {
            using var ms = new MemoryStream();
            var buffer = new ArraySegment<byte>(new byte[256]);
            WebSocketReceiveResult result;
            do
            {
                result = await wssClient.ReceiveAsync(buffer, cts.Token);
                if (buffer.Array != null) ms.Write(buffer.Array, 0, result.Count);
            } while (!result.EndOfMessage);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await wssClient.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty,
                    CancellationToken.None);
            }
            else
            {
                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms);
                var message = await reader.ReadToEndAsync();
                var op = JsonConvert.DeserializeObject<RemoteLoginOperation>(message);

                if (op.TunnelServiceType != "ssh")
                {
                    LOG.Error("{} must be ssh", TUNNEL_SERVICE_TYPE);
                    return;
                }

                if (op.Data == null)
                {
                    LOG.Error("{} must not be null", DATA);
                    return;
                }

                switch (op.OperationType)
                {
                    case "connect":
                        await CreateLocalSshClient(op);
                        break;
                    case "disconnect":
                        await CommandDisconnect(op);
                        break;
                    case "command":
                        await CommandCommand(op);
                        break;
                    default:
                        LOG.Error("unknown operation type:{}", op.OperationType);
                        break;
                }
            }
        }

        private async Task CommandCommand([NotNull] RemoteLoginOperation op)
        {
            if (sshClient == null)
            {
                LOG.Error("unexpected command operation");
                return;
            }

            var opData = op.Data.ToString();
            LOG.Debug("execute command {}", opData);

            sshShell.Write(opData);
        }

        private async Task CommandDisconnect([NotNull] RemoteLoginOperation _)
        {
            if (wssClient != null)
            {
                LOG.Debug("start to disconnect remote login");
                await wssClient.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty,
                    CancellationToken.None);
            }
            else
            {
                LOG.Error("unexpected disconnect command");
            }
        }

        private async Task OperationRespond(RemoteLoginOperation rlo)
        {
            var msg = JsonConvert.SerializeObject(rlo);
            await wssClient.SendAsync(Encoding.UTF8.GetBytes(msg), WebSocketMessageType.Text,
                true,
                cts.Token);
        }

        private async Task CreateLocalSshClient([NotNull] RemoteLoginOperation op)
        {
            if (sshClient != null)
            {
                LOG.Error("unexpected connect command");
                return;
            }

            var connectData = op.Data;

            string sshUsername;
            string sshPassword;
            int sshPort = 22;
            try
            {
                sshUsername = (string)connectData["username"];
                sshPassword = (string)connectData["password"];
                try
                {
                    sshPort = (int)connectData["port"];
                }
                catch (Exception)
                {
                    // ignored
                }
            }
            catch (Exception e)
            {
                LOG.Error(e, "parse ssh connection info failed, raw data:{}", connectData);
                return;
            }

            const string host = "10.90.119.216";
            LOG.Debug("ssh connect to {:l}@{:l}:{}", sshUsername, host, sshPort);
            sshClient = new SshClient(host, sshPort, sshUsername, sshPassword);
            await sshClient.ConnectAsync(cts.Token);
            LOG.Debug("ssh client succeed to connect");
            sshShell = sshClient.CreateShellStream("iotda", TUNNEL_TERMINAL_COLUMNS, TUNNEL_TERMINAL_ROWS,
                TUNNEL_TERMINAL_HEIGHT,
                TUNNEL_TERMINAL_WIDTH, TUNNEL_TERMINAL_BUFFER_SIZE);
            sshShell.DataReceived += (sender, args) =>
            {
                var shellOutput = Encoding.UTF8.GetString(args.Data);

                StringBuilder sb = new StringBuilder();
                foreach (char c in shellOutput)
                {
                    switch (c)
                    {
                        case '\b':
                            sb.Append("\\b");
                            break;
                        case '\t':
                            sb.Append("\\t");
                            break;
                        case '\n':
                            sb.Append("\\n");
                            break;
                        case '\v':
                            sb.Append("\\v");
                            break;
                        case '\f':
                            sb.Append("\\f");
                            break;
                        case '\r':
                            sb.Append("\\r");
                            break;
                        case '\"':
                            sb.Append("\\\"");
                            break;
                        case '\'':
                            sb.Append("\\\'");
                            break;
                        case '\\':
                            sb.Append("\\\\");
                            break;
                        default:
                            sb.Append(c);
                            break;
                    }
                }

                System.Console.WriteLine(sb.ToString());
                LOG.Debug("shell output: {}", shellOutput);
                OperationRespond(new RemoteLoginOperation
                {
                    OperationType = "command_response",
                    TunnelServiceType = "ssh",
                    RequestId = op.RequestId,
                    Data = new JValue(shellOutput)
                });
            };
            await OperationRespond(new RemoteLoginOperation
            {
                OperationType = "connect_response",
                TunnelServiceType = "ssh",
                RequestId = op.RequestId,
                Data = new JValue(sshShell.Read())
            });


            LOG.Debug("responded connect success message through wss");
        }
    }

    public class TunnelManagerService : AbstractService
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();


        public TimeSpan TunnelWssClientPingDelay { get; set; } = TimeSpan.FromSeconds(2);

        public override string GetServiceId()
        {
            return "$tunnel_manager";
        }

        public override void OnEvent(DeviceEvent deviceEvent)
        {
            if (!deviceEvent.paras.TryGetValue("tunnel_uri", out var tunnelUri) || !(tunnelUri is string))
            {
                LOG.Error("require a string value but {} is not", "tunnel_url");
                return;
            }

            if (!deviceEvent.paras.TryGetValue("tunnel_access_token", out var tunnelAccessToken) ||
                !(tunnelAccessToken is string))
            {
                LOG.Error("require a string value but {} is not", "tunnel_access_token");
                return;
            }

            var dummy = new RemoteLoginConnectionProcessor(this, (string)tunnelUri, (string)tunnelAccessToken);
        }
    }
}