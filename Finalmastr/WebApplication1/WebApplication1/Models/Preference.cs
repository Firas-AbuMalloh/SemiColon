using System;
using System.Collections.Generic;

namespace SemiColon.Models;

public partial class Preference
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string? Language { get; set; }

    public string? Currency { get; set; }

    public string? Theme { get; set; }

    public virtual User User { get; set; } = null!;
}
