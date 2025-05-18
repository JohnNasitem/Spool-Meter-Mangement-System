//***********************************************************************************
//Program: AccountSettingsPage.cs
//Description: Modifiy the account settings
//Date: Mar 19, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************



using CommunityToolkit.Maui.Views;
using Newtonsoft.Json;
using Spool_Meter_Viewer.Popups;
using Spool_Meter_Viewer.ServerResponses;

namespace Spool_Meter_Viewer.OtherPages.SettingsPages;

public partial class AccountSettingsPage : ContentPage
{
    // current account email
    private string _currentEmail = "";


    public AccountSettingsPage()
	{
		InitializeComponent();
    }


    /// <summary>
    /// Update current email on page appear
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateEmailField();
    }



    /// <summary>
    /// Bring up edit email page
    /// </summary>
    /// <param name="sender">Object that called the code</param>
    /// <param name="e">Event args</param>
    private async void EditEmailButton_Pressed(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new EditEmailPage(_currentEmail));
    }



    /// <summary>
    /// Bring up edit password page
    /// </summary>
    /// <param name="sender">Object that called the code</param>
    /// <param name="e">Event args</param>
    private async void EditPasswordButton_Pressed(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new EditPasswordPage());
    }



    /// <summary>
    /// Bring up a pop up to confirm the account deletion
    /// If user confirms then delete account and log out
    /// </summary>
    /// <param name="sender">Object that called the code</param>
    /// <param name="e">Event args</param>
    private void DeleteAccountButton_Pressed(object sender, EventArgs e)
    {
        // Display confirmation pop up
        var popup = new DeleteAccountConfirmPopup(new Action(() =>
        {
            LogOutButton_Pressed(null!, null!);
        }));
        this.ShowPopup(popup);
    }



    /// <summary>
    /// Send api call to get the current account email
    /// </summary>
    private async void UpdateEmailField()
    {
        // Send GET request to the server with the users token
        HttpResponseMessage? response = await MauiProgram.MakeApiCall(MauiProgram.RequestType.GET, $"/Api/GetAccountEmail/{MauiProgram.AccessToken}", null);

        if (response == null)
            return;

        //Parse server response
        var content = await response.Content.ReadAsStringAsync();
        if (JsonConvert.DeserializeObject<ApiResponse>(content) is not ApiResponse apiResponse)
            return;
        var result = JsonConvert.DeserializeObject<GetAccountEmailResponse>(apiResponse.Data.ToString()!);

        _currentEmail = result!.Email;
        EmailText.Text = _currentEmail;
    }



    /// <summary>
    /// Log user out of the account
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void LogOutButton_Pressed(object sender, EventArgs e)
    {
        string? refreshToken = await SecureStorage.GetAsync("refreshToken");

        // Clear stored tokens
        MauiProgram.AccessToken = "";
        SecureStorage.Remove("refreshToken");

        // Redirect to log in page
        Application.Current!.MainPage = new LoginPage();

        // Make api request to log out from all devices
        HttpResponseMessage? response = await MauiProgram.MakeApiCall(MauiProgram.RequestType.DELETE, $"/Api/LogoutAll/{refreshToken ?? ""}", null);
    }
}