using System;
using System.Collections.Generic;

namespace Spool_Meter_Server.Models;

public partial class Account
{
    public string Id { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public virtual ICollection<AccountToFcmtoken> AccountToFcmtokens { get; set; } = new List<AccountToFcmtoken>();

    public virtual NotificationSetting? NotificationSetting { get; set; }

    public virtual ICollection<SpoolMeter> SpoolMeters { get; set; } = new List<SpoolMeter>();
}
