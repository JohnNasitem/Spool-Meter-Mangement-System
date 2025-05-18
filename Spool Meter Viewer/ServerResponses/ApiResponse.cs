//***********************************************************************************
//Program: ApiResponse.cs
//Description: Generic Api response
//Date: Mar 12, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************



using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spool_Meter_Viewer.ServerResponses
{
    [method: JsonConstructor]
    internal class ApiResponse(int statusCode, object data)
    {
        [JsonProperty("statusCode")]
        public int StatusCode { get; } = statusCode;

        [JsonProperty("data")]
        public object Data { get; } = data;
    }
}
