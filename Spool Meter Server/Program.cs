//***********************************************************************************
//Program: Program.cs
//Description: Web app main program
//Date: Mar 17, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************


// TODO: As for right now i think you can only be logged into 1 device at once with the same account, due to clearing every token every single time you request a new one, can log out of all other instances this way though by deleting all the existing tokens


using Spool_Meter_Server.ServerEndpoints;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

// Densities from: https://bitfab.io/blog/3d-printing-materials-densities/

namespace Spool_Meter_Server
{
    public class Program
    {
        // Settings
        public const int USER_ID_LENGTH = 5;                                                // User ID Length
        public static readonly TimeSpan AccessTokenTTL = new(1, 0, 0);             // How long should Access Tokens be valid for
        public static readonly TimeSpan RefreshTokenTTL = new(30, 0, 0, 0);        // How long should Refresh Tokens be valid for
        public static readonly TimeSpan UsageLogTTL = new(30, 0, 0, 0);

        // Taken by https://regex101.com/library/SOgUIV
        public static string EmailRegex { get; } = @"^((?!\.)[\w\-_.]*[^.])(@\w+)(\.\w+(\.\w+)?[^.\W])$";

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers();
            var app = builder.Build();
            //Fix CORS policy issue
            //Allow web service to be called from any website (local or live)
            app.UseCors((x) => x.AllowAnyMethod().AllowAnyHeader().SetIsOriginAllowed(origin => true));

            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mauifirebasedemo-firebase-adminsdk.json")),
            });

            // Endpoint to check if the server is actually running
            app.MapGet("", () =>
            {
                return new MobileAppEndPointResponse((int)ResponseCodes.Success, new
                {
                    Message = "Spool meter server"
                });
            });

            // Set all the endpoints
            MobileAppEndPoints.SetEndpoints(app);
            SpoolMeterEndPoints.SetEndpoints(app);

            app.Run();
        }

        /// <summary>
        /// Battery status
        /// </summary>
        public enum BatteryStatus
        {
            Full,
            High,
            Half,
            Low,
            Dead,
        }



        public enum ResponseCodes
        {
            Success = 200,
            BadRequest = 400,
            Unauthorized = 401,
            ServerError = 500,
        }



        public enum MaterialMeasurement
        {
            Weight,
            Length,
        }
    }
}
