//***********************************************************************************
//Program: MainPage.xaml.cs
//Description: Page displaying all the spool meters
//Date: Feb 10, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************


//Battery icon
//<a href="https://www.flaticon.com/free-icons/battery" title="battery icons">Battery icons created by Stockio - Flaticon</a>

using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;
using System.Diagnostics.Metrics;
using Newtonsoft.Json;
using Spool_Meter_Viewer.ServerResponses;
using Spool_Meter_Viewer.Classes;
using System.Collections.Generic;
using Spool_Meter_Viewer.CustomViews;
using Plugin.Firebase.CloudMessaging;


namespace Spool_Meter_Viewer
{
    public partial class MainPage : ContentPage
    {
        /// <summary>
        /// Is a spool meter already being editted
        /// </summary>
        public bool PageAlreadyOpenned { get; set; } = false;


        /// <summary>
        /// Initializes a new instance of the <see cref="MainPage"/> class.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await RequestNotificationPermission();
            MauiProgram.Materials = await GetMaterialTypes();
            await RefreshSpoolMeters();
            await RequestNotificationPermission();
        }



        /// <summary>
        /// Request notification permissions
        /// </summary>
        private async Task RequestNotificationPermission()
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                var status = await Permissions.RequestAsync<Permissions.PostNotifications>();
            }
        }



        /// <summary>
        /// Make an api call to the server to get a list of all the material types.
        /// </summary>
        /// <returns>List of material types</returns>
        private static async Task<List<MaterialType>> GetMaterialTypes()
        {
            // Send GET request to the server with the users token
            HttpResponseMessage? response = await MauiProgram.MakeApiCall(MauiProgram.RequestType.GET, "/Api/GetMaterialTypes", null);

            // response should never be null after this get request as it doesnt need a token, but here just in case
            if (response == null)
                return [];

            try
            {
                //Parse server response
                var content = await response.Content.ReadAsStringAsync();
                if (JsonConvert.DeserializeObject<ApiResponse>(content) is not ApiResponse apiResponse)
                    return [];
                var materialTypesResponse = JsonConvert.DeserializeObject<MaterialTypesResponse>(apiResponse.Data.ToString()!);
                var materialTypes = JsonConvert.DeserializeObject<List<MaterialType>>(materialTypesResponse!.MaterialTypes.ToString()!);

                if (materialTypes == null)
                    return [];

                return materialTypes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Problem with parsing response in GetMaterialTypes() - Exception: {ex}");
                return [];
            }
        }



        /// <summary>
        /// Redisplays all the spool meters.
        /// </summary>
        public async Task RefreshSpoolMeters()
        {
            if (await GetSpoolMeters())
                RedisplaySpoolMeters();
        }



        /// <summary>
        /// Get account spool meters from database
        /// </summary>
        /// <returns>true if the api call was a success, otherwise false</returns>
        private static async Task<bool> GetSpoolMeters()
        {
            // Send GET request to the server with the users token
            HttpResponseMessage? response = await MauiProgram.MakeApiCall(MauiProgram.RequestType.GET, $"/Api/GetUserSpoolMeters/{MauiProgram.AccessToken}", null);

            if (response == null)
                return false;

            Console.WriteLine("Token is valid!");

            //Parse server response
            var content = await response.Content.ReadAsStringAsync();
            if (JsonConvert.DeserializeObject<ApiResponse>(content) is not ApiResponse apiResponse)
                return false;
            var spoolMeterResponse = JsonConvert.DeserializeObject<SpoolMeterResponse>(apiResponse!.Data.ToString()!);
            var spoolMeters = JsonConvert.DeserializeObject<List<SpoolMeter>>(spoolMeterResponse!.SpoolMeters.ToString()!);

            if (spoolMeters == null)
                return false;

            MauiProgram.SpoolMeters = spoolMeters;

            return true;
        }



        /// <summary>
        /// Redisplay the spool meters on the page and add the add button
        /// </summary>
        private void RedisplaySpoolMeters()
        {
            string[] weightUnits = ["g", "kg"];
            string[] lenghtUnits = ["cm", "m"];

            ScrollView scrollView = new();
            StackLayout meterLayout = [];
            AbsoluteLayout pageLayout = [];

            foreach (SpoolMeter meter in MauiProgram.SpoolMeters)
            {
                SpoolMeterCard card = new(meter, OpenMeterDetails);
                meterLayout.Add(card);
            }

            //Create a circle view
            var circle = new Border
            {
                BackgroundColor = Color.FromArgb("#512BD4"),
                Stroke = Color.FromArgb("#512BD4"),
                StrokeThickness = 2,                                                        //Border thickness
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(50) },   //Round corners
                WidthRequest = 70,
                HeightRequest = 70,
                Padding = 0,
                Margin = 0,
                Content = new Label()
                {
                    Text = "+",
                    TextColor = Color.FromArgb("#FFFFFF"),
                    FontSize = 40,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                }
            };


            TapGestureRecognizer addMeterTapGestureRecognizer = new();
            addMeterTapGestureRecognizer.Tapped += async (sender, e) => { await AddSpoolMeter(); };
            circle.GestureRecognizers.Add(addMeterTapGestureRecognizer);

            //AbsoluteLayout.SetLayoutBounds(meterLayout, new Rect(0, 0, 1, 1));
            //AbsoluteLayout.SetLayoutFlags(meterLayout, AbsoluteLayoutFlags.All);
            scrollView.Content = meterLayout;
            AbsoluteLayout.SetLayoutBounds(scrollView, new Rect(0, 0, 1, 1));
            AbsoluteLayout.SetLayoutFlags(scrollView, AbsoluteLayoutFlags.All);
            pageLayout.Children.Add(scrollView);

            // Add the circle to the AbsoluteLayout
            AbsoluteLayout.SetLayoutBounds(circle, new Rect(0.5, 1, 100, 100));
            AbsoluteLayout.SetLayoutFlags(circle, AbsoluteLayoutFlags.PositionProportional);
            pageLayout.Children.Add(circle);

            Content = pageLayout;
        }



        /// <summary>
        /// Add a spool meter
        /// </summary>
        private async Task AddSpoolMeter()
        {
            await Navigation.PushAsync(new BluetoothPage(this));
        }



        /// <summary>
        /// Opens the details page of the selected spool meter.
        /// </summary>
        /// <param name="meter">Meter to open</param>
        private async Task OpenMeterDetails(SpoolMeter meter)
        {
            if (PageAlreadyOpenned)
                return;

            try
            {
                MeterDetails detailsPage = new MeterDetails(this);
                detailsPage.SpoolMeter = meter;
                PageAlreadyOpenned = true;
                await Navigation.PushAsync(detailsPage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OpenMeterDetails() - Failed to connect to device - Exception: {ex}");
                await DisplayAlert("Unable to connect to device", "Failed to connect to the selected spool meter! Make sure bluetooth is enabled, you are in range, and spool meter is turned on", "OK");
                PageAlreadyOpenned = false;
            }
        }
    }
}
