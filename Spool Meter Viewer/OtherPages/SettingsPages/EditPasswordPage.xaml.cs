//***********************************************************************************
//Program: EditPasswordPage.cs
//Description: Send request to edit user password
//Date: Mar 19, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************



using Newtonsoft.Json;
using Spool_Meter_Viewer.ServerResponses;

namespace Spool_Meter_Viewer.OtherPages.SettingsPages;

public partial class EditPasswordPage : ContentPage
{
	public EditPasswordPage()
	{
		InitializeComponent();
        UpdatePasswordButton.IsEnabled = false;

    }

    private async void UpdatePasswordButton_Pressed(object sender, EventArgs e)
    {
        IsBusy = true;
        if (NewPasswordEntry.Text != ConfirmNewPasswordEntry.Text)
        {
            await DisplayAlert("Passwords not matching!", "New passwords must match exactly!", "OK");
            return;
        }

        object data = new
        {
            MauiProgram.AccessToken,
            OldPassword = OldPasswordEntry.Text,
            NewPassword = NewPasswordEntry.Text,
        };

        // Send GET request to the server with the users token
        HttpResponseMessage? response = await MauiProgram.MakeApiCall(MauiProgram.RequestType.PUT, "/Api/UpdatePassword", data);

        if (response == null)
            return;

        //Parse server response
        var content = await response.Content.ReadAsStringAsync();
        if (JsonConvert.DeserializeObject<ApiResponse>(content) is not ApiResponse apiResponse)
            return;

        IsBusy = false;
        switch(apiResponse.StatusCode)
        {
            case 200:
                await DisplayAlert("Success!", "Successfully updated password", "OK");
                await Navigation.PopAsync();
                return;

            case 400:
                await DisplayAlert("Incorrect Password!", "The current password is incorrect!", "OK");;
                return;

            default:
                await DisplayAlert("Server error!", "Error with the server! Please try again later.", "OK"); ;
                return;
        }
    }



    /// <summary>
    /// Enable button if all fields are popualated
    /// </summary>
    private void UpdateButtonState(object sender, TextChangedEventArgs e)
    {
        UpdatePasswordButton.IsEnabled = OldPasswordEntry.Text is not null && NewPasswordEntry.Text is not null && ConfirmNewPasswordEntry.Text is not null && OldPasswordEntry.Text.Length > 0 && NewPasswordEntry.Text.Length > 0 && ConfirmNewPasswordEntry.Text.Length > 0;
    }
}