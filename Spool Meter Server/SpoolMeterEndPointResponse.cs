//***********************************************************************************
//Program: SpoolMeterEndPointResponse.cs
//Description: Used for custom status codes
//Date: Mar 25, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************



namespace Spool_Meter_Server
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SpoolMeterEndPointResponse"/>
    /// </summary>
    /// <param name="statusCode">response status code</param>
    /// <param name="message">response message</param>
    public class SpoolMeterEndPointResponse(int statusCode, string message)
    {
        /// <summary>
        /// Response status code
        /// </summary>
        public int StatusCode { get; set; } = statusCode;
        /// <summary>
        /// Reponse data
        /// </summary>
        public string Message { get; set; } = message;
    }
}
