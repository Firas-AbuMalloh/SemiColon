using System;
using System.Collections.Generic;

namespace SemiColon.Models;

public partial class ContactFeedBack
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string Message { get; set; } = null!;

    public DateOnly? CreatedAt { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string Subject { get; set; } = null!;

    public string Name { get; set; } = null!;

    public bool IsPublished { get; set; }
}
