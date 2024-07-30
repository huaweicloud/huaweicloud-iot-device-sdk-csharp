/*
 * Copyright (c) 2020-2024 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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
using Newtonsoft.Json;

namespace IoT.SDK.Device.Utils
{
    public static class JsonUtil
    {
        /// <summary>
        /// Converts an object into a JSON string.
        /// </summary>
        /// <param name="o">Indicates the object.</param>
        /// <returns>Returns a JSON string.</returns>
        public static string ConvertObjectToJsonString(object o)
        {
            return o == null ? null : JsonConvert.SerializeObject(o);
        }

        /// <summary>
        /// Converts a JSON string to an object.
        /// </summary>
        /// <typeparam name="T">Indicates the object class.</typeparam>
        /// <param name="jsonString"></param>
        /// <returns></returns>
        public static T ConvertJsonStringToObject<T>(string jsonString, JsonSerializerSettings settings = null)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(jsonString, settings);
            }
            catch (Exception ex)
            {
                return default(T);
            }
        }


        /// <summary>
        /// Converts a data dictionary to an object.
        /// </summary>
        /// <typeparam name="T">Indicates the object class.</typeparam>
        /// <param name="dic">Indicates the data dictionary.</param>
        /// <returns></returns>
        public static T ConvertDicToObject<T>(Dictionary<string, object> dic) where T : new()
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(dic));
            }
            catch (Exception ex)
            {
                return default(T);
            }
        }

        /// <summary>
        /// Converts JSON string to a data dictionary.
        /// </summary>
        /// <typeparam name="TKey">dictionary key</typeparam>
        /// <typeparam name="TValue">dictionary value</typeparam>
        /// <param name="jsonString">json string</param>
        /// <returns>dictionary data</returns>
        public static Dictionary<TKey, TValue> ConvertJsonStringToDic<TKey, TValue>(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
            {
                return new Dictionary<TKey, TValue>();
            }

            return JsonConvert.DeserializeObject<Dictionary<TKey, TValue>>(jsonString);
        }
    }
}