//***********************************************************************************
//Program: MaterialTypesResponse.cs
//Description: Material type get api call response
//Date: Mar 12, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************



using Newtonsoft.Json;
using Spool_Meter_Viewer.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Spool_Meter_Viewer.ServerResponses
{
    [method: JsonConstructor]
    public class MaterialTypesResponse(string message, object materialTypes)
    {
        [JsonProperty("message")]
        public string Message { get; set; } = message;

        [JsonProperty("materialTypes")]
        public object MaterialTypes { get; set; } = materialTypes;
    }
}
