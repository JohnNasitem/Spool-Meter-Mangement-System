//***********************************************************************************
//Program: SpoolMeterCard.cs
//Description: View for displaying a spool meter card
//Date: Mar 14, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************



using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls.Shapes;
using Spool_Meter_Viewer.Classes;

namespace Spool_Meter_Viewer.CustomViews
{
    /// <summary>
    /// Method used when a spool meter card is tapped
    /// </summary>
    /// <param name="spoolMeter">spool meter relating to the card</param>
    /// <returns></returns>
    public delegate Task SpoolMeterCardTapped(SpoolMeter spoolMeter);



    public class SpoolMeterCard : ContentView
    {
        // Spool meter data
        private readonly SpoolMeter _spoolMeter;



        /// <summary>
        /// Hard copy of the a spool meter being used
        /// </summary>
        public SpoolMeter SpoolMeter
        {
            get
            {
                return new SpoolMeter(_spoolMeter);
            }
        }



        /// <summary>
        /// Initializes a new instance of the <see cref="SpoolMeterCard"/> class.
        /// </summary>
        /// <param name="spoolMeter">Spool meter data being used in the card</param>
        /// <param name="cardTapped">Method that gets called when the card is tapped</param>
        public SpoolMeterCard(SpoolMeter spoolMeter, SpoolMeterCardTapped cardTapped)
        {
            //Background of the spool meter view
            Border borderView = new()
            {
                BackgroundColor = spoolMeter.Color,
                Stroke = Colors.Black,
                StrokeThickness = 3,                                                        //Border thickness
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(15) },   //Round corners
                HeightRequest = 200,
                VerticalOptions = LayoutOptions.Fill
            };

            //Spool meter name label
            Label meterNameLabel = new()
            {
                Text = spoolMeter.Name,
                LineBreakMode = LineBreakMode.TailTruncation,       //Truncate the text if it's too long
                FontSize = 25,
                TextColor = Colors.Black,
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(16, 10, 0, 0)
            };

            //Remaining material label
            Label remainingAmountLabel = new()
            {
                Text = SpoolMeter.NumberToUnits(spoolMeter.RemainingAmount, spoolMeter.MaterialTypeUsed.MeasurementScale),
                LineBreakMode = LineBreakMode.TailTruncation,
                FontSize = 40,
                TextColor = Colors.Black,
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            //Material type label
            Label materialType = new()
            {
                Text = spoolMeter.MaterialTypeUsed.Name,
                LineBreakMode = LineBreakMode.TailTruncation,
                FontSize = 25,
                TextColor = Colors.Black,
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.End,
                Margin = new Thickness(16, 0, 0, 10)
            };

            //Battery level image
            Image batterLevel = new()
            {
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, 4, 10, 0)
            };

            //Keep the widths of labels and images inside the border propertional to the size of the border
            borderView.SizeChanged += (sender, e) =>
            {
                meterNameLabel.WidthRequest = borderView.Width * 0.8;
                batterLevel.WidthRequest = borderView.Width * 0.12;
                materialType.WidthRequest = borderView.Width * 0.95;
            };

            //Combine all the elements
            Content = new Grid
            {
                Children =
                        {
                            borderView,
                            meterNameLabel,
                            remainingAmountLabel,
                            batterLevel,
                            materialType
                        }
            };
            Padding = 10;
            BackgroundColor = Colors.Transparent; // Make the container background transparent

            //Add a click event to the border
            TapGestureRecognizer meterTapGestureRecognizer = new();
            meterTapGestureRecognizer.Tapped += async (sender, e) =>
            {
                await cardTapped(spoolMeter);
            };
            borderView.GestureRecognizers.Add(meterTapGestureRecognizer);

            _spoolMeter = spoolMeter;
            UpdateBatteryImage();
        }



        /// <summary>
        /// Sets the battery level image of the spool meter card.
        /// </summary>
        public void UpdateBatteryImage()
        {
            Image batteryLevel = (Image)((Grid)Content).Children[3];
            switch (_spoolMeter.BatteryStatus)
            {
                case BatteryStatus.Full:
                    batteryLevel.Source = "full_battery.png";
                    break;
                case BatteryStatus.High:
                    batteryLevel.Source = "high_battery.png";
                    break;
                case BatteryStatus.Half:
                    batteryLevel.Source = "half_battery.png";
                    break;
                case BatteryStatus.Low:
                    batteryLevel.Source = "low_battery.png";
                    break;
                case BatteryStatus.Dead:
                    batteryLevel.Source = "dead_battery.png";
                    break;
            }
        }
    }
}
