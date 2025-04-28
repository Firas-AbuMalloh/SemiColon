using System;
using System.Collections.Generic;

namespace SemiColon.Models;

public partial class Image
{
    public int Id { get; set; }

    public int? CardId { get; set; }

    public int? SellerId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public virtual Card? Card { get; set; }

    public virtual Seller? Seller { get; set; }
}
