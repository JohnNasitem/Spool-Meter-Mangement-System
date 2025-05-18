//***********************************************************************************
//Program: GetSpoolMeterResponse.cs
//Description: Response class for getting spool meter
//Date: Apr 2, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************



using Newtonsoft.Json;
using Spool_Meter_Viewer.Classes;

namespace Spool_Meter_Viewer.ServerResponses
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetSpoolMeterResponse"/>
    /// </summary>
    /// <param name="message">Server response message</param>
    /// <param name="spoolMeter">Spool meter</param>
    [method: JsonConstructor]
    public class GetSpoolMeterResponse(string message, SpoolMeter spoolMeter)
    {
        /// <summary>
        /// Server message
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; } = message;



        /// <summary>
        /// Spool meter
        /// </summary>
        [JsonProperty("spoolMeter")]
        public SpoolMeter SpoolMeter { get; set; } = spoolMeter;
    }
}
