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
using IoT.SDK.Device.Service.FileManager;
using IoT.SDK.Device.Utils;
using NLog;

namespace IoT.Device.Demo
{
    public class FileUploadDownloadSample : DeviceSample
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();


        protected override void RunDemo()
        {
            var fsa = new SimpleLocalFileManager(Device.FileManagerService);

            void UploadCompleteListener(SimpleLocalFileManager.FileTransferRequest req, Exception e)
            {
                if (e != null)
                {
                    LOG.Error(e, "upload file failed");
                    return;
                }

                // download the uploaded NLOG.config with generated new name(e.g. Nlog-20240527-034141-867.config)
                fsa.DownloadFile(new SimpleLocalFileManager.FileTransferRequest
                {
                    FileName = req.FileName, // filename in obs
                    Path = IotUtil.GetRootDirectory() + @"\" + req.FileName
                });
            }

            // upload the NLog.config under the production directory
            var fileName = fsa.UploadFile(new SimpleLocalFileManager.FileTransferRequest
            {
                Path = IotUtil.GetRootDirectory() + @"\Nlog.config",
                CompleteListener = UploadCompleteListener
            });
            LOG.Info("filename to be uploaded is {}", fileName);
        }
    }
}