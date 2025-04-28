using System;
using System.Collections.Generic;

namespace SemiColon.Models;

public partial class CartItem
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int CardId { get; set; }

    public int CartId { get; set; }

    public int Quantity { get; set; }

    public decimal Price { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Card Card { get; set; } = null!;

    public virtual Cart Cart { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
