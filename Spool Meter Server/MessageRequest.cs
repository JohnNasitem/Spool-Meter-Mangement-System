//***********************************************************************************
//Program: MessageRequest.cs
//Description: Message request template
//Date: Mar 30, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************



namespace Spool_Meter_Server
{
    /// <summary>
    /// Message request template
    /// </summary>
    /// <param name="title">title of notification</param>
    /// <param name="body">body of notification</param>
    /// <param name="deviceToken">fcm token of device</param>
    public class MessageRequest(string title, string body, string deviceToken) 
    {
        public string Title { get; set; } = title;
        public string Body { get; set; } = body;
        public string DeviceToken { get; set; } = deviceToken;
    }
}
