//***********************************************************************************
//Program: SignupPage.cs
//Description: Sign up page
//Date: Mar 12, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************



using Newtonsoft.Json;
using Spool_Meter_Viewer.ServerResponses;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace Spool_Meter_Viewer;

public partial class SignupPage : ContentPage
{
	public SignupPage()
	{
		InitializeComponent();
        StatusLabel.IsVisible = false;
    }

    private void AlreadyHaveAccountClick(object sender, TappedEventArgs e)
    {
        // Go to log in page
        Application.Current!.MainPage = new LoginPage();
    }

    private async void SignupButton_Clicked(object sender, EventArgs e)
    {
        IsBusy = true;
        if (SignupPassword.Text != SignupConfirmPassword.Text)
        {
            await DisplayAlert("Passwords Must Match", "The passwords must be the same!", "OK");
            return;
        }
             
        // Sign in data
        var data = new
        {
            email = SignupEmail.Text,
            password = SignupPassword.Text,
            fcmToken = await MauiProgram.GetFCMToken()
        };

        try
        {
            // Send POST request to the server with the user's email and password
            HttpResponseMessage response = await MauiProgram.HttpClient.PostAsync("/Api/SignUp",
                                                                       new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json"));


            //Parse server response
            var content = await response.Content.ReadAsStringAsync();
            if (JsonConvert.DeserializeObject<ApiResponse>(content) is not ApiResponse apiResponse)
                return;
            var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(apiResponse.Data.ToString()!);

            StatusLabel.IsVisible = true;
            if (loginResponse is LoginResponse login)
            {
                StatusLabel.Text = login.Message;

                // Move to spool meter page if account creation was successful
                if (apiResponse.StatusCode == 200)
                {
                    MauiProgram.AccessToken = login.AccessToken;
                    await SecureStorage.SetAsync("refreshToken", login.RefreshToken);
                    Application.Current!.MainPage = new AppShell();
                }
            }
            else
            {
                StatusLabel.Text = "Something went wrong with connecting to server...";
            }
        }
        catch (Exception ex)
        {
            StatusLabel.IsVisible = true;
            Console.WriteLine(ex.ToString());
            StatusLabel.Text = ex.ToString();
        }
        IsBusy = false;
    }
}