using System;
using System.Collections.Generic;

namespace SemiColon.Models;

public partial class Seller
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string? CompanyName { get; set; }

    public string? ContactEmail { get; set; }

    public string? ContactPhone { get; set; }

    public decimal? Balance { get; set; }

    public string? PaymentMethod { get; set; }

    public string? TaxId { get; set; }

    public string? BankAccount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Card> Cards { get; set; } = new List<Card>();

    public virtual ICollection<Dispute> Disputes { get; set; } = new List<Dispute>();

    public virtual ICollection<Image> Images { get; set; } = new List<Image>();

    public virtual User User { get; set; } = null!;
}
