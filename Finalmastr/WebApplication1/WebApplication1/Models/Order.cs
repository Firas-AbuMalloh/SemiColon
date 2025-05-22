using System;
using System.Collections.Generic;

namespace SemiColon.Models;

public partial class Order
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public DateTime? OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public bool IsSendAsGift { get; set; }

    public string? FirstNameGift { get; set; }

    public string? LastNameGift { get; set; }

    public string? PhoneNumberGift { get; set; }

    public string? EmailGift { get; set; }

    public string? MassegeGift { get; set; }

    public string? PaymentMethod { get; set; }

    public decimal? DiscountValue { get; set; }

    public decimal Subtotal { get; set; }

    public virtual ICollection<Dispute> Disputes { get; set; } = new List<Dispute>();

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual User User { get; set; } = null!;
}
