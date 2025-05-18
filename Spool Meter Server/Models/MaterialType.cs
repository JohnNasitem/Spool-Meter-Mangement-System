using System;
using System.Collections.Generic;

namespace Spool_Meter_Server.Models;

public partial class MaterialType
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public double Diameter { get; set; }

    public double Density { get; set; }

    public byte MeasurementType { get; set; }

    public virtual ICollection<SpoolMeter> SpoolMeters { get; set; } = new List<SpoolMeter>();
}
