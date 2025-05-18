//***********************************************************************************
//Program: LoginResponse.cs
//Description: Login api response
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
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginResponse"/>
    /// </summary>
    /// <param name="message">Server response message</param>
    /// <param name="accessToken">Access token provided when log in is successful</param>
    [method: JsonConstructor]
    public class LoginResponse(string message, string accessToken, string refreshToken)
    {
        /// <summary>
        /// Server message
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; } = message;



        /// <summary>
        /// Access token if log in is successful
        /// </summary>
        [JsonProperty("accessToken")]
        public string AccessToken { get; } = accessToken;



        /// <summary>
        /// Refresh token if log in is successful
        /// </summary>
        [JsonProperty("refreshToken")]
        public string RefreshToken { get; } = refreshToken;
    }
}
