//***********************************************************************************
//Program: Utilities.cs
//Description: Utility methods
//Date: Mar 17, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************



using Spool_Meter_Server.Controllers;
using Spool_Meter_Server.Models;
using System.Security.Cryptography;


namespace Spool_Meter_Server
{
    public static class Utilities
    {
        private static MessageController _messageController = new();



        /// <summary>
        /// Generate a random string at the desired length
        /// </summary>
        /// <param name="length">lenght of string</param>
        /// <returns>random string at desired length</returns>
        /// <exception cref="ArgumentException">throw a new exception when specified length is less than 1</exception>
        public static string GenerateRandomString(int length)
        {
            if (length <= 0)
                throw new ArgumentException("Cannot generate a random string with a negative or 0 length.");

            using var rng = RandomNumberGenerator.Create();
            // Generate a new user ID
            byte[] randomBytes = new byte[length];
            rng.GetBytes(randomBytes);

            // Enforce character length and make url safe
            return Convert.ToBase64String(randomBytes)
                .Substring(0, length)
                .Replace('+', '-')
                .Replace('/', '_');
        }



        /// <summary>
        /// Send a notification to the account connected to the spool meter
        /// </summary>
        /// <param name="spoolMeterId">id of spool meter</param>
        /// <param name="notifType">notification type</param>
        public static async void SendNotification(string spoolMeterId, NotificationType notifType)
        {
            string title = "";
            string body = "";

            if (DatabaseManagement.GetSpoolMeterInfo(spoolMeterId) is not SpoolMeter spoolMeter)
                return;
            
            // TODO: GetSpoolMeter sets accounts to [] but we need the account

            if (DatabaseManagement.GetAccountInfo(spoolMeter.Accounts.First().Id) is not Account account)
                return;

            var (_, _, Settings) = DatabaseManagement.GetAccountNotificationSettings(account.Id);

            if (Settings == null)
                return;

            switch (notifType)
            {
                case NotificationType.MaterialLow:
                    // Dont sent nofication if user toggled it off
                    if (!Settings.MaterialLow)
                        return;

                    title = "Low Material Remaing Warning";
                    body = $"There around 10% left of material remaining spool connected to the spool meter: {spoolMeter.Name}";
                    break;
                case NotificationType.MaterialRanOut:
                    // Dont sent nofication if user toggled it off
                    if (!Settings.MaterialRanOut)
                        return;

                    title = "Spool Empty!";
                    body = $"The spool connected to the spool meter: {spoolMeter.Name} is empty!";
                    break;
                case NotificationType.BatteryLow:
                    // Dont sent nofication if user toggled it off
                    if (!Settings.SpoolMeterBatteryLow)
                        return;

                    title = "Low Battery Warning";
                    body = $"The battery of the spool meter: {spoolMeter.Name} is running low";
                    break;
                case NotificationType.BatteryDead:
                    // Dont sent nofication if user toggled it off
                    if (!Settings.SpoolMeterDied)
                        return;

                    title = "Dead Battery!";
                    body = $"The battery of the spool meter: {spoolMeter.Name} is empty!";
                    break;
            }

            foreach (var fcmToken in account.AccountToFcmtokens)
                await _messageController.SendMessageAsync(new MessageRequest(title, body, fcmToken.Fcmtoken));
        }



        public enum NotificationType
        {
            MaterialLow,
            MaterialRanOut,
            BatteryLow,
            BatteryDead,
        }
    }
}
