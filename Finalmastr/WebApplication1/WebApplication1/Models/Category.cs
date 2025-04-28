using System;
using System.Collections.Generic;

namespace SemiColon.Models;

public partial class Category
{
    public int Id { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? ImageUrl { get; set; }

    public virtual ICollection<Card> Cards { get; set; } = new List<Card>();
}
