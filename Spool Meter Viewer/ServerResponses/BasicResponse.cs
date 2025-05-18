//***********************************************************************************
//Program: BasicResponse.cs
//Description: Basic api response
//Date: Mar 13, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************



using Newtonsoft.Json;

namespace Spool_Meter_Viewer.ServerResponses
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BasicResponse"/>
    /// </summary>
    /// <param name="message">Server response message</param>
    [method: JsonConstructor]
    public class BasicResponse(string message)
    {
        /// <summary>
        /// Server message
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; } = message;
    }
}
