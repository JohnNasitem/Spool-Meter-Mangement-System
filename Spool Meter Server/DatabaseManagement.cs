//***********************************************************************************
//Program: DatabaseManagement.cs
//Description: Database access methods
//Date: Mar 17, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************



using Spool_Meter_Server.Models;
using Microsoft.EntityFrameworkCore;
using static Spool_Meter_Server.Program;
using System.Security.Principal;
using System.Drawing;
using System.Diagnostics.Metrics;
using Newtonsoft.Json.Linq;

namespace Spool_Meter_Server
{
    public static class DatabaseManagement
    {
        #region MobileApp
        /// <summary>
        /// Adds a new entry int othe Account table
        /// </summary>
        /// <param name="email">account email</param>
        /// <param name="password">account password</param>
        /// <returns>(response code, message, new user id)</returns>
        public static (ResponseCodes Code, string Message, string UserID) AddAccount(string email, string password)
        {
            using var db = new SpoolMetersManagementSystemContext();
            using var transaction = db.Database.BeginTransaction();
            try
            {
                // Create new account entry
                Account newAccount = new()
                {
                    Id = GenerateUniqueID(),
                    Email = email,
                    Password = HashPassword(password)
                };

                // Add entry to the table and save changes
                db.Accounts.Add(newAccount);
                db.SaveChanges();

                // Initialize notification settings
                newAccount.NotificationSetting = new()
                {
                    AccountId = newAccount.Id,
                    SpoolMeterBatteryLow = true,
                    SpoolMeterDied = true,
                    MaterialLow = true,
                    MaterialRanOut = true,
                };
                db.SaveChanges();
                transaction.Commit();

                return (ResponseCodes.Success, "Account successfully created!", newAccount.Id);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"AddAccount() - Failed to add a new account entry to the database. Error: {ex}");
                return (ResponseCodes.ServerError, "Problem occured with accessing the database", "");
            }
        }



        /// <summary>
        /// Log in the user using the specified username and password
        /// </summary>
        /// <param name="email">account email</param>
        /// <param name="password">account password</param>
        /// <returns>(response code, message, user id for those credentials)</returns>
        public static (ResponseCodes Code, string Message, string UserID) VerifyUserCredentials(string email, string password)
        {
            try
            {
                using var db = new SpoolMetersManagementSystemContext();
                List<Account> accountsWithUsername = [.. db.Accounts.Where(a => a.Email == email)];

                foreach (Account account in accountsWithUsername)
                    if (VerifyPassword(password, account.Password))
                        return (ResponseCodes.Success, "Login successful.", account.Id);

                return (ResponseCodes.BadRequest, "Email or password is incorrect.", "");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"VerifyUserCredentials() - Failed to verify user log in credentials to the database. Error: {ex}");
                return (ResponseCodes.ServerError, "Problem occured with accessing the database", "");
            }
        }



        /// <summary>
        /// Get all the spool meters associated with the specified user id
        /// </summary>
        /// <param name="userID">user id of the account</param>
        /// <returns>(response code, message, list of spool meters or null if there was a problem with accessing database)</returns>
        public static (ResponseCodes Code, string Message, List<SpoolMeter>? SpoolMeters) GetUserMeters(string userID)
        {
            try
            {
                using var db = new SpoolMetersManagementSystemContext();
                var account = db.Accounts.Include(a => a.SpoolMeters).FirstOrDefault(a => a.Id == userID);

                if (account is null)
                    return (ResponseCodes.BadRequest, "User ID is invalid!", null);

                var spoolMeters = db.Accounts
                        .Where(a => a.Id == userID)
                        .SelectMany(a => a.SpoolMeters)
                        .ToList();

                spoolMeters.ForEach(s => s.Accounts = []);

                // Return all the spool meters connected to the specified account 
                return (ResponseCodes.Success, "Got user's spool meters!", spoolMeters);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetUserMeters() - Failed to get spool meters from the database. Error: {ex}");
                return (ResponseCodes.ServerError, "Problem occured with accessing the database", null);
            }
        }



        /// <summary>
        /// Get all the material types in the database
        /// </summary>
        /// <returns>(response code, message, list of spool meters or null if there was a problem with accessing database)</returns>
        public static (ResponseCodes Code, string Message, List<MaterialType>? MaterialTypes) GetMaterialTypes()
        {
            try
            {
                using var db = new SpoolMetersManagementSystemContext();
                // Return all the material types
                return (ResponseCodes.Success, "Got all material types!", db.MaterialTypes.ToList());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetMaterialTypes() - Failed to get material types from the database. Error: {ex}");
                return (ResponseCodes.ServerError, "Problem occured with accessing the database", null);
            }
        }



        /// <summary>
        /// Adds a new entry int othe Spool Meter table
        /// </summary>
        /// <param name="name">spool meter name</param>
        /// <param name="remainingAmount">remaining amount</param>
        /// <param name="originalAmount">original amount</param>
        /// <param name="batteryStatus">battery status</param>
        /// <param name="materialTypeId">material type id</param>
        /// <param name="colour">spool meter color</param>
        /// <returns>(response code, message, new spool meter password)</returns>
        public static (ResponseCodes Code, string Message, string SpoolMeterPassword) AddSpoolMeter(string id, string name, double remainingAmount, double originalAmount, BatteryStatus batteryStatus, int materialTypeId, Color colour)
        {
            try
            {
                using var db = new SpoolMetersManagementSystemContext();
                // Generate new password for the spool meter
                string newPassword = Utilities.GenerateRandomString(50);

                // Create new account entry
                SpoolMeter newSpoolMeter = new()
                {
                    Id = id,
                    Name = name,
                    Password = HashPassword(newPassword),
                    RemainingAmount = remainingAmount,
                    OriginalAmount = originalAmount,
                    BatteryStatus = (byte)batteryStatus,
                    MaterialTypeId = materialTypeId,
                    Color = $"#{colour.R:X2}{colour.G:X2}{colour.B:X2}",
                };

                // Add entry to the table and save changes
                db.SpoolMeters.Add(newSpoolMeter);
                db.SaveChanges();

                return (ResponseCodes.Success, "Account successfully created!", newPassword);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AddSpoolMeter() - Failed to add a new spool meter entry to the database. Error: {ex}");
                return (ResponseCodes.ServerError, "Problem occured with accessing the database", "");
            }
        }



        /// <summary>
        /// Add the specified spool meter to the specified account
        /// </summary>
        /// <param name="accountId">account id to bind spool meter to</param>
        /// <param name="spoolMeterId">spool meter id that is being binded</param>
        /// <returns>(response code, message)</returns>
        public static (ResponseCodes Code, string Message) BindSpoolMeterToAccount(string accountId, string spoolMeterId)
        {
            if (!DoesSpoolMeterIDExist(spoolMeterId))
                throw new ArgumentException("Spool meter id doesn't exist in the database!");
            if (!DoesAccountIDExist(accountId))
                throw new ArgumentException("Account id doesn't exist in the database");

            try
            {
                using var db = new SpoolMetersManagementSystemContext();
                // Link spool meter to account
                db.Accounts.First(a => a.Id == accountId).SpoolMeters
                    .Add(db.SpoolMeters.First(s => s.Id == spoolMeterId));
                db.SaveChanges();

                return (ResponseCodes.Success, "Account and spool meter successfully binded!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"BindSpoolMeterToAccount() - Failed to add an entry in the AccountToSpoolMeter table in the database. Error: {ex}");
                return (ResponseCodes.ServerError, "Problem occured with accessing the database");
            }
        }



        /// <summary>
        /// Create a spool meter and bind it to an account
        /// </summary>
        /// <param name="accountId">account id to bind the spool meter to</param>
        /// <param name="name">spool meter name</param>
        /// <param name="remainingAmount">remaining amount</param>
        /// <param name="originalAmount">original amount</param>
        /// <param name="batteryStatus">battery status</param>
        /// <param name="materialTypeId">material type id</param>
        /// <param name="colour">spool meter color</param>
        /// <returns>(response code, message, new spool meter password)</returns>
        public static (ResponseCodes Code, string Message, string SpoolMeterPassword) AddAndBindSpoolMeter(string accountId, string spoolMeterId, string name, double remainingAmount, double originalAmount, BatteryStatus batteryStatus, int materialTypeId, Color colour)
        {
            // Start transaction
            using var transaction = new SpoolMetersManagementSystemContext().Database.BeginTransaction();
            SpoolMeter? sm = GetSpoolMeterInfo(spoolMeterId);
            ResponseCodes addResultCode = ResponseCodes.Success;
            string password = "";
            string addResultMessage = "";

            // Only add spool meter if it doesnt already exist in the database
            if (sm == null)
                (addResultCode, addResultMessage, password) = AddSpoolMeter(spoolMeterId, name, remainingAmount, originalAmount, (BatteryStatus)batteryStatus, materialTypeId, colour);
            else
                (addResultCode, addResultMessage) = UpdateSpoolMeter(spoolMeterId, name, remainingAmount, originalAmount, materialTypeId, colour);
            var (Code, Message) = BindSpoolMeterToAccount(accountId, spoolMeterId);

            // If both operations are a success then commit
            if (addResultCode == ResponseCodes.Success && Code == ResponseCodes.Success)
            {
                transaction.Commit();
                return (addResultCode, addResultMessage, password);
            }

            // If one of the operations failed then rollback
            transaction.Rollback();
            if (addResultCode != ResponseCodes.Success)
                return (addResultCode, addResultMessage, password);

            return (Code, Message, "");
        }




        /// <summary>
        /// Update an existing spool meter in the database
        /// </summary>
        /// <param name="id">existing spool meter id</param>
        /// <param name="newName">new spool meter name</param>
        /// <param name="newRemainingAmount">new remaining amount</param>
        /// <param name="newOriginalAmount">new original amount</param>
        /// <param name="newMaterialTypeId">new material type id</param>
        /// <param name="colour">new color</param>
        /// <returns>(response code, message)</returns>
        public static (ResponseCodes Code, string Message) UpdateSpoolMeter(string id, string newName, double newRemainingAmount, double newOriginalAmount, int newMaterialTypeId, Color colour)
        {
            try
            {
                using var db = new SpoolMetersManagementSystemContext();

                SpoolMeter? spoolMeter = db.SpoolMeters.Find(id);

                if (spoolMeter == null)
                    return (ResponseCodes.BadRequest, "Spool Meter with specified id does not exist!");

                // Edit spool meter
                spoolMeter.Name = newName;
                spoolMeter.RemainingAmount = newRemainingAmount;
                spoolMeter.OriginalAmount = newOriginalAmount;
                spoolMeter.MaterialTypeId = newMaterialTypeId;
                spoolMeter.Color = $"#{colour.R:X2}{colour.G:X2}{colour.B:X2}";

                // save changes
                db.SaveChanges();

                return (ResponseCodes.Success, "Spool meter successfully updated!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateSpoolMeter() - Failed to update a spool meter entry to the database. Error: {ex}");
                return (ResponseCodes.ServerError, "Problem occured with accessing the database");
            }
        }



        /// <summary>
        /// Update an accounts password
        /// </summary>
        /// <param name="accountId">id of account to update</param>
        /// <param name="newPassword">new password</param>
        /// <returns>(response code, message)</returns>
        public static (ResponseCodes Code, string Message) UpdateAccountPassword(string accountId, string newPassword)
        {
            try
            {
                using var db = new SpoolMetersManagementSystemContext();

                // Find account
                if (db.Accounts.Find(accountId) is not Account account)
                    return (ResponseCodes.BadRequest, "Account with specified id does not exist!");

                // Update account password
                account.Password = HashPassword(newPassword);

                // Save changes
                db.SaveChanges();

                return (ResponseCodes.Success, "Account password successfully updated!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateAccountPassword() - Failed to update a account entry to the database. Error: {ex}");
                return (ResponseCodes.ServerError, "Problem occured with accessing the database");
            }
        }



        /// <summary>
        /// Update an accounts password
        /// </summary>
        /// <param name="accountId">id of account to update</param>
        /// <param name="newEmail">new email</param>
        /// <returns>(response code, message)</returns>
        public static (ResponseCodes Code, string Message) UpdateAccountEmail(string accountId, string newEmail)
        {
            try
            {
                using var db = new SpoolMetersManagementSystemContext();

                // Find account
                if (db.Accounts.Find(accountId) is not Account account)
                    return (ResponseCodes.BadRequest, "Account with specified id does not exist!");

                // Update account email
                account.Email = newEmail;

                // Save changes
                db.SaveChanges();

                return (ResponseCodes.Success, "Account email successfully updated!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateAccountEmail() - Failed to update a account entry to the database. Error: {ex}");
                return (ResponseCodes.ServerError, "Problem occured with accessing the database");
            }
        }



        /// <summary>
        /// Deletes an entry from the account table and removes any links to spool meter
        /// </summary>
        /// <param name="accountId">id of account to delete</param>
        /// <returns>(response code, message)</returns>
        public static (ResponseCodes Code, string Message) DeleteAccount(string accountId)
        {
            using var db = new SpoolMetersManagementSystemContext();
            using var transaction = new SpoolMetersManagementSystemContext().Database.BeginTransaction();

            try
            {
                // find account
                if (db.Accounts.Include(a => a.SpoolMeters).FirstOrDefault(a => a.Id == accountId) is not Account accountToDelete)
                    return (ResponseCodes.BadRequest, "Account with specified id does not exist!");

                // Delete fcm tokens
                db.AccountToFcmtokens.RemoveRange(db.AccountToFcmtokens.Where(t => t.AccountId == accountId));
                accountToDelete.AccountToFcmtokens = [];
                db.SaveChanges();

                // Delete notification settings
                db.NotificationSettings.Remove(db.NotificationSettings.Find(accountId)!);
                accountToDelete.NotificationSetting = null;
                db.SaveChanges();

                // Delete the account
                accountToDelete.SpoolMeters.Clear();
                db.SaveChanges();
                db.Accounts.Remove(accountToDelete);
                db.SaveChanges();

                transaction.Commit();
                return (ResponseCodes.Success, "Account successfully deleted!");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"DeleteAccount() - Failed to delete an account entry from the database. Error: {ex}");
                return (ResponseCodes.ServerError, "Problem occured with accessing the database");
            }
        }



        /// <summary>
        /// Update account notification settings
        /// </summary>
        /// <param name="accountId">id of account to update notifcation settings for</param>
        /// <param name="spoolMeterBatteryLow">should notify when spool meter battery is low</param>
        /// <param name="spoolMeterDied">should notify when spool meter ran out of battery</param>
        /// <param name="materialLow">should notify when material is low</param>
        /// <param name="materialRanOut">should notify when material ran out</param>
        /// <returns>(response code, message)</returns>
        public static (ResponseCodes Code, string Message) UpdateNotificationSettings(string accountId, bool spoolMeterBatteryLow, bool spoolMeterDied, bool materialLow, bool materialRanOut)
        {
            try
            {
                using var db = new SpoolMetersManagementSystemContext();
                // find account
                if (db.NotificationSettings.Find(accountId) is not NotificationSetting notifSettings)
                    return (ResponseCodes.BadRequest, "Account with specified id does not exist!");

                // Update Notification settings
                notifSettings.SpoolMeterBatteryLow = spoolMeterBatteryLow;
                notifSettings.SpoolMeterDied = spoolMeterDied;
                notifSettings.MaterialLow = materialLow;
                notifSettings.MaterialRanOut = materialRanOut;
                db.SaveChanges();

                return (ResponseCodes.Success, "Settings successfully updated!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateNotificationSettings() - Failed to update a notification settings entry to the database. Error: {ex}");
                return (ResponseCodes.ServerError, "Problem occured with accessing the database");
            }
        }



        /// <summary>
        /// Get an account's notification settings
        /// </summary>
        /// <param name="accountId">id of account to get notification settings from</param>
        /// <returns>(response code, message, notification settings)</returns>
        public static (ResponseCodes Code, string Message, NotificationSetting? Settings) GetAccountNotificationSettings(string accountId)
        {
            try
            {
                using var db = new SpoolMetersManagementSystemContext();
                var settings = db.NotificationSettings.Find(accountId);

                // Return account notification settings
                return settings is not null ? 
                            (ResponseCodes.Success, "Successfully got account notification settings!", settings) : 
                            (ResponseCodes.BadRequest, "Account ID is invalid!", null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetAccountNotificationSettings() - Failed to get notification settings from the database. Error: {ex}");
                return (ResponseCodes.ServerError, "Problem occured with accessing the database", null);
            }
        }



        /// <summary>
        /// Unbinds a spool meter from the specified account
        /// </summary>
        /// <param name="accountId">id of account to unbind from</param>
        /// <param name="spoolMeterId">id of spool meter to unbind</param>
        /// <returns>(response code, message)</returns>
        public static (ResponseCodes Code, string Message) UnbindSpoolMeterFromAccount(string accountId, string spoolMeterId)
        {
            try
            {
                using var db = new SpoolMetersManagementSystemContext();

                // find account
                if (db.Accounts.Include(a => a.SpoolMeters).FirstOrDefault(a => a.Id == accountId) is not Account accountToUnbindFrom)
                    return (ResponseCodes.BadRequest, "Account with specified id does not exist!");

                // Check if a spool meter with specified id exists
                var bindedSpoolMeter = accountToUnbindFrom.SpoolMeters.FirstOrDefault(s => s.Id == spoolMeterId);

                if (bindedSpoolMeter == null)
                    return (ResponseCodes.BadRequest, "Spool meter with specified id is not binded with specified account!");

                // Remove if it does
                accountToUnbindFrom.SpoolMeters.Remove(bindedSpoolMeter);
                db.SaveChanges();

                return (ResponseCodes.Success, "Spool meter successfully unbinded!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UnbindSpoolMeterFromAccount() - Failed to delete an AccountToSpoolMeter entry to the database. Error: {ex}");
                return (ResponseCodes.ServerError, "Problem occured with accessing the database");
            }
        }



        /// <summary>
        /// Get all the usage logs for a given spool meter
        /// </summary>
        /// <param name="spoolMeterId">id of spool meter to get logs from</param>
        /// <returns>(response code, message, usage logs)</returns>
        public static (ResponseCodes Code, string Message, List<Usage>? UsageLogs) GetSpoolMeterUsageLogs(string spoolMeterId)
        {
            try
            {
                using var db = new SpoolMetersManagementSystemContext();
                var spoolMeter = db.SpoolMeters.Include(s => s.Usages).FirstOrDefault(s => s.Id == spoolMeterId);

                if (spoolMeter is null)
                    return (ResponseCodes.BadRequest, "Spool Meter ID is invalid!", null);

                // Return all the spool meters connected to the specified account 
                return (ResponseCodes.Success, "Got spool meter's usage logs!", spoolMeter.Usages.ToList());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetUserMeters() - Failed to get spool meters from the database. Error: {ex}");
                return (ResponseCodes.ServerError, "Problem occured with accessing the database", null);
            }
        }



        /// <summary>
        /// Add a new fcm token entry to the database
        /// </summary>
        /// <param name="accountId">if of account to add token to</param>
        /// <param name="Token">fcm token to add</param>
        /// <returns>(response code, message)</returns>
        public static (ResponseCodes Code, string Message) AddFCMToken(string accountId, string token)
        {
            try
            {
                using var db = new SpoolMetersManagementSystemContext();

                if (db.AccountToFcmtokens.Find(token) is not null)
                    return (ResponseCodes.BadRequest, "FCM token is already in the database!");

                // Create new fcm token entry
                AccountToFcmtoken newTokenEntry = new()
                {
                    AccountId = accountId,
                    Fcmtoken = token
                };

                // Add entry to the table and save changes
                db.AccountToFcmtokens.Add(newTokenEntry);
                db.SaveChanges();

                return (ResponseCodes.Success, "FCM token successfully created!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AddFCMToken() - Failed to add a new fcm token entry to the database. Error: {ex}");
                return (ResponseCodes.ServerError, "Problem occured with accessing the database");
            }
        }



        /// <summary>
        /// Get all the fcm tokens related to an account
        /// </summary>
        /// <param name="accountId">id of account to get tokens for</param>
        /// <returns>(response code, message, fcm tokens)</returns>
        public static (ResponseCodes Code, string Message, List<AccountToFcmtoken>?) GetFCMTokens(string accountId)
        {
            try
            {
                using var db = new SpoolMetersManagementSystemContext();
                var account = db.Accounts.Include(a => a.AccountToFcmtokens).FirstOrDefault(a => a.Id == accountId);

                if (account is null)
                    return (ResponseCodes.BadRequest, "Account ID is invalid!", null);

                // Return all the tokens related to the specified account 
                return (ResponseCodes.Success, "Got account's fcm tokens!", account.AccountToFcmtokens.ToList());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetFCMTokens() - Failed to the account's fcm tokens from the database. Error: {ex}");
                return (ResponseCodes.ServerError, "Problem occured with accessing the database", null);
            }
        }



        /// <summary>
        /// Deletes an entry from the account to fcm token table
        /// </summary>
        /// <param name="fcmToken">fcm token to remove</param>
        /// <returns>(response code, message)</returns>
        public static (ResponseCodes Code, string Message) RemoveFCMToken(string fcmToken)
        {
            using var db = new SpoolMetersManagementSystemContext();

            try
            {
                // find account
                if (db.AccountToFcmtokens.Find(fcmToken) is not AccountToFcmtoken fcmTokenToDelete)
                    return (ResponseCodes.BadRequest, "Entry with the specified fcm token does not exist!");

                // Delete the account
                db.AccountToFcmtokens.Remove(fcmTokenToDelete);
                db.SaveChanges();

                return (ResponseCodes.Success, "FCM token successfully deleted");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RemoveFCMToken() - Failed to delete an fcm token from the database. Error: {ex}");
                return (ResponseCodes.ServerError, "Problem occured with accessing the database");
            }
        }
        #endregion



        #region SpoolMeter
        /// <summary>
        /// Log in the spool meter using the specified spool meter id and password
        /// </summary>
        /// <param name="meterId">spool meter id</param>
        /// <param name="password">account password</param>
        /// <returns>(response code, message, user id for those credentials)</returns>
        public static (ResponseCodes Code, string Message, string MeterID) VerifySpoolMeterCredentials(string meterId, string password)
        {
            try
            {
                using var db = new SpoolMetersManagementSystemContext();
                List<SpoolMeter> spoolMetersWithID = [.. db.SpoolMeters.Where(a => a.Id == meterId)];

                foreach (SpoolMeter spoolMeter in spoolMetersWithID)
                    if (VerifyPassword(password, spoolMeter.Password))
                        return (ResponseCodes.Success, "Login successful.", spoolMeter.Id);

                return (ResponseCodes.BadRequest, "Spool meter id or password is incorrect.", "");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to verify spool meter log in credentials to the database. Error: {ex}");
                return (ResponseCodes.ServerError, "Problem occured with accessing the database", "");
            }
        }



        /// <summary>
        /// Update a spool meter entry in the database with a new remaining amount
        /// </summary>
        /// <param name="meterID">Spool meter id</param>
        /// <param name="newAmount">new remaining amount</param>
        /// <returns>(response code, message)</returns>
        public static (ResponseCodes Code, string Message) UpdateSpoolMeter(string meterID, double newAmount)
        {
            try
            {
                using var db = new SpoolMetersManagementSystemContext();
                if (db.SpoolMeters.Find(meterID) is SpoolMeter sm)
                {
                    sm.RemainingAmount = newAmount;
                    db.SpoolMeters.Update(sm);
                    db.SaveChanges();

                    return (ResponseCodes.Success, "Successfully updated spool meter.");
                }
                else
                    return (ResponseCodes.BadRequest, "Spool meter with the specified id was not found!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to update the spool meter with these data. ID: {meterID}, New Amount: {newAmount}. Error: {ex}");
                return (ResponseCodes.ServerError, "Failed to update the spool meter in the database.");
            }
        }



        /// <summary>
        /// Update a spool meter entry in the database with a new battery status
        /// </summary>
        /// <param name="meterID">Spool meter id</param>
        /// <param name="newBatteryStatus">new battery status</param>
        /// <returns>(response code, message)</returns>
        public static (ResponseCodes Code, string Message) UpdateSpoolMeter(string meterID, BatteryStatus newBatteryStatus)
        {
            try
            {
                using var db = new SpoolMetersManagementSystemContext();
                if (db.SpoolMeters.Find(meterID) is SpoolMeter sm)
                {
                    sm.BatteryStatus = (byte)newBatteryStatus;
                    db.SpoolMeters.Update(sm);
                    db.SaveChanges();

                    return (ResponseCodes.Success, "Successfully updated spool meter.");
                }
                else
                    return (ResponseCodes.BadRequest, "Spool meter with the specified id was not found!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to update the spool meter with these data. ID: {meterID}, New Battery Level: {(byte)newBatteryStatus} ({Enum.GetName(typeof(BatteryStatus), newBatteryStatus)}). Error: {ex}");
                return (ResponseCodes.ServerError, "Failed to update the spool meter in the database.");
            }
        }



        /// <summary>
        /// Log the new update
        /// </summary>
        /// <param name="meterId">Spool meter id</param>
        /// <returns>(response code, message)</returns>
        public static (ResponseCodes Code, string Message) LogSpoolMeterUpdate(string meterId)
        {
            try
            {
                using var db = new SpoolMetersManagementSystemContext();
                if (db.SpoolMeters.Find(meterId) is SpoolMeter sm)
                {
                    sm.Usages.Add(new Usage()
                    {
                        Time = DateTime.Now,
                        RemainingAmountPercentage = sm.RemainingAmount / sm.OriginalAmount,
                    });
                    db.SaveChanges();

                    return (ResponseCodes.Success, "Successfully added usage log for spool meter");
                }
                else
                    return (ResponseCodes.BadRequest, "Spool meter with the specified id was not found!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to add a new log for the specified spool meter. METER ID: {meterId}. Error: {ex}");
                return (ResponseCodes.ServerError, "Failed to update the spool meter in the database.");
            }
        }
        #endregion



        #region HelperMethods
        /// <summary>
        /// Hashes a password
        /// </summary>
        /// <param name="password">Password to hash</param>
        /// <returns>Hashed password string</returns>
        private static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }



        /// <summary>
        /// Generate a new unique user id
        /// </summary>
        /// <returns>Program.USER_ID_LENGTH long unique user id</returns>
        private static string GenerateUniqueID()
        {
            // Check if the generated user ID is already taken in the database
            bool uniqueIDFound = false;
            // New generated user id
            string newID = string.Empty;

            using (var db = new SpoolMetersManagementSystemContext())
            {
                // Keep generating a user id until a unique one is found
                do
                {
                    // Get a random string
                    newID = Utilities.GenerateRandomString(Program.USER_ID_LENGTH);

                    // Check if the generated user ID is already taken in the database
                    var accountsWithID = db.Accounts.Count(a => a.Id == newID) + db.SpoolMeters.Count(s => s.Id == newID);

                    // Check if the generated user id is unique
                    uniqueIDFound = accountsWithID == 0;
                }
                while (!uniqueIDFound);
            }

            return newID;
        }



        /// <summary>
        /// Generate a unique token
        /// </summary>
        /// <returns>Unique token</returns>
        public static string GenerateUniqueToken()
        {
            string newToken = "";

            using (var db = new SpoolMetersManagementSystemContext())
            {
                // Generate until a unique string is created
                do newToken = Utilities.GenerateRandomString(128);
                while (db.Tokens.Any(t => t.TokenValue == newToken));
            }

            return newToken;
        }



        /// <summary>
        /// Creates new access and refresh token and saves it to the database
        /// </summary>
        /// <param name="id">ID token is for</param>
        /// <returns>(access token, refresh token)</returns>
        public static (string AccessToken, string RefreshToken) CreateAndSaveTokens(string id)
        {
            string newAccessTokenValue = GenerateUniqueToken();
            string newRefreshTokenValue = GenerateUniqueToken();
         
            using (var db = new SpoolMetersManagementSystemContext())
            {
                // Generate access token
                Token newAccessToken = new()
                {
                    TokenValue = newAccessTokenValue,
                    TokenType = TokenType.AccessToken.ToString(),
                    Id = id,
                    CreationTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds(),
                };
                db.Tokens.Add(newAccessToken);

                // Generate refresh token
                Token newRefressToken = new()
                {
                    TokenValue = newRefreshTokenValue,
                    TokenType = TokenType.RefreshToken.ToString(),
                    Id = id,
                    CreationTimestamp = newAccessToken.CreationTimestamp,
                };
                db.Tokens.Add(newRefressToken);

                // Save changes to the database
                db.SaveChanges();
            }

            return (newAccessTokenValue, newRefreshTokenValue);
        }



        /// <summary>
        /// Get token data from the token <br/>
        /// Also remove it if it is expired
        /// </summary>
        /// <param name="tokenValue">token used to get data</param>
        /// <returns>Data related to that token</returns>
        public static Token? GetTokenData(string tokenValue)
        {
            using var db = new SpoolMetersManagementSystemContext();
            Token? token = db.Tokens.FirstOrDefault(t => t.TokenValue == tokenValue);

            // Return null if no token was found
            if (token == null)
                return null;

            // Check how long the token is supposed to live for
            double ttl = token.TokenType == TokenType.AccessToken.ToString() ? Program.AccessTokenTTL.TotalSeconds : Program.RefreshTokenTTL.TotalSeconds;

            // Remove expired access tokens
            if (token.CreationTimestamp - DateTimeOffset.Now.ToUnixTimeSeconds() > ttl)
            {
                db.Tokens.Remove(token);
                db.SaveChanges();
                return null;
            }

            return token;
        }



        /// <summary>
        /// Get spool meter info
        /// </summary>
        /// <param name="id">id of spool meter to get</param>
        /// <returns>Spool meter, null if the id is invalid</returns>
        public static SpoolMeter? GetSpoolMeterInfo(string id)
        {
            using var db = new SpoolMetersManagementSystemContext();
            return db.SpoolMeters.Include(s => s.Accounts).FirstOrDefault(s => s.Id == id); ;
        }



        /// <summary>
        /// Deletes a token from the database
        /// </summary>
        /// <param name="tokenValue">token to delete</param>
        public static void DeleteToken(string tokenValue)
        {
            using var db = new SpoolMetersManagementSystemContext();
            Token? token = db.Tokens.FirstOrDefault(t => t.TokenValue == tokenValue);

            if (token == null)
                return;

            db.Tokens.Remove(token);
            db.SaveChanges();
        }



        /// <summary>
        /// Delete tokens related to the specified account
        /// </summary>
        /// <param name="accountID">account id to remove tokens from</param>
        /// <param name="tokenType">token type to remove</param>
        public static void DeleteAccountTokens(string accountID, TokenType tokenType)
        {
            using var db = new SpoolMetersManagementSystemContext();
            var tokensToDelete = db.Tokens.Where(t => t.Id == accountID
                                                   && t.TokenType == Enum.GetName(typeof(TokenType), tokenType));

            foreach (var token in tokensToDelete)
                db.Tokens.Remove(token);

            db.SaveChanges();
        }



        /// <summary>
        /// Deletes any tokens related to the specified account and creates new ones
        /// </summary>
        /// <param name="accountID">id of account that needs tokens replaced</param>
        /// /// <returns>(access token, refresh token)</returns>
        public static (string AccessToken, string RefreshToken) ReplaceAccountTokens(string accountID)
        {
            DeleteAccountTokens(accountID, TokenType.AccessToken);
            DeleteAccountTokens(accountID, TokenType.RefreshToken);
            return CreateAndSaveTokens(accountID);
        }



        /// <summary>
        /// Search for an account from the account id
        /// </summary>
        /// <param name="accountID"></param>
        /// <returns>Account if account id is valid, otherwise null</returns>
        public static Account? GetAccountInfo(string accountID)
        {
            using var db = new SpoolMetersManagementSystemContext();
            return db.Accounts.Include(a => a.AccountToFcmtokens).FirstOrDefault(a => a.Id == accountID);
        }



        /// <summary>
        /// Removes all the expired usage logs
        /// </summary>
        /// <returns>amount of logs remvoed, -1 if an error occured</returns>
        public static int RemoveAllExpiredUsageLogs()
        {
            using var db = new SpoolMetersManagementSystemContext();
            int logsRemoved = 0;

            try
            {
                foreach (Usage log in db.Usages)
                {
                    // Remove expired access tokens
                    if (log.Time - DateTimeOffset.Now > UsageLogTTL)
                    {
                        logsRemoved++;
                        db.Usages.Remove(log);
                    }
                }

                db.SaveChanges();
                return logsRemoved;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RemoveAllExpiredUsageLogs() - Problem occured when trying to delete all the expired usage logs. Exception: {ex}");
                return -1;
            }
        }
        #endregion



        #region ValidationMethods
        /// <summary>
        /// Verifies if a password is correct
        /// </summary>
        /// <param name="password">Password to verify</param>
        /// <param name="hashedPassword">Hashed password in the database</param>
        /// <returns>true if the specified password is correct, otherwise false</returns>
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }



        /// <summary>
        /// Check if a material type with the specified id exists
        /// </summary>
        /// <param name="materialTypeID">material type id to check</param>
        /// <returns>true if it exists, otherwise false</returns>
        public static bool DoesMaterialTypeIDExist(int materialTypeID)
        {
            using var db = new SpoolMetersManagementSystemContext();
            return db.MaterialTypes.Any(a => a.Id == materialTypeID);
        }



        /// <summary>
        /// Does email exist in the database
        /// </summary>
        /// <param name="email">email to check</param>
        /// <returns>true if the email already exists in the database</returns>
        public static bool DoesEmailAlreadyExist(string email)
        {
            using var db = new SpoolMetersManagementSystemContext();
            return db.Accounts.Any(a => a.Email == email);
        }



        /// <summary>
        /// Does token exist in the database
        /// </summary>
        /// <param name="token">token to check</param>
        /// <returns>true if the token exists, false otherwise</returns>
        public static bool DoesTokenExist(string token)
        {
            using var db = new SpoolMetersManagementSystemContext();
            return db.Tokens.Any(a => a.TokenValue == token);
        }



        /// <summary>
        /// Does spool meter with the specified id exists
        /// </summary>
        /// <param name="spoolMeterID">id to check</param>
        /// <returns>true if a spool meter with the id exists in the database, otherwise false</returns>
        public static bool DoesSpoolMeterIDExist(string spoolMeterID)
        {
            using var db = new SpoolMetersManagementSystemContext();
            return db.SpoolMeters.Any(a => a.Id == spoolMeterID);
        }



        /// <summary>
        /// Does account with the specified id exists
        /// </summary>
        /// <param name="accountID">id to check</param>
        /// <returns>true if a account with the id exists in the database, otherwise false</returns>
        public static bool DoesAccountIDExist(string accountID)
        {
            using var db = new SpoolMetersManagementSystemContext();
            return db.Accounts.Any(a => a.Id == accountID);
        }
        #endregion
    }



    public enum TokenType
    {
        AccessToken,
        RefreshToken,
    }
}
