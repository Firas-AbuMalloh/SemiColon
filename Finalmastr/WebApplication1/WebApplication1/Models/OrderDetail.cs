using System;
using System.Collections.Generic;

namespace SemiColon.Models;

public partial class OrderDetail
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int CardId { get; set; }

    public int Quantity { get; set; }

    public decimal Price { get; set; }

    public virtual Card Card { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}
