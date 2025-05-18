//***********************************************************************************
//Program: GetAccountNotificationSettings.cs
//Description: Send request to get account notification settings
//Date: Mar 22, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************



using Newtonsoft.Json;

namespace Spool_Meter_Viewer.ServerResponses
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetAccountNotificationSettings"/>
    /// </summary>
    /// <param name="message">Server response message</param>
    /// <param name="spoolMeterBatteryLow">Should a push notification be sent when the spool meter battery is low</param>
    /// <param name="spoolMeterDied">Should a push notification be sent when the spool meter battery is dead</param>
    /// <param name="materialLow">Should a push notification be sent when the remaining material is low</param>
    /// <param name="materialRanOut">Should a push notification be sent when the remaining material ran out</param>
    [method: JsonConstructor]
    public class GetAccountNotificationSettings(string message, bool spoolMeterBatteryLow, bool spoolMeterDied, bool materialLow, bool materialRanOut)
    {
        /// <summary>
        /// Server message
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; } = message;



        /// <summary>
        /// Should a push notification be sent when the spool meter battery is low
        /// </summary>
        [JsonProperty("spoolMeterBatteryLow")]
        public bool? SpoolMeterBatteryLow { get; } = spoolMeterBatteryLow;



        /// <summary>
        /// Should a push notification be sent when the spool meter battery is dead
        /// </summary>
        [JsonProperty("spoolMeterDied")]
        public bool? SpoolMeterDied { get; } = spoolMeterDied;



        /// <summary>
        /// Should a push notification be sent when the remaining material is low
        /// </summary>
        [JsonProperty("materialLow")]
        public bool? MaterialLow { get; } = materialLow;



        /// <summary>
        /// Should a push notification be sent when the remaining material ran out
        /// </summary>
        [JsonProperty("materialRanOut")]
        public bool? MaterialRanOut { get; } = materialRanOut;
    }
}