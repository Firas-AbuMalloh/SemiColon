using System;
using System.Collections.Generic;

namespace SemiColon.Models;

public partial class CardKey
{
    public int Id { get; set; }

    public int CardId { get; set; }

    public string KeyValue { get; set; } = null!;

    public bool? IsUsed { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Card Card { get; set; } = null!;
}
