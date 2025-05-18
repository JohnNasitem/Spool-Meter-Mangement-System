//***********************************************************************************
//Program: UsageGraphs.cs
//Description: Page displaying usage graphs
//Date: Mar 26, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************



using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Maui;
using Spool_Meter_Viewer.ViewModels;
using Spool_Meter_Viewer.Classes;
using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using System.Collections.Generic;

namespace Spool_Meter_Viewer;

public partial class UsageGraphs : ContentPage
{
    public UsageGraphs()
	{
		InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        SetUpGraph();
    }

    private async void SetUpGraph()
    {
        var viewModel = new UsageViewModel();

        var cartesianChart = new CartesianChart
        {
            Series = await viewModel.GetGraphData(),
            XAxes = viewModel.XAxes,
            YAxes = viewModel.YAxes,
            ZoomMode = LiveChartsCore.Measure.ZoomAndPanMode.Both,
            LegendPosition = LiveChartsCore.Measure.LegendPosition.Top
        };

        Content = cartesianChart;
    }
}