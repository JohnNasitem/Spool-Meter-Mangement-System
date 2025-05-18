using System;
using System.Collections.Generic;

namespace Spool_Meter_Server.Models;

public partial class NotificationSetting
{
    public string AccountId { get; set; } = null!;

    public bool SpoolMeterBatteryLow { get; set; }

    public bool SpoolMeterDied { get; set; }

    public bool MaterialLow { get; set; }

    public bool MaterialRanOut { get; set; }

    public virtual Account Account { get; set; } = null!;
}
