using System;
using System.Collections.Generic;

namespace SemiColon.Models;

public partial class Promotion
{
    public int Id { get; set; }

    public int CardId { get; set; }

    public decimal Discount { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Card Card { get; set; } = null!;
}
