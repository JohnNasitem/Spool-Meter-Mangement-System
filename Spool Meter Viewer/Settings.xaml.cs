using Spool_Meter_Viewer.OtherPages.SettingsPages;

namespace Spool_Meter_Viewer;

public partial class Settings : ContentPage
{
	public Settings()
	{
		InitializeComponent();
	}

    private async void AccountSettings_Tapped(object sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new AccountSettingsPage());
    }

    private async void NotificationSettings_Tapped(object sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new NotificationSettingsPage());
    }
}