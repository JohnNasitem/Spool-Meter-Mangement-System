using System;
using System.Collections.Generic;

namespace Spool_Meter_Server.Models;

public partial class AccountToFcmtoken
{
    public string Fcmtoken { get; set; } = null!;

    public string AccountId { get; set; } = null!;

    public virtual Account Account { get; set; } = null!;
}
