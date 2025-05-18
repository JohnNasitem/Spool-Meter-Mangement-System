//***********************************************************************************
//Program: LoginPage.cs
//Description: Log in page
//Date: Mar 12, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************



using Newtonsoft.Json;
using System.Text;
using Spool_Meter_Viewer.ServerResponses;
using Microsoft.Maui.Storage;
using System.Text.Json.Nodes;

namespace Spool_Meter_Viewer;

public partial class LoginPage : ContentPage
{
	public LoginPage()
	{
		InitializeComponent();
        StatusLabel.IsVisible = false;
    }

    private void DontHaveAccountClick(object sender, TappedEventArgs e)
    {
        // Go to sign up page
        Application.Current!.MainPage = new SignupPage();
    }

    private void ForgotPasswordClick(object sender, TappedEventArgs e)
    {
        // TODO: Redirect to new page, and make that page send an email
        // Send email
    }

    private async void LoginButton_Clicked(object sender, EventArgs e)
    {
        IsBusy = true;
        // log in data
        var data = new
        {
            email = LoginEmail.Text,
            password = LoginPassword.Text,
            fcmToken = await MauiProgram.GetFCMToken()
        };

        try
        {
            // Send POST request to the server with the user's email and password
            HttpResponseMessage response = await MauiProgram.HttpClient.PostAsync("/Api/LogIn",
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