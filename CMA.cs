using System;
using System.Drawing;
using TradingPlatform.BusinessLayer; // Ensure correct using directive

namespace QuanTAlib;

public class ColorfulMovingAverages : Indicator // Inherits from Indicator
{
    #region Parameters

    [InputParameter("T3 Volume Factor", 0, 0.1, 1.0, 0.1)]
    private double T3VolumeFactor { get; set; } = 0.7; // Default is 0.7

    // Define input parameters, fields, and methods here
    [InputParameter("Moving Average Type", 1, variants: new object[]
    {
        "T3", 0,
        "SMA", 1,
        "EMA", 2,
        "RMA", 3,
        "WMA", 4,
        "VWMA", 5,
        "LSMA", 6,
        "HMA", 7
    })]
    private int MovingAverageType { get; set; } = 0;

    [InputParameter("Period", 2, 1, 100, 1)]
    private int Period { get; set; } = 8; // Default is 8

    [InputParameter("Source", 3, variants: new object[]
    {
        "Close", 0,
        "Open", 1,
        "High", 2,
        "Low", 3
    })]
    private int Source { get; set; } = 0; // Default is Close = 0

    #endregion Parameters

    private TBars bars;
    protected HistoricalData History;
    private TSeries MovingAverageSeries;

    private double previousMAValue = 0;
    // Assuming you have a field to store the previous delta value
    private double previousDelta = 0;
    private double previousGamma = 0;
    /// 

    /// 

    public ColorfulMovingAverages() : base()
    {

        // Constructor logic here
        Name = "Colorful Moving Average";
        Description = "A colorful momentum indicator.";
        AddLineSeries("MA Line", Color.CadetBlue, 2, LineStyle.Solid); // Initialize the line series with a default line width and color
        SeparateWindow = false;
    }

    protected override void OnInit()
    {   
        
        bars = new TBars();
        History = Symbol.GetHistory(period: HistoricalData.Period, fromTime: HistoricalData.FromTime);
        for (int i = History.Count - 1; i >= 0; i--)
        {
            var rec = History[i, SeekOriginHistory.Begin];
            bars.Add(rec.TimeLeft, rec[PriceType.Open],
            rec[PriceType.High], rec[PriceType.Low],
            rec[PriceType.Close], rec[PriceType.Volume]);
        }

        // Example: Initialize a specific type of moving average based on user input
        switch (MovingAverageType)
        {
            case 0: // T3
                MovingAverageSeries = new T3_Series(source: bars.Select(Source), period: Period, vfactor: T3VolumeFactor, useNaN: false);
                Name += $"T3";
                break;
            case 1: // SMA
                MovingAverageSeries = new SMA_Series(source: bars.Select(Source), period: Period, useNaN: false);
                Name += $"SMA";
                break;
            case 2: // EMA
                MovingAverageSeries = new EMA_Series(source: bars.Select(Source), period: Period, useNaN: false);
                Name += $"EMA";
                break;
            case 3: // RMA
                MovingAverageSeries = new RMA_Series(source: bars.Select(Source), period: Period, useNaN: false);
                Name += $"RMA";
                break;
            case 4: // WMA
                MovingAverageSeries = new WMA_Series(source: bars.Select(Source), period: Period, useNaN: false);
                Name += $"WMA";
                break;
            case 5: // VWMA - not finished
                MovingAverageSeries = new SMA_Series(source: bars.Select(Source), period: Period, useNaN: false);
                Name += $"VWMA";
                break;
            case 6: // LSMA - not finished
                MovingAverageSeries = new SMA_Series(source: bars.Select(Source), period: Period, useNaN: false);
                Name += $"LSMA";
                break;
            case 7: // RMA
                MovingAverageSeries = new HMA_Series(source: bars.Select(Source), period: Period, useNaN: false);
                Name += $"HMA";
                break;
                // Add cases for other types of moving averages
        }

        // Additional setup specific to the indicator (if any)
    }

    protected override void OnUpdate(UpdateArgs args)
    {   
        
        bool update = !(args.Reason == UpdateReason.NewBar || args.Reason == UpdateReason.HistoricalBar);
        bars.Add(Time(), GetPrice(PriceType.Open),
                                GetPrice(PriceType.High),
                                GetPrice(PriceType.Low),
                                GetPrice(PriceType.Close),
                                GetPrice(PriceType.Volume), update);

        // Update the moving average series
        if (MovingAverageSeries != null)
        {
            MovingAverageSeries.Add(bars.Last);
        }

        // Calculate Momentum (Δ) and Acceleration (Γ)
        double currentMAValue = MovingAverageSeries.Last.v;
        double delta = currentMAValue - previousMAValue; // Momentum (Δ)
        double gamma = delta - previousDelta; // Acceleration (Γ)

        // Normalize Δ and Γ values between -100 and 100
        double normalizedDelta = NormalizeValue(delta);
        double normalizedGamma = NormalizeValue(gamma);

        // Calculate color based on delta
        double f = 2.55 * normalizedDelta / 2 + 50 * 2.55;
        Color lineColor = Color.FromArgb(Math.Min(510 - (int)f, 255), Math.Min((int)f, 255), 0);

        // Update the moving average line with new color and value
        LinesSeries[0].Color = lineColor;
        SetValue(currentMAValue, 0);

        // Check for Buy/Sell Signals
        if (normalizedDelta > 0 && previousDelta <= 0)
        {
            // Plot buy signal (example: green diamond below bar)
            LinesSeries[0].SetMarker(0, new IndicatorLineMarker(Color.Green, bottomIcon: IndicatorLineMarkerIconType.UpArrow));
        }
        else if (normalizedDelta < 0 && previousDelta >= 0)
        {
            // Plot sell signal (example: red diamond above bar)
            LinesSeries[0].SetMarker(0, new IndicatorLineMarker(Color.Red, upperIcon: IndicatorLineMarkerIconType.DownArrow));
        }

        // Update previous values for the next iteration
        previousMAValue = currentMAValue;
        previousDelta = delta;
        previousGamma = gamma;

    }

    public override void OnPaintChart(PaintChartEventArgs args)
    {
        if (this.CurrentChart == null)
        {
            return;
        }

        Graphics gr = args.Graphics;
        

        double g = previousGamma;
        double d = previousDelta;

        // Example coordinates for delta and gamma display
        int deltaPosX = 100; // X coordinate for delta
        int deltaPosY = 50;  // Y coordinate for delta
        int gammaPosX = 200; // X coordinate for gamma
        int gammaPosY = 50;  // Y coordinate for gamma

        // Draw Delta (Δ) and Gamma (Γ) labels at the specified positions
        gr.DrawString("\u0394", new Font("Arial", 16, FontStyle.Bold), Brushes.Black, new PointF(deltaPosX, deltaPosY));
        gr.DrawString("\u0393", new Font("Arial", 16, FontStyle.Bold), Brushes.Black, new PointF(gammaPosX, gammaPosY));

        // Calculate Delta and Gamma values
        double roundedDelta = Math.Round(previousDelta * 100) / 100;
        double roundedGamma = Math.Round(previousGamma * 100) / 100;

        // Determine the text color and background color based on values
        Brush deltaTextColor = d < 0 ? Brushes.White : Brushes.Black;
        Brush deltaBgColor = d > 0 ? Brushes.Green : Brushes.Red;
        Brush gammaTextColor = g < 0 ? Brushes.White : Brushes.Black;
        Brush gammaBgColor = g > 0 ? Brushes.Green : Brushes.Red;

        // Draw Delta and Gamma values at the specified positions
        gr.DrawString("\u0394: " + roundedDelta.ToString(), new Font("Arial", 12), deltaTextColor, new PointF(deltaPosX + 20, deltaPosY));
        gr.DrawString("\u0393: " + roundedGamma.ToString(), new Font("Arial", 12), gammaTextColor, new PointF(gammaPosX + 20, gammaPosY));

        // Draw colored backgrounds for Delta and Gamma values
        gr.FillRectangle(deltaBgColor, deltaPosX - 5, deltaPosY - 5, 40, 20);
        gr.FillRectangle(gammaBgColor, gammaPosX - 5, gammaPosY - 5, 40, 20);

        // Rest of your code...
    }



    // Helper method to normalize values
    private double NormalizeValue(double value)
    {
        // Example normalization logic
        // Adjust the contrast factor as needed
        double contrast = 1; // Adjust this based on your requirements

        double normalized = 100 - 200 / (Math.Pow(1 + contrast / Math.Abs(value), value) + 1);
        return normalized;
    }



}
