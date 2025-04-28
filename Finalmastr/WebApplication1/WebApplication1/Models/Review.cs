using System;
using System.Collections.Generic;

namespace SemiColon.Models;

public partial class Review
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int CardId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Card Card { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
