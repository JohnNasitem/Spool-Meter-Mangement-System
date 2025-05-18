//***********************************************************************************
//Program: SpoolMeterResponse.cs
//Description: Response class for getting spool meters
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
    public class SpoolMeterResponse(string message, object spoolMeters)
    {
        [JsonProperty("message")]
        public string Message { get; set; } = message;

        [JsonProperty("spoolMeters")]
        public object SpoolMeters { get; set; } = spoolMeters;
    }
}
