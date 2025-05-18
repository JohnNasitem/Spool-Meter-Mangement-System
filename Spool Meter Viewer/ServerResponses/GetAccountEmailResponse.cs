//***********************************************************************************
//Program: GetAccountEmailResponse.cs
//Description: Send request to get account email
//Date: Mar 20, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************



using Newtonsoft.Json;

namespace Spool_Meter_Viewer.ServerResponses
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetAccountEmailResponse"/>
    /// </summary>
    /// <param name="message">Server response message</param>
    /// <param name="email">Email of account related to access token</param> 
    [method: JsonConstructor]
    public class GetAccountEmailResponse(string message,  string email)
    {
        /// <summary>
        /// Server message
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; } = message;



        /// <summary>
        /// Email of account related to access token
        /// </summary>
        [JsonProperty("email")]
        public string Email { get; } = email;
    }
}
