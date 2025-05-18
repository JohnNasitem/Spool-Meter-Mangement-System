namespace Spool_Meter_Viewer
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            UpdateNotifSettigns();
        }


        private async void UpdateNotifSettigns()
        {
            await MauiProgram.FetchAndUpdatetNotificationSettings();
        }
    }
}
