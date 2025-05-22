using System;
using System.Collections.Generic;

namespace SemiColon.Models;

public partial class MainCategory
{
    public int Id { get; set; }

    public string MainCategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? ImageUrl { get; set; }

    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
}
