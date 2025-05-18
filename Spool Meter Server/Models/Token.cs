using System;
using System.Collections.Generic;

namespace Spool_Meter_Server.Models;

public partial class Token
{
    public string TokenValue { get; set; } = null!;

    public string TokenType { get; set; } = null!;

    public string Id { get; set; } = null!;

    public long CreationTimestamp { get; set; }
}
