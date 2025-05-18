//***********************************************************************************
//Program: GetUsageLogsResponse.cs
//Description: Send request to get all usage logs related to the account
//Date: Mar 26, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************



using Newtonsoft.Json;

namespace Spool_Meter_Viewer.Classes
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UsageLog"/>
    /// </summary>
    /// <param name="id">id of usage log</param>
    /// <param name="spoolMeterId">id of spool meter usage log is for</param>
    /// <param name="time">time usage log was created</param>
    /// <param name="remainingAmountPercentage">remaining amount % at the time</param>
    [method: JsonConstructor]
    public class UsageLog(int id, string spoolMeterId, DateTime time, double remainingAmountPercentage) : IComparable
    {
        /// <summary>
        /// Id of usage log
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; } = id;



        /// <summary>
        /// Id of spool meter usage log is for
        /// </summary>
        [JsonProperty("spoolMeterId")]
        public string SpoolMeterId { get; set; } = spoolMeterId;



        /// <summary>
        /// Time usage log was created
        /// </summary>
        [JsonProperty("time")]
        public DateTime Time { get; set; } = time;



        /// <summary>
        /// Remaining amount % at the time of the log
        /// </summary>
        [JsonProperty("remainingAmountPercentage")]
        public double RemainingAmountPercentage { get; set; } = remainingAmountPercentage;



        /// <summary>
        /// Compare with specified <see cref="UsageLog"/> based on creation time.
        /// </summary>
        /// <param name="obj">obj to compare to</param>
        /// <returns>Negative - Instance creation time is earlier than specified instance<br/>Zero - Creation time of both instances are the same<br/>Positive - Instance creation time is later thant specified instance</returns>
        /// <exception cref="ArgumentException">throw new exception if specified object is not another UsageLog</exception>
        public int CompareTo(object? obj)
        {
            if (obj is not UsageLog other)
                throw new ArgumentException("Cannot compare to an object that is not UsageLog");

            return Time.CompareTo(other.Time);
        }
    }
}
