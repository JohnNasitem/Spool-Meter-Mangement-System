//***********************************************************************************
//Program: NotificationSettingsPage.cs
//Description: Modifiy the notification settings
//Date: Mar 19, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************



using Newtonsoft.Json;
using Spool_Meter_Viewer.ServerResponses;

namespace Spool_Meter_Viewer.OtherPages.SettingsPages;

public partial class NotificationSettingsPage : ContentPage
{
    // Disable updating main toggle
    private bool _toggleAll = false;
    // Disable AllPushNotifications_Toggled() functionality
    private bool _disableMainToggle = false;


    private bool[] _oldSettings;



    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationSettingsPage"/> class
    /// </summary>
	public NotificationSettingsPage()
	{
        IsBusy = true;
		InitializeComponent();
        UpdateNotificationSettings();
        _oldSettings = [MauiProgram.NotifyOnSpoolMeterBatteryLow, MauiProgram.NotifyOnSpoolMeterBatteryDead, MauiProgram.NotifyOnRemainingMaterialLow, MauiProgram.NotifyOnRemainingMaterialRanOut];
        IsBusy = false;
        RequestNotificationPermission();
    }


    /// <summary>
    /// Request notification permissions
    /// </summary>
    private async void RequestNotificationPermission()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var status = await Permissions.RequestAsync<Permissions.PostNotifications>();
            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Permission Needed", "Please enable notifications in settings.", "OK");
            }
        }
    }



    /// <summary>
    /// Toggle all the toggles
    /// </summary>
    /// <param name="sender">Object that called the method</param>
    /// <param name="e">Event args</param>
    private async void AllPushNotifications_Toggled(object sender, ToggledEventArgs e)
    {
        await Task.Delay(10);
        ((Switch)sender).OnColor = Color.FromArgb("#512BD4");
        ((Switch)sender).ThumbColor = Color.FromArgb("#512BD4");
        Console.WriteLine($"Main Toggled: {_disableMainToggle}");
        if (!_disableMainToggle)
        {
            _toggleAll = true;
            foreach (ContentView contentView in NotificationSettingsContainer.Children)
                ((Switch)((Grid)contentView.Content).Children[1]).IsToggled = e.Value;
            await Task.Delay(100);
            _toggleAll = false;
        }
        else
            _disableMainToggle = false;
    }



    /// <summary>
    /// Toggle sending notifications for when the spool meter battery is low
    /// </summary>
    /// <param name="sender">Object that called the method</param>
    /// <param name="e">Event args</param>
    private async void SpoolMeterBatteryLow_Toggled(object sender, ToggledEventArgs e)
    {
        await Task.Delay(10);
        ((Switch)sender).OnColor = Color.FromArgb("#512BD4");
        ((Switch)sender).ThumbColor = Color.FromArgb("#512BD4");
        
        MauiProgram.NotifyOnSpoolMeterBatteryLow = e.Value;
        if (!_toggleAll)
            UpdateMainToggle();
    }



    /// <summary>
    /// Toggle sending notifications for when the spool meter died
    /// </summary>
    /// <param name="sender">Object that called the method</param>
    /// <param name="e">Event args</param>
    private async void SpoolMeterDied_Toggled(object sender, ToggledEventArgs e)
    {
        await Task.Delay(10);
        ((Switch)sender).OnColor = Color.FromArgb("#512BD4");
        ((Switch)sender).ThumbColor = Color.FromArgb("#512BD4");
        MauiProgram.NotifyOnSpoolMeterBatteryDead = e.Value;
        if (!_toggleAll)
            UpdateMainToggle();
    }



    /// <summary>
    /// Toggle sending notifications for when the remaining amount of material is low
    /// </summary>
    /// <param name="sender">Object that called the method</param>
    /// <param name="e">Event args</param>
    private async void MaterialLow_Toggled(object sender, ToggledEventArgs e)
    {
        await Task.Delay(10);
        ((Switch)sender).OnColor = Color.FromArgb("#512BD4");
        ((Switch)sender).ThumbColor = Color.FromArgb("#512BD4");
        MauiProgram.NotifyOnRemainingMaterialLow = e.Value;
        if (!_toggleAll)
            UpdateMainToggle();
    }



    /// <summary>
    /// Toggle sending notifications for when the material has ran out
    /// </summary>
    /// <param name="sender">Object that called the method</param>
    /// <param name="e">Event args</param>
    private async void MaterialRanOut_Toggled(object sender, ToggledEventArgs e)
    {
        await Task.Delay(10);
        ((Switch)sender).OnColor = Color.FromArgb("#512BD4");
        ((Switch)sender).ThumbColor = Color.FromArgb("#512BD4");
        MauiProgram.NotifyOnRemainingMaterialRanOut = e.Value;
        if (!_toggleAll)
            UpdateMainToggle();
    }



    /// <summary>
    /// Update settings from Preferences
    /// </summary>
    public async void UpdateNotificationSettings()
    {
        await MauiProgram.FetchAndUpdatetNotificationSettings();

        SpoolMeterBatteryLow.IsToggled = MauiProgram.NotifyOnSpoolMeterBatteryLow;
        SpoolMeterDied.IsToggled = MauiProgram.NotifyOnSpoolMeterBatteryDead;
        MaterialLow.IsToggled = MauiProgram.NotifyOnRemainingMaterialLow;
        MaterialRanOut.IsToggled = MauiProgram.NotifyOnRemainingMaterialRanOut;
    }



    /// <summary>
    /// Check if all the toggles are switched (except the main toggle)
    /// </summary>
    /// <returns>true if all toggles except the main toggle are toggled on, otherwise false</returns>
    private bool AreAllSwitchesToggled()
    {
        return SpoolMeterBatteryLow.IsToggled && SpoolMeterDied.IsToggled && MaterialLow.IsToggled && MaterialRanOut.IsToggled;
    }



    /// <summary>
    /// Update the main toggle state
    /// </summary>
    private void UpdateMainToggle()
    {
        Console.WriteLine("UpdateMainToggle() called!");
        _disableMainToggle = true;
        AllPushNotifications.IsToggled = AreAllSwitchesToggled();
    }


    /// <summary>
    /// Send update if notification settings changed
    /// </summary>
    protected async override void OnDisappearing()
    {
        base.OnDisappearing();


        if (_oldSettings.SequenceEqual([MauiProgram.NotifyOnSpoolMeterBatteryLow, MauiProgram.NotifyOnSpoolMeterBatteryDead, MauiProgram.NotifyOnRemainingMaterialLow, MauiProgram.NotifyOnRemainingMaterialRanOut]))
        {
            object data = new 
            { 
                MauiProgram.AccessToken,
                SpoolMeterBatteryLow = MauiProgram.NotifyOnSpoolMeterBatteryLow,
                SpoolMeterDied = MauiProgram.NotifyOnSpoolMeterBatteryDead,
                MaterialLow = MauiProgram.NotifyOnRemainingMaterialLow,
                MaterialRanOut = MauiProgram.NotifyOnRemainingMaterialRanOut 
            };

            // Make api request to log out from all devices
            HttpResponseMessage? response = await MauiProgram.MakeApiCall(MauiProgram.RequestType.PUT, $"/Api/UpdateNotificationSettings/", data);

            if (response == null)
                return;


            //Parse server response
            var content = await response.Content.ReadAsStringAsync();
            if (JsonConvert.DeserializeObject<ApiResponse>(content) is not ApiResponse apiResponse)
            {
                await DisplayAlert("Server Error", "Unable to save notification settings to server. Changes have not been saved!", "OK");
                return;
            }
            var result = JsonConvert.DeserializeObject<BasicResponse>(apiResponse.Data.ToString()!);

            if (apiResponse.StatusCode != 200)
            {
                await DisplayAlert("Error", result!.Message, "OK");
                return;
            }
        }
    }
}