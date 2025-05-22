using System;
using System.Collections.Generic;

namespace SemiColon.Models;

public partial class Discount
{
    public int Id { get; set; }

    public string? DiscountCode { get; set; }

    public string? Description { get; set; }

    public decimal DiscountValue { get; set; }

    public bool IsPercentage { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int UsageCount { get; set; }

    public int? UsageLimit { get; set; }

    public bool IsActive { get; set; }

    public int? ApplicableToUser { get; set; }

    public bool ApplicableToCart { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
