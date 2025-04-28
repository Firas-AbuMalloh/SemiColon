using System;
using System.Collections.Generic;

namespace SemiColon.Models;

public partial class Analytic
{
    public int Id { get; set; }

    public int CardId { get; set; }

    public int? Views { get; set; }

    public int? Sales { get; set; }

    public DateOnly Date { get; set; }

    public virtual Card Card { get; set; } = null!;
}
