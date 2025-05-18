//***********************************************************************************
//Program: SpoolMeterEndPoints.cs
//Description: Sets up the end points the spool meter device uses
//Date: Mar 17, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************



using static Spool_Meter_Server.Program;
using Spool_Meter_Server.Models;
using Spool_Meter_Server.Controllers;

namespace Spool_Meter_Server.ServerEndpoints
{
    // Records
    public record UpdateRemainingAmountInfo(string meterId, string password, string NewAmount);
    public record UpdatedBatteryInfo(string meterId, string password, string NewBatteryLevel);



    /// <summary>
    /// Static class used to set end points for the spool meter device
    /// </summary>
    public static class SpoolMeterEndPoints
    {
        /// <summary>
        /// Set up end points for the specified <see cref=WebApplication/>
        /// </summary>
        /// <param name="app">Web application that are end points are being set up in</param>
        public static void SetEndpoints(WebApplication app)
        {
            // Endpoint to handle updating the remaining amount of the specified spool meter id in the database
            app.MapPut("Api/UpdateRemainingAmount", (UpdateRemainingAmountInfo remainingAmountInfo) =>
            {
                // Bad request if data isnt populated
                if (remainingAmountInfo.meterId is null || remainingAmountInfo.meterId.Length == 0)
                    return new SpoolMeterEndPointResponse((int)ResponseCodes.BadRequest, "Must supply a spool meter id!");
                if (remainingAmountInfo.password is null || remainingAmountInfo.password.Length == 0)
                    return new SpoolMeterEndPointResponse((int)ResponseCodes.BadRequest, "Must supply a password!");
                if (remainingAmountInfo.NewAmount is null || remainingAmountInfo.NewAmount.Length == 0)
                    return new SpoolMeterEndPointResponse((int)ResponseCodes.BadRequest, "Must supply the new amount!");

                if (DatabaseManagement.GetSpoolMeterInfo(remainingAmountInfo.meterId) is not SpoolMeter meter ||
                    !DatabaseManagement.VerifyPassword(remainingAmountInfo.password, meter.Password))
                    return new SpoolMeterEndPointResponse((int)ResponseCodes.BadRequest, "Spool meter credentials is invalid");

                // Parse new amount
                if (!double.TryParse(remainingAmountInfo.NewAmount, out double newAmount))
                    return new SpoolMeterEndPointResponse((int)ResponseCodes.BadRequest, "The new amount has to be a valid double.");

                if (newAmount < 0)
                    return new SpoolMeterEndPointResponse((int)ResponseCodes.BadRequest, "The new amount cannot be negative.");


                // Try to update the entry
                var (Code, Message) = DatabaseManagement.UpdateSpoolMeter(remainingAmountInfo.meterId, newAmount);
                // Log change
                _ = DatabaseManagement.LogSpoolMeterUpdate(remainingAmountInfo.meterId);


                double newRemainingAmount = newAmount / meter.OriginalAmount;

                // Send notifications
                if (0.09 < newRemainingAmount && newRemainingAmount < 0.11)
                    Utilities.SendNotification(remainingAmountInfo.meterId, Utilities.NotificationType.MaterialLow);
                else if (newRemainingAmount < 0.01)
                    Utilities.SendNotification(remainingAmountInfo.meterId, Utilities.NotificationType.MaterialRanOut);

                // Respond back to spool meter
                return new SpoolMeterEndPointResponse((int)Code, Message);
            });



            // Endpoint to handle updating the remaining amount of the specified spool meter id in the database
            app.MapPut("Api/UpdateBatteryLevel", (UpdatedBatteryInfo remainingAmountInfo) =>
            {
                // Bad request if data isnt populated
                if (remainingAmountInfo.meterId is null || remainingAmountInfo.meterId.Length == 0)
                    return new SpoolMeterEndPointResponse((int)ResponseCodes.BadRequest, "Must supply a spool meter id!");
                if (remainingAmountInfo.password is null || remainingAmountInfo.password.Length == 0)
                    return new SpoolMeterEndPointResponse((int)ResponseCodes.BadRequest, "Must supply a password!");
                if (remainingAmountInfo.NewBatteryLevel is null || remainingAmountInfo.NewBatteryLevel.Length == 0)
                    return new SpoolMeterEndPointResponse((int)ResponseCodes.BadRequest, "Must supply the new battery level!");

                if (DatabaseManagement.GetSpoolMeterInfo(remainingAmountInfo.meterId) is not SpoolMeter meter ||
                    !DatabaseManagement.VerifyPassword(remainingAmountInfo.password, meter.Password))
                    return new SpoolMeterEndPointResponse((int)ResponseCodes.BadRequest, "Spool meter credentials is invalid");

                // Parse new battery level
                if (!Enum.TryParse(typeof(BatteryStatus),  remainingAmountInfo.NewBatteryLevel, out object? newBatteryLevel) || newBatteryLevel is not BatteryStatus newBatteryStatus)
                    return new SpoolMeterEndPointResponse((int)ResponseCodes.BadRequest, "The new battery status is invalid!");

                // Try to update the entry
                var (Code, Message) = DatabaseManagement.UpdateSpoolMeter(remainingAmountInfo.meterId, newBatteryStatus);

                // Send notification
                if (newBatteryStatus == BatteryStatus.Low)
                    Utilities.SendNotification(remainingAmountInfo.meterId, Utilities.NotificationType.BatteryLow);
                else if (newBatteryStatus == BatteryStatus.Dead)
                    Utilities.SendNotification(remainingAmountInfo.meterId, Utilities.NotificationType.BatteryDead);

                // Respond back to spool meter
                return new SpoolMeterEndPointResponse((int)Code, Message);
            });
        }
    }
}
