//***********************************************************************************
//Program: DeleteAccountConfirmPopup.cs
//Description: Popup page to confirm if the user wants to delete the account
//Date: Mar 24, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************



using CommunityToolkit.Maui.Views;
using Newtonsoft.Json;
using Spool_Meter_Viewer.ServerResponses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spool_Meter_Viewer.Popups
{
    public class DeleteAccountConfirmPopup : Popup
    {
        public bool AccountDeleted { get; private set; } = false;


        // Called when account is deleted
        private Action _deleteAccEvent;

        public DeleteAccountConfirmPopup(Action deleteAccEvent)
        {
            _deleteAccEvent = deleteAccEvent;

            Label statusLabel = new Label
            {
                TextColor = Colors.Black,
                IsVisible = false,
                FontSize = 12
            };
            Entry passwordEntry = new Entry
            {
                TextColor = Colors.Black,
                IsPassword = true,
                Placeholder = "Password",
                HorizontalOptions = LayoutOptions.Fill,
                
            };
            Button deleteButton = new Button
            {
                Text = "Delete Account",
                TextColor = Colors.White,
                BackgroundColor = Colors.Red,
                Command = new Command(async () =>
                {
                    // Make api call to delete account
                    // Send GET request to the server with the users token
                    HttpResponseMessage? response = await MauiProgram.MakeApiCall(MauiProgram.RequestType.DELETE, $"/Api/DeleteAccount/{MauiProgram.AccessToken}/{passwordEntry.Text}", null);

                    if (response == null)
                    {
                        Close();
                        return;
                    }

                    //Parse server response
                    var content = await response.Content.ReadAsStringAsync();
                    if (JsonConvert.DeserializeObject<ApiResponse>(content) is not ApiResponse apiResponse)
                    {
                        statusLabel.Text = "Something went wrong with the server. Please try again later";
                        statusLabel.IsVisible = true;
                        return;
                    }
                    var result = JsonConvert.DeserializeObject<BasicResponse>(apiResponse.Data.ToString()!);

                    // Close popup if api call is successfully
                    if (apiResponse.StatusCode == 200)
                    {
                        AccountDeleted = true;
                        Close();
                        _deleteAccEvent.Invoke();
                    }
                    // If not then display error message
                    else
                    {
                        statusLabel.Text = result!.Message;
                        statusLabel.IsVisible = true;
                    }
                })
            };

            passwordEntry.TextChanged += (s, e) => deleteButton.IsEnabled = e.NewTextValue is not null && e.NewTextValue.Length > 0;


            // Define popup content
            Content = new VerticalStackLayout
            {
                BackgroundColor = Colors.White,
                WidthRequest = 350,
                Spacing = 10,
                Padding = 10,
                Children =
                {
                    new ImageButton
                    {
                       HorizontalOptions = LayoutOptions.Start,
                       Source = "reject.png",
                       HeightRequest = 20,
                       WidthRequest = 20,
                       Command = new Command(() => Close())
                    },
                    new Label
                    {
                        TextColor = Colors.Black,
                        Text = "Type your password to finish the account deletion process",
                        FontSize = 15,
                        HorizontalOptions = LayoutOptions.Center
                    },
                    passwordEntry,
                    deleteButton,
                    statusLabel
                }
            };
        }
    }
}
