//***********************************************************************************
//Program: UsageViewModel.cs
//Description: View model for the usage graph
//Date: Mar 26, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************



using System.Collections.ObjectModel;
using System.Globalization;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Newtonsoft.Json;
using SkiaSharp;
using Spool_Meter_Viewer.Classes;
using Spool_Meter_Viewer.ServerResponses;


namespace Spool_Meter_Viewer.ViewModels
{
    public class UsageViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UsageViewModel"/>
        /// </summary>
        public UsageViewModel()
        {
            Series = [];
            var xAxis = new DateTimeAxis(TimeSpan.FromMinutes(1), (date) =>
            {
                string str = date.ToString("MMM dd - HH:mm", CultureInfo.InvariantCulture);
                return str;
            });
            xAxis.Name = "Last 30 Days";
            xAxis.NameTextSize = 10;
            xAxis.TextSize = 10;
            XAxes = [xAxis];
        }


        /// <summary>
        /// ISeries
        /// </summary>
        public ISeries[] Series { get; set; }



        /// <summary>
        /// XAxis settings
        /// </summary>
        public Axis[] XAxes { get; set; } =
        {
            new DateTimeAxis(TimeSpan.FromMinutes(1), (date) =>
            {
                string str = date.ToString("MMM dd - HH:mm", CultureInfo.InvariantCulture);
                return str;
            }),
        };



        /// <summary>
        /// YAxis settings
        /// </summary>
        public Axis[] YAxes { get; set; } =
        {
            new Axis
            {
                Name = "Percentage Left",
                NameTextSize = 10,
                TextSize = 10
            }
        };



        /// <summary>
        /// Get the graph data
        /// </summary>
        public async Task<ISeries[]> GetGraphData()
        {
            ISeries[]? logs = await GetUsageLogs();

            if (logs != null)
                Series = logs;

            return Series;
        }



        /// <summary>
        /// Make api call to get usage logs
        /// </summary>
        /// <returns>ISeries if call was successfull, otherwise null</returns>
        private async Task<ISeries[]?> GetUsageLogs()
        {
            // Send GET request to the server with the users token
            HttpResponseMessage? response = await MauiProgram.MakeApiCall(MauiProgram.RequestType.GET, $"/Api/GetAllSpoolMeterUsageLogs/{MauiProgram.AccessToken}", null);

            if (response == null)
                return null;

            //Parse server response
            string content = await response.Content.ReadAsStringAsync();
            Console.WriteLine(content);
            if (JsonConvert.DeserializeObject<ApiResponse>(content) is not ApiResponse apiResponse)
                return null;
            if (JsonConvert.DeserializeObject<GetUsageLogsResponse>(apiResponse.Data.ToString()!) is not GetUsageLogsResponse result)
                return null;


            List<ISeries<DateTimePoint>> spoolMeterLogs = [];

            // Group each log into their spool meter id
            foreach (var logGroup in result!.Logs.GroupBy(l => l.SpoolMeterId))
            {
                SpoolMeter? meter = MauiProgram.SpoolMeters.FirstOrDefault(s => s.ID == logGroup.First().SpoolMeterId);

                if (meter == null)
                    continue;

                var usageLogs = logGroup.ToList();
                usageLogs.Sort();

                SKColor color = new SKColor(
                    (byte)(meter.Color.Red * 255),
                    (byte)(meter.Color.Green * 255),
                    (byte)(meter.Color.Blue * 255),
                    (byte)(meter.Color.Alpha * 255)
                );

                spoolMeterLogs.Add(new LineSeries<DateTimePoint>()
                {
                    Values = new ObservableCollection<DateTimePoint>(logGroup.Select(l => new DateTimePoint(l.Time, l.RemainingAmountPercentage * 100))),
                    Stroke = new SolidColorPaint(color) { StrokeThickness = 3 },
                    Fill = null,
                    GeometrySize = 5,
                    GeometryStroke = new SolidColorPaint(color) { StrokeThickness = 3 },
                    GeometryFill = new SolidColorPaint(color),
                    Name = $"{meter.Name} - Predicted to run out on: {PredictSpoolRunOutDate(usageLogs).ToString("MMM dd yy")}"
                });
            }

            // Add corner points to make sure the correct x and y axis positions are showing
            spoolMeterLogs.Add(new ScatterSeries<DateTimePoint>()
            {
                Values = new ObservableCollection<DateTimePoint>()
                {
                    new DateTimePoint(DateTime.Now, 0),
                    new DateTimePoint(DateTimeOffset.Now.AddDays(-30).DateTime, 100)
                },
                GeometrySize = 0.000001,
                IsVisibleAtLegend = false
            });

            return spoolMeterLogs.ToArray();
        }



        /// <summary>
        /// Use existing usage logs to predict when the spool meter will run out using linear regression
        /// </summary>
        /// <param name="meterUsageLog">usage logs of a spool meter</param>
        /// <returns>Spool run out prediction date</returns>
        private static DateTime PredictSpoolRunOutDate(List<UsageLog> meterUsageLog)
        {
            List<List<UsageLog>> sessions = [];
            List<UsageLog> session = [];

            // Group each log into a session
            // Session being spool being used before it gets replaced
            for (int i = 0; i < meterUsageLog.Count; i++)
            {
                // End the session if the usage log indicates the spool meter is empty
                if (meterUsageLog[i].RemainingAmountPercentage == 0)
                {
                    session.Add(meterUsageLog[i]);
                    if (session.Count > 0)
                        sessions.Add(session);
                    session = [];
                    continue;
                }

                // If a backwards change of more than NEW_SESSION_PERCENTAGE or a reset to 100% then add to a new session
                if ((meterUsageLog[i].RemainingAmountPercentage == 1 && session.Count != 0) || (i > 0 && meterUsageLog[i].RemainingAmountPercentage + MauiProgram.NEW_SESSION_PERCENTAGE > meterUsageLog[i - 1].RemainingAmountPercentage))
                {
                    if (session.Count > 0)
                        sessions.Add(session);
                    session = [];
                }

                session.Add(meterUsageLog[i]);
            }

            // Dont add any empty sessions
            if (session.Count > 0)
                sessions.Add(session);

            List<Point> plotData = [];

            // Convert the sessions into a list (days from start of session, percentage left)
            foreach (var usageSession in sessions)
            {
                DateTime startDate = usageSession.First().Time;
                plotData.AddRange(usageSession.Select(u => new Point((u.Time - startDate).Days, u.RemainingAmountPercentage)));
            }


            // Get line of best fit using linear regression
            // Y = b0 + b1x
            double xMean = plotData.Average(p => p.X);
            double yMean = plotData.Average(p => p.Y);
            double b1Dividend = plotData.Sum(p => (p.X - xMean) * (p.Y - yMean));
            double b1Divisor = plotData.Sum(p => Math.Pow(p.X - xMean, 2));
            double b1 = b1Dividend / b1Divisor;
            double b0 = yMean - (b1 * xMean);


            // Now that line of best fit is found, find where the line interects with the x axis when y (remaining amount percentage) is 0
            // X = (y - b0) / b1 => y = 0 => X = -b0 / b1
            double predictedDaysUntilRunOut = -b0 / b1;

            // return the date that is average days until run out from the start of the current session
            return sessions.Last().First().Time.AddDays(predictedDaysUntilRunOut);
        }
    }
}
