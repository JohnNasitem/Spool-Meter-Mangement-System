//***********************************************************************************
//Program: EditEmailPage.cs
//Description: Send request to edit user email
//Date: Mar 19, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************



using Newtonsoft.Json;
using Spool_Meter_Viewer.ServerResponses;

namespace Spool_Meter_Viewer.OtherPages.SettingsPages;

public partial class EditEmailPage : ContentPage
{
	public EditEmailPage(string currentEmail)
	{
		InitializeComponent();
        UpdateEmailButton.IsEnabled = false;
        OldEmailEntry.Text = currentEmail;
    }

    private async void UpdateEmailButton_Pressed(object sender, EventArgs e)
    {
        IsBusy = true;
        object data = new
        {
            MauiProgram.AccessToken,
            NewEmail = NewEmailEntry.Text,
        };

        // Send GET request to the server with the users token
        HttpResponseMessage? response = await MauiProgram.MakeApiCall(MauiProgram.RequestType.PUT, "/Api/UpdateEmail", data);

        if (response == null)
            return;

        //Parse server response
        var content = await response.Content.ReadAsStringAsync();
        if (JsonConvert.DeserializeObject<ApiResponse>(content) is not ApiResponse apiResponse)
            return;

        switch (apiResponse.StatusCode)
        {
            case 200:
                await DisplayAlert("Success!", "Successfully updated email", "OK");
                await Navigation.PopAsync();
                return;

            default:
                await DisplayAlert("Server error!", "Error with the server! Please try again later.", "OK"); ;
                return;
        }
        IsBusy = false;
    }

    private void NewEmailEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateEmailButton.IsEnabled = NewEmailEntry.Text is not null && NewEmailEntry.Text.Length > 0;
    }
}