//***********************************************************************************
//Program: SpoolMeter.cs
//Description: SpoolMeter class
//Date: Feb 10, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************



using Microsoft.Maui.Devices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Spool_Meter_Viewer.Classes
{
    public class SpoolMeter
    {
        //Weight units to scale
        private static readonly Dictionary<string, double> _weightUnitToScale = new()
        {
            { "g", 1 },
            { "kg", 1_000 },
            { "t", 1_000_000 },
        };
        //Length units to scale
        private static readonly Dictionary<string, double> _lengthUnitToScale = new()
        {
            { "cm", 1 },
            { "m", 100 },
            { "km", 100_000 },
        };



        /// <summary>
        /// Spool metere id
        /// </summary>
        [JsonProperty("id")]
        public string ID { get; set; }



        /// <summary>
        /// Spool meter name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }



        /// <summary>
        /// Spool meter password
        /// </summary>
        [JsonProperty("password")]
        public string Password { get; set; }



        /// <summary>
        /// Remaining amount in the spool
        /// </summary>
        [JsonProperty("remainingAmount")]
        public double RemainingAmount { get; set; }



        /// <summary>
        /// Original spool meter amount
        /// </summary>
        [JsonProperty("originalAmount")]
        public double OrginalAmount { get; set; }



        /// <summary>
        /// Battery status of the spool meter
        /// </summary>
        public BatteryStatus BatteryStatus { get; set; }



        /// <summary>
        /// Material of the spool
        /// </summary>
        public MaterialType MaterialTypeUsed { get; set; }



        /// <summary>
        /// Backcolor of the spool meter view
        /// </summary>
        public Color Color { get; set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="SpoolMeter"/> class.
        /// </summary>
        public SpoolMeter()
        {
            ID = "";
            Name = "";
            Password = "";
            RemainingAmount = 0;
            BatteryStatus = 0;
            MaterialTypeUsed = new MaterialType();
            OrginalAmount = 0;
            Color = Colors.White;
        }



        /// <summary>
        /// Initializes a new instance of the <see cref="SpoolMeter"/> class.
        /// </summary>
        /// <param name="name">Spool meter name</param>
        /// <param name="remainingAmount">Remaining amount in the spool</param>
        /// <param name="originalAmount">Original amount in the spool</param>
        /// <param name="batteryStatus">Battery level of the spool meter</param>
        /// <param name="materialType">Material in the spool</param>
        /// <param name="color">Color of the backcolor of the spool meter view</param>
        public SpoolMeter(string name, double remainingAmount, double originalAmount, BatteryStatus batteryStatus, MaterialType materialType, Color color)
        {
            if (originalAmount < 0 || remainingAmount < 0)
                throw new ArgumentException("Remaining and original amount cannot be negative");
            if (remainingAmount > originalAmount)
                throw new ArgumentException("Remaining material cannot be greater than original amount");

            Name = name;
            RemainingAmount = remainingAmount;
            BatteryStatus = batteryStatus;
            MaterialTypeUsed = materialType;
            OrginalAmount = originalAmount;
            Color = color;
        }



        /// <summary>
        /// Initializes a new instance of the <see cref="SpoolMeter"/> class.
        /// </summary>
        /// <param name="id">spool meter id</param>
        /// <param name="name">spool meter name</param>
        /// <param name="password">spool meter password</param>
        /// <param name="remainingAmount">remaining amount of material left in the spool meter</param>
        /// <param name="originalAmount">original amount of material in the spool meter</param>
        /// <param name="batteryStatus">battery status of the spool meter</param>
        /// <param name="materialTypeId">material type id that is in the spool meter</param>
        /// <param name="color">ui colour of of the spool meter</param>
        [JsonConstructor]
        public SpoolMeter(string id, string name, string password, double remainingAmount, double originalAmount, int batteryStatus, int materialTypeId, string color)
        {
            ID = id;
            Name = name;
            Password = password;
            RemainingAmount = remainingAmount;
            BatteryStatus = (BatteryStatus)batteryStatus;
            OrginalAmount = originalAmount;
            MaterialTypeUsed = MauiProgram.Materials.FirstOrDefault(m => m.ID == materialTypeId);
            Color = Color.Parse(color);
        }



        /// <summary>
        /// Initializes a new instance of the <see cref="SpoolMeter"/> class.
        /// </summary>
        /// <param name="meter">Spool meter to copy</param>
        public SpoolMeter(SpoolMeter meter)
        {
            ID = meter.ID;
            Name = meter.Name;
            Password = meter.Password;
            RemainingAmount = meter.RemainingAmount;
            BatteryStatus = meter.BatteryStatus;
            MaterialTypeUsed = meter.MaterialTypeUsed;
            OrginalAmount = meter.OrginalAmount;
            Color = Color.FromArgb(meter.Color.ToHex());
        }



        /// <summary>
        /// Convert amount in grams to its largest unit
        /// </summary>
        /// <param name="amount">Amount to convert</param>
        /// <param name="unitType">Measurement type</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Throw a exception if the MaterialMeasurement hasnt been implemented yet</exception>
        public static string NumberToUnits(double amount, MaterialMeasurement unitType)
        {
            string key;
            switch (unitType)
            {
                case MaterialMeasurement.Weight:
                    if (amount == 0)
                        return "0 g";
                    key = _weightUnitToScale.Last(x => x.Value <= amount).Key;
                    return amount < 1000 ? $"{amount} g" : $"{amount / _weightUnitToScale[key]:F3} {key}";
                case MaterialMeasurement.Length:
                    if (amount == 0)
                        return "0 cm";
                    key = _lengthUnitToScale.Last(x => x.Value <= amount).Key;
                    return amount < 1000 ? $"{amount} cm" : $"{amount / _lengthUnitToScale[key]:F3} {key}";
                default:
                    throw new ArgumentException("Invalid unit type");
            }
        }



        /// <summary>
        /// Convert unitted number into the smallest unit
        /// </summary>
        /// <param name="amount">Amount to convert</param>
        /// <param name="unitType">Measurement type</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Throw a exception if the MaterialMeasurement hasnt been implemented yet</exception>
        public static double UnitsToNumber(string amount, MaterialMeasurement unitType)
        {
            //Default with weight
            Match match = Match.Empty;
            Dictionary<string, double> unitToMultiplier;
            string defaultUnit;
            switch (unitType)
            {
                case MaterialMeasurement.Weight:
                    match = Regex.Match(amount, @"^(\d+(\.\d+)?)\s*(g|kg|t)?$");
                    unitToMultiplier = _weightUnitToScale;
                    defaultUnit = "g";
                    break;
                case MaterialMeasurement.Length:
                    match = Regex.Match(amount, @"^(\d+(\.\d+)?)\s*(cm|m|km)?$");
                    unitToMultiplier = _lengthUnitToScale;
                    defaultUnit = "cm";
                    break;
                default:
                    throw new ArgumentException("Invalid unit type");
            }

            if (!match.Success)
                return -1;

            return double.Parse(match.Groups[1].Value) * unitToMultiplier[match.Groups[3].Success ? match.Groups[3].Value.ToLower() : defaultUnit];

        }
    }



    /// <summary>
    /// Battery status
    /// </summary>
    public enum BatteryStatus
    {
        Full,
        High,
        Half,
        Low,
        Dead,
    }
}
