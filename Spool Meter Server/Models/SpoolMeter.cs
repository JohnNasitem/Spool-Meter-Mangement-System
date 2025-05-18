using System;
using System.Collections.Generic;

namespace Spool_Meter_Server.Models;

public partial class SpoolMeter
{
    public string Id { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Password { get; set; } = null!;

    public double RemainingAmount { get; set; }

    public double OriginalAmount { get; set; }

    public byte BatteryStatus { get; set; }

    public int MaterialTypeId { get; set; }

    public string Color { get; set; } = null!;

    public virtual MaterialType MaterialType { get; set; } = null!;

    public virtual ICollection<Usage> Usages { get; set; } = new List<Usage>();

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
}
