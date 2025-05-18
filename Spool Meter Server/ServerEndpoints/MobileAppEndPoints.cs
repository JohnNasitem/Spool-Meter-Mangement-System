//***********************************************************************************
//Program: MobileAppEndPoints.cs
//Description: Sets up the end points the mobile app uses
//Date: Mar 17, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************



using Azure.Core;
using Spool_Meter_Server.Controllers;
using Spool_Meter_Server.Models;
using System.Drawing;
using System.Security.Cryptography.Xml;
using System.Text.RegularExpressions;
using static Spool_Meter_Server.Program;

namespace Spool_Meter_Server.ServerEndpoints
{
    //Records
    public record AccountInfo(string Email, string Password, string FcmToken);
    public record UserTokens(string RefreshToken, string AccessToken);
    public record AddSpoolMeter(string AccessToken, string SpoolMeterId, string Name, string RemainingAmount, string OriginalAmount, string MaterialId, string ColorHex, string BatteryStatus);
    public record UpdateSpoolMeter(string AccessToken, string SpoolMeterId, string Name, string RemainingAmount, string OriginalAmount, string MaterialId, string ColorHex, string BatteryStatus);
    public record AddMaterialType(string Name, string Diameter, string MeasurementType);
    public record UpdateAccountEmail(string AccessToken, string NewEmail);
    public record UpdateAccountPassword(string AccessToken, string OldPassword, string NewPassword);
    public record UpdateAccountNotificationSettings(string AccessToken, bool SpoolMeterBatteryLow, bool SpoolMeterDied, bool MaterialLow, bool MaterialRanOut);



    /// <summary>
    /// Static class used to set end points for the mobile app
    /// </summary>
    public static class MobileAppEndPoints
    {
        private static MessageController _messageController = new();

        /// <summary>
        /// Set up end points for the specified <see cref=WebApplication/>
        /// </summary>
        /// <param name="app">Web application that are end points are being set up in</param>
        public static void SetEndpoints(WebApplication app)
        {
            // Endpoint to add a new account into the database
            app.MapPost("/Api/SignUp", (AccountInfo signupInfo) =>
            {
                Console.WriteLine($"Creating account with these credentials email: {signupInfo.Email}, Password: {signupInfo.Password}");

                // Validating input data
                if (signupInfo.Email == null || signupInfo.Password == null || signupInfo.Email.Length == 0 || signupInfo.Password.Length == 0)
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "All fields must be filled out!"
                    });
                if (!Regex.Match(signupInfo.Email, EmailRegex).Success)
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "Email is in an invalid format!"
                    });
                if (DatabaseManagement.DoesEmailAlreadyExist(signupInfo.Email))
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "Email already exists"
                    });

                // Add account to database
                var (Code, Message, UserID) = DatabaseManagement.AddAccount(signupInfo.Email, signupInfo.Password);
                _ = DatabaseManagement.AddFCMToken(UserID, signupInfo.FcmToken);

                // Create and return server response
                object responseData = new { };

                switch (Code)
                {
                    case ResponseCodes.ServerError:
                    case ResponseCodes.BadRequest:
                        responseData = new
                        {
                            Message
                        };
                        break;

                    case ResponseCodes.Success:
                        var (AccessToken, RefreshToken) = DatabaseManagement.CreateAndSaveTokens(UserID);

                        responseData = new
                        {
                            Message,
                            AccessToken,
                            RefreshToken
                        };
                        break;
                    case ResponseCodes.Unauthorized:
                        break;
                    default:
                        throw new NotImplementedException($"Code: {Code} ({(int)Code}) is not implemented");
                }

                return new MobileAppEndPointResponse((int)Code, responseData);
            });



            // Endpoint to log in
            app.MapPost("/Api/LogIn", (AccountInfo loginInfo) =>
            {
                Console.WriteLine($"Logging in with these credentials email: {loginInfo.Email}, Password: {loginInfo.Password}");

                // Validating input data
                if (loginInfo.Email == null || loginInfo.Password == null || loginInfo.Email.Length == 0 || loginInfo.Password.Length == 0)
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "All fields must be filled out!"
                    });

                // Check if user credentials are valid
                var (Code, Message, UserID) = DatabaseManagement.VerifyUserCredentials(loginInfo.Email, loginInfo.Password);
                _ = DatabaseManagement.AddFCMToken(UserID, loginInfo.FcmToken);

                // Create and return server response
                object responseData = new { };

                switch (Code)
                {
                    case ResponseCodes.ServerError:
                    case ResponseCodes.BadRequest:
                        responseData = new
                        {
                            Message
                        };
                        break;

                    case ResponseCodes.Success:
                        var (AccessToken, RefreshToken) = DatabaseManagement.ReplaceAccountTokens(UserID);

                        responseData = new
                        {
                            Message,
                            AccessToken,
                            RefreshToken
                        };
                        break;
                    case ResponseCodes.Unauthorized:
                        break;
                    default:
                        throw new NotImplementedException($"Code: {Code} ({(int)Code}) is not implemented");
                }

                return new MobileAppEndPointResponse((int)Code, responseData);
            });



            // Endpoint to refresh the access and refresh token
            app.MapPost("/Api/RefreshTokens", (UserTokens tokens1) =>
            {
                // Get token entry from database
                Token? token = DatabaseManagement.GetTokenData(tokens1.RefreshToken);

                // Check if token entry exists
                if (token == null)
                    return new MobileAppEndPointResponse((int)ResponseCodes.Unauthorized, new
                    {
                        Message = "Invalid refresh token!"
                    });

                // If it exists create new access and refresh tokens
                var (AccessToken, RefreshToken) = DatabaseManagement.ReplaceAccountTokens(token.Id);
                return new MobileAppEndPointResponse((int)ResponseCodes.Success, new
                {
                    Message = "Successfully created new access and refresh token",
                    AccessToken,
                    RefreshToken
                });
            });



            // Endpoint to get all the spool meters connected to the specified user id
            app.MapGet("Api/GetUserSpoolMeters/{accessToken?}", (string? accessToken) =>
            {
                if (accessToken == null)
                    return new MobileAppEndPointResponse((int)ResponseCodes.Unauthorized, new
                    {
                        Message = "Access token is invalid"
                    });

                // Get user id related to token
                Token? tokenData = DatabaseManagement.GetTokenData(accessToken);

                // Validate input data
                if (tokenData == null)
                {
                    return new MobileAppEndPointResponse((int)ResponseCodes.Unauthorized, new
                    {
                        Message = "Access token is invalid"
                    });
                }

                // Get user's spool meters from the database
                var (Code, Message, SpoolMeters) = DatabaseManagement.GetUserMeters(tokenData.Id);

                // Remove tokens that have an id that doesn't exist anymore
                if (Code == ResponseCodes.BadRequest)
                {
                    DatabaseManagement.DeleteAccountTokens(tokenData.Id, TokenType.AccessToken);
                    DatabaseManagement.DeleteAccountTokens(tokenData.Id, TokenType.RefreshToken);

                    return new MobileAppEndPointResponse((int)ResponseCodes.Unauthorized, new
                    {
                        Message = "Access token is invalid"
                    });
                }

                // Create and return server response
                object responseData = new { };

                switch (Code)
                {
                    case ResponseCodes.ServerError:
                    case ResponseCodes.BadRequest:
                    case ResponseCodes.Unauthorized:
                        responseData = new
                        {
                            Message
                        };
                        break;

                    case ResponseCodes.Success:
                        responseData = new
                        {
                            Message,
                            SpoolMeters
                        };
                        break;
                    default:
                        throw new NotImplementedException($"Code: {Code} ({(int)Code}) is not implemented");
                }

                return new MobileAppEndPointResponse((int)Code, responseData);
            });



            // Endpoint to add a spool meter
            app.MapPost("/Api/AddSpoolMeter", (AddSpoolMeter addSpoolMeterInfo) =>
            {
                Console.WriteLine($"Adding new spool meter from token: {addSpoolMeterInfo.AccessToken}");

                // Validate input data
                if (addSpoolMeterInfo.AccessToken == null || addSpoolMeterInfo.AccessToken.Trim() == "")
                    return new MobileAppEndPointResponse((int)ResponseCodes.Unauthorized, new
                    {
                        Message = "Access token is invalid"
                    });
                if (!double.TryParse(addSpoolMeterInfo.OriginalAmount, out double originalAmount) || originalAmount < 0)
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "Original amount must be a positive number!"
                    });
                if (!double.TryParse(addSpoolMeterInfo.RemainingAmount, out double remainingAmount) || remainingAmount < 0)
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "Remaining amount must be a positive number!"
                    });
                if (remainingAmount > originalAmount)
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "Remaining amount cannot be greater than remaining!"
                    });
                if (!Enum.TryParse(typeof(BatteryStatus), addSpoolMeterInfo.BatteryStatus, out object? batteryStatus) || batteryStatus == null)
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = $"Battery Status must be a valid int from 0 to {Enum.GetValues(typeof(BatteryStatus)).Length - 1}!"
                    });
                if (!int.TryParse(addSpoolMeterInfo.MaterialId, out int materialTypeID) || !DatabaseManagement.DoesMaterialTypeIDExist(materialTypeID))
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "Invalid material type id!"
                    });
                Color col = Color.Empty;
                try
                {
                    col = ColorTranslator.FromHtml(addSpoolMeterInfo.ColorHex);
                }
                catch
                {
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "Color must be a valid hex code in the format #XXXXXX!"
                    });
                }

                // Get user id related to token
                Token? tokenData = DatabaseManagement.GetTokenData(addSpoolMeterInfo.AccessToken.Trim());

                // Validate token
                if (tokenData == null)
                {
                    return new MobileAppEndPointResponse((int)ResponseCodes.Unauthorized, new
                    {
                        Message = "Access token is invalid"
                    });
                }

                // Create spool meter and also bind it to the user 
                var (Code, Message, SpoolMeterPassword) = DatabaseManagement.AddAndBindSpoolMeter(tokenData.Id, addSpoolMeterInfo.SpoolMeterId, addSpoolMeterInfo.Name, remainingAmount, originalAmount, (BatteryStatus)batteryStatus, materialTypeID, col);
                _ = DatabaseManagement.LogSpoolMeterUpdate(addSpoolMeterInfo.SpoolMeterId);

                // Create and return server response
                object responseData = new { };
                int resultCode = (int)Code;

                switch (Code)
                {
                    case ResponseCodes.ServerError:
                    case ResponseCodes.BadRequest:
                        responseData = new
                        {
                            Message
                        };
                        break;

                    case ResponseCodes.Success:
                        responseData = new
                        {
                            Message,
                            SpoolMeterPassword,
                        };
                        break;
                    case ResponseCodes.Unauthorized:
                        break;
                    default:
                        throw new NotImplementedException($"Code: {Code} ({(int)Code}) is not implemented");
                }

                return new MobileAppEndPointResponse((int)Code, responseData);
            });



            // Endpoint to add a spool meter
            app.MapPut("/Api/UpdateSpoolMeter", (UpdateSpoolMeter updateSpoolMeterInfo) =>
            {
                Console.WriteLine($"Updating existing spool meter from token: {updateSpoolMeterInfo.AccessToken}");

                // Validate input data
                if (updateSpoolMeterInfo.AccessToken == null || updateSpoolMeterInfo.AccessToken.Trim() == "")
                    return new MobileAppEndPointResponse((int)ResponseCodes.Unauthorized, new
                    {
                        Message = "Access token is invalid!"
                    });
                if (!DatabaseManagement.DoesSpoolMeterIDExist(updateSpoolMeterInfo.SpoolMeterId))
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "Spool meter with id doesnt exist!"
                    });
                if (!double.TryParse(updateSpoolMeterInfo.OriginalAmount, out double originalAmount) || originalAmount < 0)
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "Original amount must be a positive number!"
                    });
                if (!double.TryParse(updateSpoolMeterInfo.RemainingAmount, out double remainingAmount) || remainingAmount < 0)
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "Remaining amount must be a positive number!"
                    });
                if (remainingAmount > originalAmount)
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "Remaining amount cannot be greater than remaining!"
                    });
                if (!Enum.TryParse(typeof(BatteryStatus), updateSpoolMeterInfo.BatteryStatus, out object? batteryStatus) || batteryStatus == null)
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = $"Battery Status must be a valid int from 0 to {Enum.GetValues(typeof(BatteryStatus)).Length - 1}!"
                    });
                if (!int.TryParse(updateSpoolMeterInfo.MaterialId, out int materialTypeID) || !DatabaseManagement.DoesMaterialTypeIDExist(materialTypeID))
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "Invalid material type id!"
                    });
                Color col = Color.Empty;
                try
                {
                    col = ColorTranslator.FromHtml(updateSpoolMeterInfo.ColorHex);
                }
                catch
                {
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "Color must be a valid hex code in the format #XXXXXX!"
                    });
                }

                // Get user id related to token
                Token? tokenData = DatabaseManagement.GetTokenData(updateSpoolMeterInfo.AccessToken.Trim());

                // Validate token
                if (tokenData == null)
                {
                    return new MobileAppEndPointResponse((int)ResponseCodes.Unauthorized, new
                    {
                        Message = "Access token is invalid"
                    });
                }

                // Update spool meter entry in the database
                var (Code, Message) = DatabaseManagement.UpdateSpoolMeter(updateSpoolMeterInfo.SpoolMeterId, updateSpoolMeterInfo.Name, remainingAmount, originalAmount, materialTypeID, col);
                _ = DatabaseManagement.LogSpoolMeterUpdate(updateSpoolMeterInfo.SpoolMeterId);

                return new MobileAppEndPointResponse((int)Code, new { Message });
            });



            // Endpoint to get a spool meter
            app.MapGet("Api/GetSpoolMeter/{accessToken?}/{spoolMeterId?}", (string? accessToken, string? spoolMeterId) =>
            {
                if (accessToken == null || accessToken == "")
                    return new MobileAppEndPointResponse((int)ResponseCodes.Unauthorized, new
                    {
                        Message = "Access token is invalid"
                    });

                if (spoolMeterId == null || spoolMeterId == "")
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "Spool meter id is invalid"
                    });

                // Get user id related to token
                Token? tokenData = DatabaseManagement.GetTokenData(accessToken);

                // Validate input data
                if (tokenData == null)
                {
                    return new MobileAppEndPointResponse((int)ResponseCodes.Unauthorized, new
                    {
                        Message = "Access token is invalid"
                    });
                }

                if (DatabaseManagement.GetSpoolMeterInfo(spoolMeterId) is not SpoolMeter spoolMeter)
                    return new MobileAppEndPointResponse((int)ResponseCodes.Unauthorized, new
                    {
                        Message = "Spool meter id is invalid"
                    });

                spoolMeter.Accounts = [];

                return new MobileAppEndPointResponse((int)ResponseCodes.Unauthorized, new
                {
                    Message = "Found spool meter",
                    SpoolMeter = spoolMeter
                });
            });



            // Endpoint to get all material types
            app.MapGet("/Api/GetMaterialTypes", () =>
            {
                var (Code, Message, MaterialTypes) = DatabaseManagement.GetMaterialTypes();

                // Create and return server response
                object responseData = new { };

                switch (Code)
                {
                    case ResponseCodes.ServerError:
                    case ResponseCodes.BadRequest:
                        responseData = new
                        {
                            Message
                        };
                        break;

                    case ResponseCodes.Success:
                        responseData = new
                        {
                            Message,
                            MaterialTypes
                        };
                        break;
                    case ResponseCodes.Unauthorized:
                        break;
                    default:
                        throw new NotImplementedException($"Code: {Code} ({(int)Code}) is not implemented");
                }

                return new MobileAppEndPointResponse((int)Code, responseData);
            });



            // Endpoint to update account email
            app.MapPut("/Api/UpdateEmail", (UpdateAccountEmail updateEmailDetails) =>
            {
                // Sanitize and validate data
                if (updateEmailDetails.AccessToken == null || updateEmailDetails.NewEmail == null || updateEmailDetails.AccessToken.Trim().Length == 0 || updateEmailDetails.NewEmail.Trim().Length == 0)
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "All fields must be filled out!"
                    });
                if (!Regex.Match(updateEmailDetails.NewEmail, EmailRegex).Success)
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "Email is in an invalid format!"
                    });
                if (DatabaseManagement.DoesEmailAlreadyExist(updateEmailDetails.NewEmail))
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "Email already exists"
                    });

                // Get user id related to token
                Token? tokenData = DatabaseManagement.GetTokenData(updateEmailDetails.AccessToken);

                // Validate token
                if (tokenData == null)
                    return new MobileAppEndPointResponse((int)ResponseCodes.Unauthorized, new
                    {
                        Message = "Access token is invalid"
                    });

                //Get account info from id
                if (DatabaseManagement.GetAccountInfo(tokenData.Id) is not Account accountInfo)
                    return new MobileAppEndPointResponse((int)ResponseCodes.Unauthorized, new
                    {
                        Message = "Access token linked to an invalid account id... Request a new access token."
                    });

                // Update password
                var (Code, Message) = DatabaseManagement.UpdateAccountEmail(accountInfo.Id, updateEmailDetails.NewEmail);

                return new MobileAppEndPointResponse((int)Code, new
                {
                    Message
                });
            });



            // Endpoint to update account password
            app.MapPut("/Api/UpdatePassword", (UpdateAccountPassword updatePasswordDetails) =>
            {
                // Sanitize and validate data
                if (updatePasswordDetails.AccessToken == null || updatePasswordDetails.OldPassword == null || updatePasswordDetails.NewPassword == null || updatePasswordDetails.AccessToken.Trim().Length == 0 || updatePasswordDetails.OldPassword.Trim().Length == 0 || updatePasswordDetails.NewPassword.Trim().Length == 0)
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "All fields must be filled out!"
                    });

                // Get user id related to token
                Token? tokenData = DatabaseManagement.GetTokenData(updatePasswordDetails.AccessToken);

                // Validate token
                if (tokenData == null)
                    return new MobileAppEndPointResponse((int)ResponseCodes.Unauthorized, new
                    {
                        Message = "Access token is invalid"
                    });

                //Get account info from id
                if (DatabaseManagement.GetAccountInfo(tokenData.Id) is not Account accountInfo)
                    return new MobileAppEndPointResponse((int)ResponseCodes.Unauthorized, new
                    {
                        Message = "Access token linked to an invalid account id... Request a new access token."
                    });

                // Verify old password
                var (Code, Message, UserID) = DatabaseManagement.VerifyUserCredentials(accountInfo.Email, updatePasswordDetails.OldPassword);


                if (Code == ResponseCodes.BadRequest)
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "Supplied current password is incorrect!"
                    });

                if (Code != ResponseCodes.Success)
                    return new MobileAppEndPointResponse((int)ResponseCodes.ServerError, new
                    {
                        Message = "Problem occured server side... Please try again later."
                    });

                // Update password
                var result = DatabaseManagement.UpdateAccountPassword(accountInfo.Id, updatePasswordDetails.NewPassword);

                return new MobileAppEndPointResponse((int)result.Code, new
                {
                    result.Message
                });
            });



            // Endpoint to get account email
            app.MapGet("/Api/GetAccountEmail/{accessToken?}", (string? accessToken) =>
            {
                // Sanitize and validate data
                if (accessToken == null || accessToken.Trim().Length == 0)
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "Must supply an access token"
                    });

                // Get user id related to token
                Token? tokenData = DatabaseManagement.GetTokenData(accessToken);

                // Validate token
                if (tokenData == null)
                    return new MobileAppEndPointResponse((int)ResponseCodes.Unauthorized, new
                    {
                        Message = "Access token is invalid"
                    });

                //Get account info from id
                if (DatabaseManagement.GetAccountInfo(tokenData.Id) is not Account accountInfo)
                    return new MobileAppEndPointResponse((int)ResponseCodes.Unauthorized, new
                    {
                        Message = "Access token linked to an invalid account id... Request a new access token."
                    });

                return new MobileAppEndPointResponse((int)ResponseCodes.Success, new
                {
                    Message = "Successfully obtained account email",
                    accountInfo.Email
                });
            });



            // Endpoint to logout out of all tokens 
            app.MapDelete("/Api/LogoutAll/{token?}", (string? token) =>
            {
                // Bad request if both tokens are null
                if (token is null || token.Length == 0)
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "Must supply a valid token!"
                    });

                Token? tokenData = DatabaseManagement.GetTokenData(token);

                if (tokenData is null)
                    return new MobileAppEndPointResponse((int)ResponseCodes.Unauthorized, new
                    {
                        Message = "Token doesn't exist or is expired!"
                    });

                // Delete all access and refresh token
                DatabaseManagement.DeleteAccountTokens(tokenData.Id, TokenType.AccessToken);
                DatabaseManagement.DeleteAccountTokens(tokenData.Id, TokenType.RefreshToken);

                return new MobileAppEndPointResponse((int)ResponseCodes.Success, new
                {
                    Message = "Successfully logged out from all accounts"
                });
            });



            // Endpoint to delete an account
            app.MapDelete("/Api/DeleteAccount/{token?}/{password?}", (string? token, string? password) =>
            {
                // Bad request if both tokens are null
                if (token is null || token.Length == 0)
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "Must supply a valid token!"
                    });
                if (password is null || password.Length == 0)
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "Must supply the account password!"
                    });

                Token? tokenData = DatabaseManagement.GetTokenData(token);

                if (tokenData is null)
                    return new MobileAppEndPointResponse((int)ResponseCodes.Unauthorized, new
                    {
                        Message = "Token doesn't exist or is expired!"
                    });

                //Get account info from id
                if (DatabaseManagement.GetAccountInfo(tokenData.Id) is not Account accountInfo)
                    return new MobileAppEndPointResponse((int)ResponseCodes.Unauthorized, new
                    {
                        Message = "Access token linked to an invalid account id... Request a new access token."
                    });

                if (!DatabaseManagement.VerifyPassword(password, accountInfo.Password))
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "Password is incorrect"
                    });

                var (Code, Message) = DatabaseManagement.DeleteAccount(tokenData.Id);

                return new MobileAppEndPointResponse((int)Code, new
                {
                    Message,
                });
            });



            // Endpoint to update account notification settings
            app.MapPut("/Api/UpdateNotificationSettings", (UpdateAccountNotificationSettings updateNotificationSettingsDetails) =>
            {
                // Sanitize and validate data
                if (updateNotificationSettingsDetails.AccessToken == null || updateNotificationSettingsDetails.AccessToken.Length == 0 || updateNotificationSettingsDetails.SpoolMeterBatteryLow == null || updateNotificationSettingsDetails.SpoolMeterDied == null || updateNotificationSettingsDetails.MaterialLow == null || updateNotificationSettingsDetails.MaterialRanOut == null)
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "All fields must be filled out!"
                    });

                // Get user id related to token
                Token? tokenData = DatabaseManagement.GetTokenData(updateNotificationSettingsDetails.AccessToken);

                // Validate token
                if (tokenData == null)
                    return new MobileAppEndPointResponse((int)ResponseCodes.Unauthorized, new
                    {
                        Message = "Access token is invalid"
                    });

                // Update notification settings
                var (Code, Message) = DatabaseManagement.UpdateNotificationSettings(tokenData.Id, updateNotificationSettingsDetails.SpoolMeterBatteryLow, updateNotificationSettingsDetails.SpoolMeterDied, updateNotificationSettingsDetails.MaterialLow, updateNotificationSettingsDetails.MaterialRanOut);

                return new MobileAppEndPointResponse((int)Code, new
                {
                    Message
                });
            });



            // Endpoint to get account notification settings
            app.MapGet("/Api/GetAccountNotificationSettings/{accessToken?}", (string? accessToken) =>
            {
                // Sanitize and validate data
                if (accessToken == null || accessToken.Trim().Length == 0)
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "Must supply an access token"
                    });

                // Get user id related to token
                Token? tokenData = DatabaseManagement.GetTokenData(accessToken);

                // Validate token
                if (tokenData == null)
                    return new MobileAppEndPointResponse((int)ResponseCodes.Unauthorized, new
                    {
                        Message = "Access token is invalid"
                    });

                //Get account notification settings
                var (Code, Message, Settings) = DatabaseManagement.GetAccountNotificationSettings(tokenData.Id);

                if (Code == ResponseCodes.Success)
                    return new MobileAppEndPointResponse((int)ResponseCodes.Success, new
                    {
                        Message,
                        Settings!.SpoolMeterBatteryLow,
                        Settings.SpoolMeterDied,
                        Settings.MaterialLow,
                        Settings.MaterialRanOut
                    });
                else
                    return new MobileAppEndPointResponse((int)Code, new
                    {
                        Message
                    });
            });



            // Endpoint to unbind a spool meter from an account
            app.MapDelete("/Api/RemoveSpoolMeterFromAccount/{token?}/{spoolMeterId?}", (string? token, string? spoolMeterId) =>
            {
                // Bad request if both tokens are null
                if (token is null || token.Length == 0)
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "Must supply a valid token!"
                    });
                if (spoolMeterId is null || spoolMeterId.Length == 0)
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "Must supply the spool meter id to delete!"
                    });

                Token? tokenData = DatabaseManagement.GetTokenData(token);

                if (tokenData is null)
                    return new MobileAppEndPointResponse((int)ResponseCodes.Unauthorized, new
                    {
                        Message = "Token doesn't exist or is expired!"
                    });

                //Get account info from id
                if (DatabaseManagement.GetAccountInfo(tokenData.Id) is not Account accountInfo)
                    return new MobileAppEndPointResponse((int)ResponseCodes.Unauthorized, new
                    {
                        Message = "Access token linked to an invalid account id... Request a new access token."
                    });

                if (!DatabaseManagement.DoesSpoolMeterIDExist(spoolMeterId))
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "Supplied spool meter id doesn't exist in the database."
                    });

                // Unbind spool meter from account
                var (Code, Message) = DatabaseManagement.UnbindSpoolMeterFromAccount(tokenData.Id, spoolMeterId);

                return new MobileAppEndPointResponse((int)Code, new
                {
                    Message
                });
            });



            // Endpoint to get all usage logs from all the spool meters from an account
            app.MapGet("/Api/GetAllSpoolMeterUsageLogs/{accessToken?}", (string? accessToken) =>
            {
                // Sanitize and validate data
                if (accessToken == null || accessToken.Trim().Length == 0)
                    return new MobileAppEndPointResponse((int)ResponseCodes.BadRequest, new
                    {
                        Message = "Must supply an access token"
                    });

                // Get user id related to token
                Token? tokenData = DatabaseManagement.GetTokenData(accessToken);

                // Validate token
                if (tokenData == null)
                    return new MobileAppEndPointResponse((int)ResponseCodes.Unauthorized, new
                    {
                        Message = "Access token is invalid"
                    });

                // Remove any expire logs
                _ = DatabaseManagement.RemoveAllExpiredUsageLogs();
                // Get all the spool meters under the specified account
                var (Code, Message, SpoolMeters) = DatabaseManagement.GetUserMeters(tokenData.Id);

                // Remove tokens that have an id that doesn't exist anymore
                if (Code != ResponseCodes.Success || SpoolMeters == null)
                {
                    DatabaseManagement.DeleteAccountTokens(tokenData.Id, TokenType.AccessToken);
                    DatabaseManagement.DeleteAccountTokens(tokenData.Id, TokenType.RefreshToken);

                    return new MobileAppEndPointResponse((int)Code, new
                    {
                        Message
                    });
                }

                List<Usage> logs = [];

                // Get usage logs from all the spool meters from the specified account
                foreach (var spoolMeter in SpoolMeters)
                {
                    var result = DatabaseManagement.GetSpoolMeterUsageLogs(spoolMeter.Id);

                    // Remove tokens that have an id that doesn't exist anymore
                    if (result.Code != ResponseCodes.Success || result.UsageLogs == null)
                        return new MobileAppEndPointResponse((int)Code, new
                        {
                            Message
                        });

                    logs.AddRange(result.UsageLogs.Select(l => l = new Usage() { Id = l.Id, SpoolMeterId = l.SpoolMeterId, RemainingAmountPercentage = l.RemainingAmountPercentage, Time = l.Time }));
                }

                return new MobileAppEndPointResponse((int)ResponseCodes.Success, new
                {
                    Message,
                    Logs = logs
                });
            });
        }
    }
}
