/*Copyright (c) <2020>, <Huawei Technologies Co., Ltd>
 * All rights reserved.
 * &Redistribution and use in source and binary forms, with or without modification,
 * are permitted provided that the following conditions are met:
 * 1. Redistributions of source code must retain the above copyright notice, this list of
 * conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice, this list
 * of conditions and the following disclaimer in the documentation and/or other materials
 * provided with the distribution.
 * 3. Neither the name of the copyright holder nor the names of its contributors may be used
 * to endorse or promote products derived from this software without specific prior written permission.
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF
 * USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
 * OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 *
 * */

using System.Security.Cryptography;
using System.Text;

namespace IoT.SDK.Device.Utils
{
    public static class EncryptUtil
    {
        /// <summary>
        /// 加密算法HmacSHA256
        /// </summary>
        /// <param name="secret"></param>
        /// <param name="signTimestampKey"></param>
        /// <returns></returns>
        public static string HmacSHA256(string secret, string signTimestampKey)
        {
            string signRet = string.Empty;
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signTimestampKey)))
            {
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(secret));
                
                signRet = ToHexString(hashBytes);
            }

            return signRet;
        }

        /// <summary>
        /// byte[]转16进制格式string
        /// </summary>
        /// <param name="hashBytes"></param>
        /// <returns></returns>
        private static string ToHexString(byte[] hashBytes)
        {
            string hexStr = string.Empty;
            if (hashBytes != null)
            {
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.AppendFormat("{0:x2}", b);
                }

                hexStr = sb.ToString();
            }

            return hexStr;
        }
    }
}
