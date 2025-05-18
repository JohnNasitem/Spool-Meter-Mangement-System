//***********************************************************************************
//Program: MeterDeatils.xaml.cs
//Description: Spool Meter Details page
//Date: Feb 10, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************

// TODO: make a colour pallet window to select default colour pallet but allow allow user type a hex code
// TODO: MauiProgram.BluetoothConnectedDevice is not being set when card is tapped

using System.Diagnostics.Metrics;
using System.Text.RegularExpressions;
using Microsoft.Maui;
using Newtonsoft.Json;
using Spool_Meter_Viewer.Classes;
using Spool_Meter_Viewer.ServerResponses;
using System.Text;
using Plugin.BLE.Abstractions;
using System.Reflection.PortableExecutable;
using Plugin.BLE.Abstractions.Contracts;
using CommunityToolkit.Maui.Views;
using Spool_Meter_Viewer.Popups;

namespace Spool_Meter_Viewer;

public partial class MeterDetails : ContentPage
{
    // Settings
    // Make sure to make these the same with the database
    private const int MAX_NAME_LENGTH = 64;

    // Current spool meter connected
	private SpoolMeter _spoolMeter;
    // Spool meter holding the editted changes
	private SpoolMeter _tempMeter;
    // Spool meter page
    private readonly MainPage _mainPage;
    // Flag to check if spool meter is being added or editted
    private bool _isAdding;

    /// <summary>
    /// Spool meter being editted<br/>
    /// Pass in a spool meter instance of editting, null for adding a new one
    /// </summary>
    public SpoolMeter? SpoolMeter
	{
		get
		{
			return _spoolMeter;
        }
		set
		{
            if (value == null)
            {
                _isAdding = true;
                _spoolMeter = new SpoolMeter();
                _tempMeter = new SpoolMeter();
                RemoveMeterButton.IsVisible = false;
                _tempMeter.ID = MauiProgram.BluetoothConnectedDevice!.Id.ToString();
                _spoolMeter.ID = MauiProgram.BluetoothConnectedDevice!.Id.ToString();
            }
            else
            {
                _isAdding = false;
                _spoolMeter = value;
                _tempMeter = new SpoolMeter(_spoolMeter);
            }

            SetMeter();
        }
	}


    public bool IsAdding
    {
        set
        {
            _isAdding = value;
            SaveChangesButton.Text = "Add Meter";
            SaveChangesButton.IsEnabled = true;
            RemoveMeterButton.IsVisible = false;
        }
    }



    /// <summary>
    /// Material Types
    /// </summary>
    public List<MaterialType> Materials { get; set; }



    /// <summary>
    /// Initializes a new instance of the <see cref="MeterDetails"/> class.
    /// </summary>
    /// <param name="mainPage">Spool meter page</param>
    public MeterDetails(MainPage mainPage)
	{
		InitializeComponent();
        if (_isAdding)
            SaveChangesButton.IsEnabled = false;
        _mainPage = mainPage;
        Materials = MauiProgram.Materials;
    }



    /// <summary>
    /// Save the changes made to the spool meter
    /// </summary>
    /// <param name="sender">Object calling the method</param>
    /// <param name="e">Event args</param>
    private async void SaveChanges_Clicked(object sender, EventArgs e)
    {
        IsBusy = true;
        //Make sure all fields are valid before saving
        MeterNameEntry.Unfocus();
        RemainingAmountEntry.Unfocus();
        OriginalAmountEntry.Unfocus();
        MaterialTypePicker.Unfocus();
        UpdateButtonState();
        if (!SaveChangesButton.IsEnabled)
            return;

        if(await SyncDeviceAndServer())
        {
            IsBusy = false;
            await Navigation.PopToRootAsync();
            return;
        }

        IsBusy = false;
    }



    /// <summary>
    /// Allow users to open the meter details page again
    /// and update the spool meters
    /// </summary>
    protected async override void OnDisappearing()
    {
        base.OnDisappearing();
        _mainPage.PageAlreadyOpenned = false;
        await _mainPage.RefreshSpoolMeters();
    }



    #region InputValidation
    /// <summary>
    /// Save changes made to the name to the temp spool meter
    /// </summary>
    /// <param name="sender">Object calling the method</param>
    /// <param name="e">Event args</param>
    private void MeterNameEntry_Unfocused(object sender, FocusEventArgs e)
    {
        Entry? entry = IsValidEntryInput(sender);

        if (entry == null)
        {
            SaveChangesButton.IsEnabled = false;
            return;
        }

        if (entry.Text.Length > MAX_NAME_LENGTH)
            entry.Text = entry.Text.Substring(0, MAX_NAME_LENGTH);
        _tempMeter.Name = entry.Text;
        UpdateButtonState();
    }



    /// <summary>
    /// Save the changes made to the material types to the temp spool meter
    /// </summary>
    /// <param name="sender">Object calling the method</param>
    /// <param name="e">Event args</param>
    private void MaterialTypePicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (sender is not Picker picker || picker.Items.Count == 0)
            return;

        _tempMeter.MaterialTypeUsed = Materials[picker.SelectedIndex];

        UpdateButtonState();
    }



    /// <summary>
    /// Save the changes made to the original amount to the temp spool meter
    /// </summary>
    /// <param name="sender">Object calling the method</param>
    /// <param name="e">Event args</param>
    private async void OriginalAmountEntry_Unfocused(object sender, FocusEventArgs e)
    {
        Entry? entry = IsValidEntryInput(sender);

        if (entry == null)
        {
            SaveChangesButton.IsEnabled = false;
            return;
        }

        double newAmount = SpoolMeter.UnitsToNumber(entry.Text, _tempMeter.MaterialTypeUsed.MeasurementScale);

        if (newAmount < 0)
        {
            entry.Text = "";
            await DisplayAlert("Invalid Amount", "You entered an invalid amount!", "OK");
        }
        else
        {
            _tempMeter.OrginalAmount = newAmount;
            entry.Text = SpoolMeter.NumberToUnits(newAmount, _tempMeter.MaterialTypeUsed.MeasurementScale);

            if ((RemainingAmountEntry.Text == null || RemainingAmountEntry.Text.Length == 0 || RemainingAmountEntry.Text == "0 cm") && _isAdding)
            {
                _tempMeter.RemainingAmount = newAmount;
                RemainingAmountEntry.Text = entry.Text;
            }
        }

        UpdateButtonState();
    }



    /// <summary>
    /// Save the changes made to the remaining amount made to the temp spool meter
    /// </summary>
    /// <param name="sender">Object calling the method</param>
    /// <param name="e">Event args</param>
    private async void RemainingAmountEntry_Unfocused(object sender, FocusEventArgs e)
    {
        Entry? entry = IsValidEntryInput(sender);

        if (entry == null)
        {
            SaveChangesButton.IsEnabled = false;
            return;
        }

        // Try and convert input into a valid number
        double newAmount = SpoolMeter.UnitsToNumber(entry.Text, _tempMeter.MaterialTypeUsed.MeasurementScale);

        if (newAmount < 0)
        {
            entry.Text = "";
            await DisplayAlert("Invalid Amount", "You entered an invalid amount! Must be a positive number", "OK");
        }
        else if (newAmount > _tempMeter.OrginalAmount)
        {
            entry.Text = "";
            await DisplayAlert("Invalid Amount", "The remaining amount cannot be greater than the original amount", "OK");
        }
        else
        {
            _tempMeter.RemainingAmount = newAmount;
            entry.Text = SpoolMeter.NumberToUnits(newAmount, _tempMeter.MaterialTypeUsed.MeasurementScale);
        }

        UpdateButtonState();
    }



    /// <summary>
    /// Save changes made to the colour to the temp spool meter
    /// </summary>
    /// <param name="sender">Object calling the method</param>
    /// <param name="e">Event args</param>
    private async void ColourEntry_Unfocused(object sender, FocusEventArgs e)
    {
        Entry? entry = IsValidEntryInput(sender);

        if (entry == null)
        {
            SaveChangesButton.IsEnabled = false;
            return;
        }

        string colString = ConvertHexString(entry.Text);
        entry.Text = colString;
        try
        {
            _tempMeter.Color = Color.FromArgb(colString);
        }
        catch
        {
            _tempMeter.Color = Color.FromArgb("#000000");
            ColourPreview.BackgroundColor = _tempMeter.Color;
            ColourEntry.Text = "";
            await DisplayAlert("Invalid Color Format", $"The specified color {colString} is invalid! Must be in this format #FFFFFF", "OK");
            SaveChangesButton.IsEnabled = false;
            return;
        }
        ColourPreview.BackgroundColor = _tempMeter.Color;
        UpdateButtonState();

    }



    /// <summary>
    /// Update button on text change
    /// </summary>
    /// <param name="sender">Object calling the method</param>
    /// <param name="e">Event args</param>
    private void TextChange(object sender, TextChangedEventArgs e)
    {
        UpdateButtonState();
    }
    #endregion



    #region HelperMethods
    /// <summary>
    /// Make sure object is an Entry and has text inside it
    /// </summary>
    /// <param name="obj">object to check</param>
    /// <returns>Entry if obj is valid, otherwise null</returns>
    private static Entry? IsValidEntryInput(object obj)
    {
        if (obj is not Entry entry)
            return null;

        if (entry.Text == null || entry.Text.Length == 0)
            return null;

        return entry;
    }



    /// <summary>
    /// Disable save button if any of the fields are empty
    /// </summary>
    private void UpdateButtonState()
    {
        bool enableButton = true;

        foreach (var child in MeterDetailsContainer.Children)
        {
            if (child is Entry entry && (entry.Text == "" || entry.Text == null))
                enableButton = false;
            if (child is Picker picker && picker.SelectedIndex == -1)
                enableButton = false;
        }

        SaveChangesButton.IsEnabled = enableButton;
    }



    /// <summary>
    /// Populate all the fields using current spool meter
    /// </summary>
	private async void SetMeter()
    {
        MaterialTypePicker.Items.Clear();
        foreach (MaterialType materialType in Materials)
            MaterialTypePicker.Items.Add(materialType.Name);

        if (_isAdding)
        {
            SaveChangesButton.Text = "Add Meter";
        }
        else
        {
            MeterNameEntry.Text = _spoolMeter.Name;
            RemainingAmountEntry.Text = SpoolMeter.NumberToUnits(_spoolMeter.RemainingAmount, _spoolMeter.MaterialTypeUsed.MeasurementScale);
            OriginalAmountEntry.Text = SpoolMeter.NumberToUnits(_spoolMeter.OrginalAmount, _spoolMeter.MaterialTypeUsed.MeasurementScale);
            MaterialTypePicker.SelectedItem = _spoolMeter.MaterialTypeUsed.Name;
            ColourEntry.Text = _spoolMeter.Color.ToHex();
            ColourPreview.BackgroundColor = _spoolMeter.Color;
            SaveChangesButton.Text = "Save changes";
            SaveChangesButton.IsEnabled = true;
            try
            {
                MauiProgram.BluetoothConnectedDevice = await MauiProgram.BluetoothAdapter!.ConnectToKnownDeviceAsync(new Guid(_spoolMeter.ID));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SetMeter() - Couldn't connect to the spool meter ({_spoolMeter.ID})!. Exception {ex}");
                await DisplayAlert("Connection Error", "Couldn't connect to the spool meter! Make sure you have bluetooth enabled and close enough to the device.", "OK");
                await Navigation.PopToRootAsync();
            }
        }
    }



    /// <summary>
    /// Format string into a hex code format
    /// </summary>
    /// <param name="input">input string to parse</param>
    /// <returns>Hex code string if parse was valid, otherwise empty string</returns>
    private static string ConvertHexString(string input)
    {
        Match match = Regex.Match(input, @"^#?(([A-Za-z0-9]){1,6})$");

        if (match.Success)
            return '#' + match.Groups[1].Value.ToUpper().PadRight(6, '0');
        else
            return string.Empty;
    }
    #endregion



    /// <summary>
    /// Sync spool meter info to the server and the device
    /// </summary>
    /// <returns></returns>
    private async Task<bool> SyncDeviceAndServer()
    {

        // Data used to update the server
        object data = new
        {
            MauiProgram.AccessToken,
            _tempMeter.Name,
            SpoolMeterId = _tempMeter.ID,
            RemainingAmount = _tempMeter.RemainingAmount.ToString(),
            OriginalAmount = _tempMeter.OrginalAmount.ToString(),
            MaterialId = _tempMeter.MaterialTypeUsed.ID.ToString(),
            ColorHex = _tempMeter.Color.ToHex(),
            BatteryStatus = ((byte)_tempMeter.BatteryStatus).ToString()
        };

        HttpResponseMessage? response;
        // Update/Add spool meter database entry
        if (_isAdding)
            response = await MauiProgram.MakeApiCall(MauiProgram.RequestType.POST, "/Api/AddSpoolMeter", data);
        else
            response = await MauiProgram.MakeApiCall(MauiProgram.RequestType.PUT, "/Api/UpdateSpoolMeter", data);

        if (response == null)
            return false;

        //Parse server response
        var content = await response.Content.ReadAsStringAsync();
        if (JsonConvert.DeserializeObject<ApiResponse>(content) is not ApiResponse apiResponse)
            return false;

        string serverMessage = string.Empty;

        // Parse server response
        if (_isAdding)
        {
            var responseData = JsonConvert.DeserializeObject<AddSpoolMeterResponse>(apiResponse.Data.ToString()!);

            if (responseData != null)
            {
                _tempMeter.Password = responseData.Password;
                serverMessage = responseData.Message;
            }
        }
        else
        {
            var responseData = JsonConvert.DeserializeObject<BasicResponse>(apiResponse.Data.ToString()!);

            if (responseData != null)
                serverMessage = responseData.Message;
        }

        if (!await SendBluetoothUpdatePacket())
        {
            await DisplayAlert("Bluetooth Error", "Problem occured when trying to connect to the spool meter through bluetooth. Make sure you have bluetooth enabled!", "OK");
            return false;
        }

        if (apiResponse.StatusCode == 200)
            return true;
        else if (apiResponse.StatusCode == 400)
            await DisplayAlert("Invalid Inputs", $"One or more of the inputs are invalid!.{(serverMessage != string.Empty ? $"Server message: {serverMessage}" : "")}", "OK");
        else if (apiResponse.StatusCode == 400)
            await DisplayAlert("Server Error", "Problem occured on the server side when trying to access the database. Please try again later", "OK");

        return false;
    }



    /// <summary>
    /// Send the bluetooth packet to the connected device
    /// </summary>
    /// <returns>true if operation was successful, otherwise false</returns>
    private async Task<bool> SendBluetoothUpdatePacket()
    {
        if (MauiProgram.BluetoothConnectedDevice == null)
            return false;

        // Discover services
        var services = await MauiProgram.BluetoothConnectedDevice.GetServicesAsync();
        ICharacteristic? writeCharacteristic = null;
        IReadOnlyList<ICharacteristic>? characteristics = null;
        foreach (var service in services)
        {
            // Discover characteristics for each service
            characteristics = await service.GetCharacteristicsAsync();
        }

        if (characteristics == null)
            return false;

        writeCharacteristic = characteristics.FirstOrDefault(c => c.Properties.HasFlag(CharacteristicPropertyType.Write));

        if (writeCharacteristic == null)
            return false;

        // Prepare the message
        var jsonString = JsonConvert.SerializeObject(new
        {
            MauiProgram.BluetoothConnectedDevice.Id,
            _tempMeter.Password,
            _tempMeter.Name,
            RemainingAmount = _tempMeter.RemainingAmount.ToString(),
            OriginalAmount = _tempMeter.OrginalAmount.ToString(),
            MaterialName = _tempMeter.MaterialTypeUsed.Name,
            MaterialDensity = _tempMeter.MaterialTypeUsed.Density.ToString(),
            MaterialDiameter = _tempMeter.MaterialTypeUsed.Diameter.ToString(),
            ColorHex = _tempMeter.Color.ToHex(),
            BatteryStatus = ((byte)_tempMeter.BatteryStatus).ToString()
        });

        try
        {
            // Write to the characteristic
            await writeCharacteristic.WriteAsync(Encoding.UTF8.GetBytes(jsonString));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SendBluetoothUpdatePacket() - Failed to write to the device. Exception: {ex}");
            return false;
        }

        bool disconnected;
        int disconnectAttempts = 0;

        do
        {
            disconnected = true;

            // Return false if couldn't disconnect within 5 tries
            if (++disconnectAttempts == 5)
                return false;

            try
            {
                // Disconnect from device
                await MauiProgram.BluetoothAdapter!.DisconnectDeviceAsync(MauiProgram.BluetoothConnectedDevice);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SendBluetoothUpdatePacket() - Failed to disconnect. Trying again. Exception: {ex}");
                disconnected = true;
            }
        }
        while (!disconnected);
        
        MauiProgram.BluetoothConnectedDevice = null;

        return true;
    }



    /// <summary>
    /// Sends request to unbind spool meter from account
    /// </summary>
    /// <param name="sender">Object calling the method</param>
    /// <param name="e">Event args</param>
    private void RemoveMeterButton_Clicked(object sender, EventArgs e)
    {
        // Display yes no pop up
        var popup = new YesNoPopup(new Action(async () =>
        {
            // If account was deleted then go back to log in page
            // Send GET request to the server with the users token
            if (await MauiProgram.MakeApiCall(MauiProgram.RequestType.DELETE, $"/Api/RemoveSpoolMeterFromAccount/{MauiProgram.AccessToken}/{_spoolMeter.ID}", null) is not HttpResponseMessage response)
                return;

            //Parse server response
            var content = await response.Content.ReadAsStringAsync();
            if (JsonConvert.DeserializeObject<ApiResponse>(content) is not ApiResponse apiResponse)
            {
                await DisplayAlert("Server Error", "Problem occured on the server side! Please try again later.", "OK");
                return;
            }

            var basicResponse = JsonConvert.DeserializeObject<BasicResponse>(apiResponse!.Data.ToString()!);

            if (apiResponse.StatusCode != 200)
            {
                await DisplayAlert("Server Error", $"Problem occured on the server side! Please try again later. Error: {basicResponse!.Message}", "OK");
                return;
            }

            if (MauiProgram.BluetoothConnectedDevice is not null)
                await MauiProgram.BluetoothAdapter!.DisconnectDeviceAsync(MauiProgram.BluetoothConnectedDevice);
            await Navigation.PopToRootAsync();
        }), "Are you sure you want to remove the spool meter?", null, Colors.Red);
        this.ShowPopup(popup);
    }
}