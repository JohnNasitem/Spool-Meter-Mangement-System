//***********************************************************************************
//Program: App.cs
//Description: Application
//Date: Mar 12, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************



using Newtonsoft.Json;
using Spool_Meter_Viewer.ServerResponses;
using System.Text;


namespace Spool_Meter_Viewer
{
    public partial class App : Application
    {

        public App()
        {
            InitializeComponent();

            MainPage = new LoginPage();

            DetermineSession();
        }



        /// <summary>
        /// Determine if the user has an active session
        /// </summary>
        private async void DetermineSession()
        {
            bool isRefreshTokenValid = await MauiProgram.RefreshTokens();

            if (isRefreshTokenValid)
                MainPage = new AppShell();
        }
    }
}
