//***********************************************************************************
//Program: BluetoothPage.xaml.cs
//Description: Page for connecting to a spool meter over bluetooth
//Date: Mar 14, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************


using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.Exceptions;
using Spool_Meter_Viewer.Classes;
using Spool_Meter_Viewer.ServerResponses;

namespace Spool_Meter_Viewer;

public partial class BluetoothPage : ContentPage
{
    public ObservableCollection<Item> Items { get; set; }

    private readonly List<IDevice> _deviceList = [];
    private MainPage _mainPage;
 

    public BluetoothPage(MainPage mainPage)
	{
        InitializeComponent();
        SelectSpoolMeterButton.IsEnabled = false;
        Items = [];
        BindingContext = this;
        _mainPage = mainPage;

        try
        {
            ScanForDevices();
        }
        catch (Exception ex)
        {
            StatusLabel.IsVisible = true;
            Console.WriteLine(ex.ToString());
            StatusLabel.Text = ex.ToString();
        }
    }



    /// <summary>
    /// Scan for bluetooth devices
    /// </summary>
    private async void ScanForDevices()
    {
        await RequestPermissions();

        if (!MauiProgram.BluetoothLE!.IsOn)
        {
            await DisplayAlert("Error", "Bluetooth is turned off. Please enable it.", "OK");
            return;
        }

        // Clear any previous devices in the list
        _deviceList.Clear();
        Items.Clear();

        

        // Subscribe to device found event
        MauiProgram.BluetoothAdapter!.DeviceDiscovered += (s, a) =>
        {
            var device = a.Device;

            if (device.IsConnectable == false || device.Name == null || !device.Name.StartsWith("SpoolMeter"))
                return;

            // Ignore any already connected spool meters
            if (MauiProgram.SpoolMeters.Any(s => s.ID == device.Id.ToString()))
                return;

            _deviceList.Add(device);
            Console.WriteLine($"Discovered device: {device.Name} - {device.Id}");
            Items.Add(new Item { Name = $"{device.Name} - {device.Id}", Device = device});
        };
        // Start scanning
        try
        {
            await MauiProgram.BluetoothAdapter.StartScanningForDevicesAsync();

            // Give feedback that scanning is in progress
            Console.WriteLine("Scanning started...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting scan: {ex.Message}");
            await DisplayAlert("Error", $"Error starting scan: {ex.Message}", "OK");
        }
    }

    private static async Task StopScanning()
    {
        Console.WriteLine("Stop scanning for devices");
        await MauiProgram.BluetoothAdapter!.StopScanningForDevicesAsync();
    }



    /// <summary>
    /// CollectionView Item
    /// </summary>
    public struct Item
    {
        public string Name { get; set; }
        public IDevice Device { get; set; }
    }



    /// <summary>
    /// Connect to the selected spool meter
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void SelectSpoolMeterButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            MeterDetails detailsPage = new(_mainPage);
            await MauiProgram.BluetoothAdapter!.ConnectToDeviceAsync(MauiProgram.BluetoothConnectedDevice);
            await StopScanning();

            detailsPage.SpoolMeter = await GetSpoolMeterIfItExists(MauiProgram.BluetoothConnectedDevice!.Id.ToString());
            detailsPage.IsAdding = true;
            await Navigation.PushAsync(detailsPage);
        }
        catch (DeviceConnectionException ex)
        {
            Console.WriteLine($"SelectSpoolMeterButton_Clicked() - Couldn't not connect to device!. Exception: {ex}");
            await DisplayAlert("Bluetooth Error", "Unable to connect to spool meter! Make sure bluetooth is enabled, you are in range of the spool meter, and spool meter is turned on.", "OK");
        }
    }



    /// <summary>
    /// Update selected device
    /// </summary>
    /// <param name="sender">Object calling the method</param>
    /// <param name="e">Event args</param>
    private void CollectionView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection[0] is Item item)
        {
            MauiProgram.BluetoothConnectedDevice = item.Device;
            SelectSpoolMeterButton.IsEnabled = true;
        }
        else
        {
            SelectSpoolMeterButton.IsEnabled = false;
            MauiProgram.BluetoothConnectedDevice = null;
        }
    }


    /// <summary>
    /// Dynamically request bluetooth permissions
    /// </summary>
    /// <returns>true if permissions were granted, otherwise false</returns>
    private async Task<bool> RequestPermissions()
    {
        try
        {
            // Request Bluetooth permission (covers both BLUETOOTH_SCAN and BLUETOOTH_CONNECT)
            var bluetoothPermissionStatus = await Permissions.RequestAsync<Permissions.Bluetooth>();
            if (bluetoothPermissionStatus != PermissionStatus.Granted)
            {
                await DisplayAlert("Permission Denied", "Bluetooth permission is required.", "OK");
                return false;
            }

            // Proceed with Bluetooth functionality
            return true;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }

        return false;
    }



    /// <summary>
    /// Gets a spool meter from the server if the id is valid
    /// </summary>
    /// <param name="spoolMeterId">id of spool meter to get</param>
    /// <returns>Spool meter if the id exists, otherwise null</returns>
    private async Task<SpoolMeter?> GetSpoolMeterIfItExists(string spoolMeterId)
    {
        // Send GET request to the server with the users token
        HttpResponseMessage? response = await MauiProgram.MakeApiCall(MauiProgram.RequestType.GET, $"/Api/GetSpoolMeter/{MauiProgram.AccessToken}/{spoolMeterId}", null);
        
        if (response == null)
            return null;

        try
        {
            //Parse server response
            var content = await response.Content.ReadAsStringAsync();
            if (JsonConvert.DeserializeObject<ApiResponse>(content) is not ApiResponse apiResponse)
                return null;
            var getSpoolMeterResponse = JsonConvert.DeserializeObject<GetSpoolMeterResponse>(apiResponse.Data.ToString()!);

            if (getSpoolMeterResponse == null)
                return null;

            return getSpoolMeterResponse.SpoolMeter;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Problem with parsing response in GetSpoolMeterIfItExists() - Exception: {ex}");
            return null;
        }
    }
}