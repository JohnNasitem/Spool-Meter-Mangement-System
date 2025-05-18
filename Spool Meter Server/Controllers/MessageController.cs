//***********************************************************************************
//Program: MessageController.cs
//Description: Notification controller
//Date: Mar 30, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************



using Microsoft.AspNetCore.Mvc;
using FirebaseAdmin.Messaging;
using Azure.Core;
using Spool_Meter_Server.Models;

namespace Spool_Meter_Server.Controllers
{
    /// <summary>
    /// Notification controller
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class MessageController : ControllerBase
    {
        /// <summary>
        /// Send notification to a device
        /// </summary>
        /// <param name="request">request</param>
        /// <returns>IActionResult</returns>
        /// <exception cref="Exception">throw new exception if there was a problem sending the message</exception>
        [HttpPost]
        public async Task<IActionResult> SendMessageAsync([FromBody] MessageRequest request)
        {
            var message = new Message()
            {
                Notification = new Notification
                {
                    Title = request.Title,
                    Body = request.Body,
                },
                Token = request.DeviceToken
            };

            try
            {
                var messaging = FirebaseMessaging.DefaultInstance;
                var result = await messaging.SendAsync(message);

                if (!string.IsNullOrEmpty(result))
                {
                    // Message was sent successfully
                    return Ok("Message sent successfully!");
                }
                else
                {
                    // There was an error sending the message
                    throw new Exception("Error sending the message.");
                }
            }
            catch (FirebaseMessagingException ex)
            {
                // Check if the token is invalid or the device is unregistered
                if (ex.MessagingErrorCode == MessagingErrorCode.Unregistered ||
                    ex.MessagingErrorCode == MessagingErrorCode.InvalidArgument)
                {
                    Console.WriteLine($"SendMessageAsync() - Token is invalid: {request.DeviceToken}, removing from database.");
                    DatabaseManagement.RemoveFCMToken(request.DeviceToken);
                }
                else
                {
                    Console.WriteLine($"SendMessageAsync() - Failed to send a notifcation message to token. Error: {ex}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SendMessageAsync() - Failed to send a notifcation message to token. Error: {ex}");
            }

            return Problem("Failed to send the message");
        }
    }
}
