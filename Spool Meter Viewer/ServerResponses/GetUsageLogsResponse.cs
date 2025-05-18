//***********************************************************************************
//Program: GetUsageLogsResponse.cs
//Description: Send request to get account usage logs
//Date: Mar 26, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************



using Newtonsoft.Json;
using Spool_Meter_Viewer.Classes;

namespace Spool_Meter_Viewer.ServerResponses
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetUsageLogsResponse"/>
    /// </summary>
    /// <param name="message"></param>
    /// <param name="logs"></param>
    public class GetUsageLogsResponse(string message, List<UsageLog> logs)
    {
        /// <summary>
        /// Server message
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; } = message;



        /// <summary>
        /// usage logs related to the account
        /// </summary>
        [JsonProperty("logs")]
        public List<UsageLog> Logs { get; } = logs;
    }
}
