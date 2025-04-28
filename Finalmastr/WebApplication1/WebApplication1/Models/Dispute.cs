using System;
using System.Collections.Generic;

namespace SemiColon.Models;

public partial class Dispute
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int UserId { get; set; }

    public int SellerId { get; set; }

    public string? Description { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Seller Seller { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
