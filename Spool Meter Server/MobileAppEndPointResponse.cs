//***********************************************************************************
//Program: EndPointResponse.cs
//Description: Used for custom status codes
//Date: Mar 17, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************



namespace Spool_Meter_Server
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MobileAppEndPointResponse"/>
    /// </summary>
    /// <param name="statusCode">response status code</param>
    /// <param name="data">response data</param>
    public class MobileAppEndPointResponse(int statusCode, object data)
    {
        /// <summary>
        /// Response status code
        /// </summary>
        public int StatusCode { get; set; } = statusCode;
        /// <summary>
        /// Reponse data
        /// </summary>
        public object Data { get; set; } = data;
    }
}
