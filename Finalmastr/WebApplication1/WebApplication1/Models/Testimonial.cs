using System;
using System.Collections.Generic;

namespace SemiColon.Models;

public partial class Testimonial
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string? UserName { get; set; }

    public string? UserEmail { get; set; }

    public string? Subject { get; set; }

    public string Message { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public bool IsPublished { get; set; }

    public virtual User? User { get; set; }
}
