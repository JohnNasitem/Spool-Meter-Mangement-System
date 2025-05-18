//***********************************************************************************
//Program: MauiProgram.cs
//Description: Holds properties and methods used in different pages
//Date: Mar 12, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************


// TODO: bug with starting app, then switching to sign up then switching back causes a object disposed exception

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Spool_Meter_Viewer.ServerResponses;
using System.Text;
using Spool_Meter_Viewer.Classes;
using Plugin.BLE.Abstractions.Contracts;
using Spool_Meter_Viewer.OtherPages.SettingsPages;
using CommunityToolkit.Maui;
using SkiaSharp.Views.Maui.Controls.Hosting;
using LiveChartsCore.SkiaSharpView.Maui;
using Plugin.BLE;
using Microsoft.Maui.LifecycleEvents;
using Plugin.Firebase.CloudMessaging;


#if ANDROID
using Plugin.Firebase.Core.Platforms.Android;
#endif

// Settings icon from <a href="https://www.flaticon.com/free-icons/settings" title="settings icons">Settings icons created by Pixel perfect - Flaticon</a>
// Graph icon from <a href="https://www.flaticon.com/free-icons/graph" title="graph icons">Graph icons created by ibobicon - Flaticon</a>
// Spool icon from <a href="https://www.flaticon.com/free-icons/filament" title="filament icons">Filament icons created by Freepik - Flaticon</a>
// Edit icon from <a href="https://www.flaticon.com/free-icons/edit" title="edit icons">Edit icons created by Pixel perfect - Flaticon</a>
// Reject icon from <a href="https://www.flaticon.com/free-icons/exit" title="exit icons">Exit icons created by GOFOX - Flaticon</a>

namespace Spool_Meter_Viewer
{
    public static class MauiProgram
    {
        public static readonly string ServerIP = "HIDDEN";

        public const double NEW_SESSION_PERCENTAGE = 0.1;


        public static HttpClient HttpClient { get; set; } = new();
        public static IAdapter? BluetoothAdapter { get; set; } = CrossBluetoothLE.Current.Adapter;
        public static IBluetoothLE? BluetoothLE { get; set; } = CrossBluetoothLE.Current;
        public static IDevice? BluetoothConnectedDevice { get; set; } = null;


        public static string AccessToken { get; set; } = "";



        public static bool NotifyOnSpoolMeterBatteryLow { get; set; } = true;
        public static bool NotifyOnSpoolMeterBatteryDead { get; set; } = true;
        public static bool NotifyOnRemainingMaterialLow { get; set; } = true;
        public static bool NotifyOnRemainingMaterialRanOut{ get; set; } = true;

        /// <summary>
        /// All the connceted spool meters
        /// </summary>
        public static List<SpoolMeter> SpoolMeters { get; set; } = [];

        /// <summary>
        /// All material types
        /// </summary>
        public static List<MaterialType> Materials { get; set; } = [];

        public static MauiApp CreateMauiApp()
        {
            var handler = new HttpClientHandler
            {
                // This ignores the certificate for prototype
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            HttpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(ServerIP)
            };

            HttpClient.DefaultRequestHeaders.Accept.Clear();
            HttpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var builder = MauiApp.CreateBuilder();
            builder
                .UseSkiaSharp()
                .UseLiveCharts()
                .UseMauiApp<App>()
                .RegisterFirebaseServices()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }



        /// <summary>
        /// Get the device's FCM token
        /// </summary>
        /// <returns></returns>
        public async static Task<string> GetFCMToken()
        {
            await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();
            string tcmToken = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
            Console.WriteLine($"FcmToken found: {tcmToken}");
            return tcmToken;
        }


        private static MauiAppBuilder RegisterFirebaseServices(this MauiAppBuilder builder)
        {
#if ANDROID
            builder.ConfigureLifecycleEvents(events =>
            {
                events.AddAndroid(andriod => andriod.OnCreate((activity, _) =>
                CrossFirebase.Initialize(activity)));
            });
#endif
            return builder;
        }



        /// <summary>
        /// Make an api call to the server
        /// </summary>
        /// <param name="requestType">Type of request being made</param>
        /// <param name="endpoint">endpoint of the request</param>
        /// <param name="data">any data being send through post or put, use null for get and delete</param>
        /// <returns>HttpResponseMessage if request is successful, null if tokens are invalid</returns>
        /// <exception cref="Exception">Throw a new exception if the the client failed to make an api call</exception>
        public static async Task<HttpResponseMessage?> MakeApiCall(RequestType requestType, string endpoint, object? data)
        {
            HttpResponseMessage? response = null;

            // Make api call
            switch (requestType)
            {
                case RequestType.GET:
                    response = await HttpClient.GetAsync(endpoint);
                    break;

                case RequestType.POST:
                    response = await HttpClient.PostAsync(endpoint, new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json"));
                    break;

                case RequestType.PUT:
                    response = await HttpClient.PutAsync(endpoint, new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json"));
                    break;

                case RequestType.DELETE:
                    response = await HttpClient.DeleteAsync(endpoint);
                    break;
            }

            if (response == null)
                throw new Exception("Api call wasn't sent!");

            // Check if the token is expired and the server response is proper
            try
            {
                var content = await response.Content.ReadAsStringAsync();
                if (JsonConvert.DeserializeObject<ApiResponse>(content) is not ApiResponse apiResponse)
                    return null;

                // Check if access token exipired
                if (apiResponse.StatusCode == 401)
                {
                    // If token is expire and refresh token return null
                    if (!await RefreshTokens())
                    {
                        Console.WriteLine("Refresh token is invalid go back to log in page!");
                        Application.Current!.MainPage = new LoginPage();
                        AccessToken = "";
                        SecureStorage.Remove("refreshToken");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("MakeApiCall() - Problem with parsing response. Exception: {ex}");
                throw new Exception("MakeApiCall() - Server error");
                // TODO: Inform user there was a problem and remove the throw exception
            }

            return response;
        }



        /// <summary>
        /// Make an api call to refresh the tokens
        /// </summary>
        /// <returns>true if tokens were refreshed, false if refresh token was invalid</returns>
        public static async Task<bool> RefreshTokens()
        {
            string? refreshToken = await SecureStorage.GetAsync("refreshToken");

            if (refreshToken == null)
                return false;

            // Token data
            var tokens = new
            {
                refreshToken,
                accessToken = ""
            };

            // Send POST request to refresh tokens
            try
            {
                HttpResponseMessage response = await HttpClient.PostAsync("/Api/RefreshTokens",
                                                                       new StringContent(JsonConvert.SerializeObject(tokens), Encoding.UTF8, "application/json"));

                //Parse server response
                var content = await response.Content.ReadAsStringAsync();
                if (JsonConvert.DeserializeObject<ApiResponse>(content) is not ApiResponse apiResponse)
                    return false;
                var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(apiResponse.Data.ToString()!);

                // If response is valid, then initialize tokens
                if (loginResponse is LoginResponse login && apiResponse.StatusCode == 200)
                {
                    // Initialize tokens
                    AccessToken = login.AccessToken;
                    await SecureStorage.SetAsync("refreshToken", login.RefreshToken);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RefreshTokens() - Problem occured when trying to fresh tokens. Exception: {ex}");
                return false;
            }
        }



        /// <summary>
        /// Update static notification properties
        /// </summary>
        /// <returns>true if update was successfull, otherwise false</returns>
        public async static Task<bool> FetchAndUpdatetNotificationSettings()
        {
            // Send GET request to the server with the users token
            HttpResponseMessage? response = await MakeApiCall(RequestType.GET, $"/Api/GetAccountNotificationSettings/{AccessToken}", null);

            if (response == null)
                return false;

            //Parse server response
            var content = await response.Content.ReadAsStringAsync();
            if (JsonConvert.DeserializeObject<ApiResponse>(content) is not ApiResponse apiResponse)
                return false;
            var result = JsonConvert.DeserializeObject<GetAccountNotificationSettings>(apiResponse.Data.ToString()!);

            if (apiResponse.StatusCode == 200)
            {
                NotifyOnSpoolMeterBatteryLow = (bool)result!.SpoolMeterBatteryLow!;
                NotifyOnSpoolMeterBatteryDead = (bool)result!.SpoolMeterDied!;
                NotifyOnRemainingMaterialLow = (bool)result!.MaterialLow!;
                NotifyOnRemainingMaterialRanOut = (bool)result!.MaterialRanOut!;
                return true;
            }

            return false;
        }


        public enum RequestType
        {
            GET,
            POST,
            PUT,
            DELETE
        }
    }
}
