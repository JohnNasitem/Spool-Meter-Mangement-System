//***********************************************************************************
//Program: AddSpoolMeterResponse.cs
//Description: Add spool meter api response
//Date: Mar 13, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************



using Newtonsoft.Json;

namespace Spool_Meter_Viewer.ServerResponses
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddSpoolMeterResponse"/>
    /// </summary>
    /// <param name="message">message from server</param>
    /// <param name="spoolMeterPassword">newly generated spool meter password</param>
    [method: JsonConstructor]
    public class AddSpoolMeterResponse(string message, string spoolMeterPassword)
    {
        /// <summary>
        /// Server message
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; } = message;



        /// <summary>
        /// Newly generated spool meter password
        /// </summary>
        [JsonProperty("spoolMeterPassword")]
        public string Password { get; } = spoolMeterPassword;
    }
}
