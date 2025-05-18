//***********************************************************************************
//Program: BasicPopup.cs
//Description: Popup page to get a yes or no answer
//Date: Mar 24, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************



using CommunityToolkit.Maui.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spool_Meter_Viewer.Popups
{
    public class YesNoPopup : Popup
    {
        public bool Yes { get; set; } = false;


        // Called when yes is pressed
        private Action _yesEvent;

        public YesNoPopup(Action yesEvent, string message, Color? noButtonColor = null, Color? yesButtonColor = null)
        {
            _yesEvent = yesEvent;

            // Set color defaults
            noButtonColor ??= Color.FromArgb("#512BD4");
            yesButtonColor ??= Color.FromArgb("#512BD4");

            var cancelButton = new ImageButton
            {
                HorizontalOptions = LayoutOptions.Start,
                Source = "reject.png",
                HeightRequest = 20,
                WidthRequest = 20,
                Command = new Command(() => Close())
            };
            var messageLabel = new Label
            {
                Text = message,
                TextColor = Colors.Black,
                FontSize = 15,
                HorizontalOptions = LayoutOptions.Start,
                LineBreakMode = LineBreakMode.WordWrap,
                WidthRequest = 330
            };
            var noButton = new Button
            {
                TextColor = Colors.White,
                BackgroundColor = noButtonColor,
                HorizontalOptions = LayoutOptions.End,
                Margin = 0,
                Padding = 0,
                WidthRequest = 160,
                Text = "No",
                Command = new Command(() => Close())
            };
            var yesButton = new Button
            {
                TextColor = Colors.White,
                BackgroundColor = yesButtonColor,
                HorizontalOptions = LayoutOptions.Start,
                WidthRequest = 160,
                Margin = 0,
                Padding = 0,
                Text = "Yes",
                Command = new Command(() =>
                {
                    Yes = true;
                    Close();
                    _yesEvent?.Invoke();
                })
            };
            var grid = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Star } },
                RowDefinitions = { new RowDefinition { Height = GridLength.Auto }, new RowDefinition { Height = GridLength.Auto }, new RowDefinition { Height = GridLength.Auto } },
                BackgroundColor = Colors.White,
                WidthRequest = 350,
                RowSpacing = 10,
                ColumnSpacing = 10,
                Padding = 10
            };

            grid.SetRow(cancelButton, 0);
            grid.SetColumn(cancelButton, 0);

            grid.SetRow(messageLabel, 1);
            grid.SetColumn(messageLabel, 0);

            grid.SetRow(noButton, 2);
            grid.SetColumn(noButton, 0);
            grid.SetRow(yesButton, 2);
            grid.SetColumn(yesButton, 1);

            grid.Children.Add(cancelButton);
            grid.Children.Add(messageLabel);
            grid.Children.Add(noButton);
            grid.Children.Add(yesButton);

            Content = grid;
        }
    }
}
