//***********************************************************************************
//Program: MaterialType.cs
//Description: Material type that can be used in the Spool Meter
//Date: Mar 12, 2025
//Author: John Nasitem
//Course: CMPE2965
//Class: CNTA01
//***********************************************************************************



using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spool_Meter_Viewer.Classes
{
    public class MaterialType

    {
        /// <summary>
        /// Material type ID in the database
        /// </summary>
        [JsonProperty("id")]
        public int ID { get; set; }



        /// <summary>
        /// Material type name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }



        /// <summary>
        /// Diameter of the material, only applicable if MeasurementScale is weight
        /// </summary>
        [JsonProperty("diameter")]
        public double Diameter { get; set; }



        /// <summary>
        /// Density of the material, only applicable if MeasurementScale is weight
        /// </summary>
        [JsonProperty("density")]
        public double Density { get; set; }
        public MaterialMeasurement MeasurementScale { get; set; }



        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialType"/>
        /// </summary>
        public MaterialType()
        {
            Name = "";
            MeasurementScale = MaterialMeasurement.Length;
            Density = 0;
            Diameter = 0;
        }



        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialType"/>
        /// </summary>
        /// <param name="id">ID of material type</param>
        /// <param name="name">Name of material type</param>
        /// <param name="diameter">Diameter of material</param>
        /// <param name="density">Density of material</param>
        /// <param name="measurementType">Measurement type int</param>
        [JsonConstructor]
        public MaterialType(int id, string name, double diameter, double density, byte measurementType)
        {
            ID = id;
            Name = name;
            Diameter = diameter;
            Density = density;
            MeasurementScale = (MaterialMeasurement)Enum.Parse(typeof(MaterialMeasurement), measurementType.ToString());
        }



        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialType"/>
        /// </summary>
        /// <param name="id">ID of material type</param>
        /// <param name="name">Name of material type</param>
        /// <param name="diameter">Diameter of material</param>
        /// <param name="density">Density of material</param>
        /// <param name="measurementScale">Measurement scale</param>
        public MaterialType(int id, string name, double diameter, double density, MaterialMeasurement measurementScale)
        {
            ID = id;
            Name = name;
            Diameter = diameter;
            Density = density;
            MeasurementScale = measurementScale;
        }
    }



    /// <summary>
    /// Way material is measured
    /// </summary>
    public enum MaterialMeasurement
    {
        Weight = 0,
        Length = 1,
    }
}
