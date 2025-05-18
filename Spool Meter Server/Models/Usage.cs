using System;
using System.Collections.Generic;

namespace Spool_Meter_Server.Models;

public partial class Usage
{
    public int Id { get; set; }

    public string SpoolMeterId { get; set; } = null!;

    public DateTime Time { get; set; }

    public double RemainingAmountPercentage { get; set; }

    public virtual SpoolMeter SpoolMeter { get; set; } = null!;
}
