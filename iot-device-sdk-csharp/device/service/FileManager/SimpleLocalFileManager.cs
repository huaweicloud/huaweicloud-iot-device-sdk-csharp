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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using Newtonsoft.Json;
using NLog;

namespace IoT.SDK.Device.Service.FileManager
{
    public class SimpleLocalFileManager : FileManagerListener
    {
        private const string FILE_SESSION_ID = "file_session_id";

        public Func<string, bool, string> FileNameGenerator { get; set; } = (path, isDownload) =>
        {
            if (isDownload)
            {
                return Path.GetFileName(path);
            }
            else
            {
                return Path.GetFileNameWithoutExtension(path) + "-" + DateTime.Now.ToString("yyyyMMdd-hhmmss-fff") +
                       Path.GetExtension(path);
            }
        };


        public class FileTransferRequest : ICloneable
        {
            /// <summary>
            ///In uploading, fileName shown in obs storage, automatically get from path if it's <c>null</c>
            /// </summary>
            public string Path { get; set; }

            /// <summary>
            ///In both uploading or downloading, this is the fileName shown in obs storage, automatically get from path if it's <c>null</c>
            /// </summary>
            public string FileName { get; set; }

            /// <summary>
            /// extra file attributes
            /// </summary>
            public Dictionary<string, object> FileAttributes { get; set; }

            [JsonIgnore]
            public Action<FileTransferRequest, Exception> CompleteListener { get; set; }

            public object Clone()
            {
                return new FileTransferRequest
                {
                    Path = Path,
                    FileName = FileName,
                    FileAttributes =
                        JsonConvert.DeserializeObject<Dictionary<string, object>>(
                            JsonConvert.SerializeObject(FileAttributes)),
                    CompleteListener = CompleteListener
                };
            }
        }

        private class FileTransferSession
        {
            public FileTransferRequest FileTransferRequest { get; set; }
            public DateTime StartTime { get; set; }
            public TimeSpan ExpireInterval { get; set; }
        }

        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();
        private readonly FileManagerService fileManagerService;

        private readonly ConcurrentDictionary<string, FileTransferSession> fileTransferSessionMap =
            new ConcurrentDictionary<string, FileTransferSession>();

        public SimpleLocalFileManager(FileManagerService service)
        {
            fileManagerService = service;
            service.fileManagerListener = this;
        }


        protected virtual string GetFileHash(string path)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(path))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "");
                }
            }
        }

        private void ClearExpiredSession()
        {
            var expiredFileName = new List<string>();
            foreach (var fileTransferSession in fileTransferSessionMap)
            {
                if (fileTransferSession.Value.StartTime + fileTransferSession.Value.ExpireInterval < DateTime.Now)
                {
                    continue;
                }

                expiredFileName.Add(fileTransferSession.Key);
            }

            foreach (var s in expiredFileName)
            {
                fileTransferSessionMap.TryRemove(s, out _);
            }
        }

        private UrlRequest CreateFileTransferSession(FileTransferRequest req)
        {
            ClearExpiredSession();
            if (req.FileName == null)
            {
                req.FileName = FileNameGenerator(req.Path, true);
            }

            var session = new FileTransferSession
            {
                FileTransferRequest = req,
                StartTime = DateTime.Now,
                ExpireInterval = TimeSpan.FromSeconds(5 * 60)
            };

            while (true)
            {
                var sessionId = Guid.NewGuid().ToString("N");
                req.FileAttributes.Add(FILE_SESSION_ID, sessionId);
                if (fileTransferSessionMap.TryAdd(sessionId, session))
                {
                    break;
                }
            }

            return new UrlRequest
            {
                fileName = req.FileName,
                fileAttributes = req.FileAttributes
            };
        }


        private FileTransferSession GetAndRemoveFileTransferSession(UrlResponse response)
        {
            var fAttr = response.fileAttributes;
            if (fAttr == null || !fAttr.TryGetValue(FILE_SESSION_ID, out var fileSessionIdObj))
            {
                LOG.Error("can't find file session id");
                return null;
            }

            // 处理对应session
            if (fileSessionIdObj is string fileSessionId
                && fileTransferSessionMap.TryRemove(fileSessionId, out var session)) return session;

            LOG.Error("object name {} has no related file path", response.objectName);
            return null;
        }

        /// <summary>
        /// upload file
        /// </summary>
        /// <param name="reqIn">upload file request</param>
        public string UploadFile(FileTransferRequest reqIn)
        {
            var fReq = (FileTransferRequest)reqIn.Clone();
            if (fReq.FileName == null)
            {
                fReq.FileName = FileNameGenerator(fReq.Path, false);
            }

            if (fReq.FileAttributes == null)
            {
                fReq.FileAttributes = new Dictionary<string, object>
                {
                    { "hash_code", GetFileHash(fReq.Path) },
                    { "size", new FileInfo(fReq.Path).Length },
                };
            }

            var req = CreateFileTransferSession(fReq);


            fileManagerService.GetUploadUrl(req);
            return req.fileName;
        }

        /// <summary>
        /// upload file
        /// </summary>
        /// <param name="reqIn">download file request</param>
        public void DownloadFile(FileTransferRequest reqIn)
        {
            var fReq = (FileTransferRequest)reqIn.Clone();
            if (fReq.FileName == null)
            {
                fReq.FileName = FileNameGenerator(fReq.Path, true);
            }

            if (fReq.FileAttributes == null)
            {
                fReq.FileAttributes = new Dictionary<string, object>();
            }

            var req = CreateFileTransferSession(fReq);


            fileManagerService.GetDownloadUrl(req);
        }

        protected virtual OpFileStatusRequest GetStatusForReport(string objectName, string action,
            Exception e)
        {
            // 上报文件上传结果
            var opFileStatusRequest = new OpFileStatusRequest
            {
                objectName = objectName,
                resultCode = 0,
                statusCode = 200,
                statusDescription = $"{action} file success"
            };

            if (e == null)
            {
                PrintProgressBar(100, 0, $"{action} file");
                return opFileStatusRequest;
            }


            opFileStatusRequest.resultCode = 1;
            opFileStatusRequest.statusCode = (int)HttpStatusCode.InternalServerError;
            opFileStatusRequest.statusDescription = e.GetBaseException().Message;

            return opFileStatusRequest;
        }

        private void UploadFileCompletedCallback(object sender, UploadFileCompletedEventArgs e)
        {
            var req = (FileTransferRequest)e.UserState;
            try
            {
                // 上报文件上传结果
                fileManagerService.ReportUploadFileStatus(GetStatusForReport(req.FileName, "uploading", e.Error));
            }
            finally

            {
                req.CompleteListener?.Invoke(req, e.Error);
            }
        }

        private void DownloadFileCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            // 上报文件上传结果
            var req = (FileTransferRequest)e.UserState;
            try
            {
                fileManagerService.ReportDownloadFileStatus(GetStatusForReport(req.FileName, "downloading", e.Error));
            }
            finally
            {
                req.CompleteListener?.Invoke(req, e.Error);
            }
        }

        private static void PrintProgressBar(int progress, long speedInByte, string prefixMessage)
        {
            const int totalProgressBar = 10;
            int currentProgressBar = progress * totalProgressBar / 100;
            LOG.Debug("{:l}: [{:l}{:l}], {} B/s", prefixMessage, new string('=', currentProgressBar),
                new string(' ', totalProgressBar - currentProgressBar), speedInByte);
        }

        private void UploadProgressCallback(object sender, UploadProgressChangedEventArgs e)
        {
            PrintProgressBar(e.ProgressPercentage, e.BytesSent, "uploading file");
        }

        private void DownloadProgressCallback(object sender, DownloadProgressChangedEventArgs e)
        {
            PrintProgressBar(e.ProgressPercentage, e.BytesReceived, "downloading file");
        }

        public void OnUploadUrl(UrlResponse response)
        {
            var session = GetAndRemoveFileTransferSession(response);
            if (session == null)
            {
                return;
            }

            if (!File.Exists(session.FileTransferRequest.Path))
            {
                LOG.Error("file {} doesn't exist or is not a file", session.FileTransferRequest.Path);
                return;
            }


            try
            {
                var uploadUri = new Uri(response.url);

                var wc = new WebClient();
                wc.Headers.Add("Content-Type", "text/plain");
                wc.Headers.Add("Host", uploadUri.Host);
                wc.UploadFileCompleted += UploadFileCompletedCallback;
                wc.UploadProgressChanged += UploadProgressCallback;
                wc.UploadFileAsync(uploadUri, "PUT", session.FileTransferRequest.Path, session.FileTransferRequest);
            }
            catch (Exception e)
            {
                session.FileTransferRequest.CompleteListener?.Invoke(session.FileTransferRequest, e);

                LOG.Error(e, "start to upload file failed");
            }
        }

        public void OnDownloadUrl(UrlResponse response)
        {
            var session = GetAndRemoveFileTransferSession(response);
            if (session == null)
            {
                return;
            }

            try
            {
                if (File.Exists(session.FileTransferRequest.Path))
                {
                    LOG.Warn("file {} already exist , it'll be overwritten", session.FileTransferRequest.Path);
                }

                var downloadUri = new Uri(response.url);

                var wc = new WebClient();
                wc.Headers.Add("Content-Type", "text/plain");
                wc.Headers.Add("Host", downloadUri.Host);
                wc.DownloadFileCompleted += DownloadFileCompletedCallback;
                wc.DownloadProgressChanged += DownloadProgressCallback;
                wc.DownloadFileAsync(downloadUri, session.FileTransferRequest.Path, session.FileTransferRequest);
            }
            catch (Exception e)
            {
                session.FileTransferRequest.CompleteListener?.Invoke(session.FileTransferRequest, e);

                LOG.Error(e, "start to download file failed");
            }
        }
    }
}