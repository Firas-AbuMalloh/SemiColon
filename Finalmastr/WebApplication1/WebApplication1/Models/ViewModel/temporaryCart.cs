namespace SemiColon.Models.ViewModel
{
    public class temporaryCart
    {
        public int Id { get; set; } = 0;
        public int CardID { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public string CardName { get; set; }
        public decimal discount { get; set; } = 0;
        public decimal Subtotal { get; set; } = 0;
        public decimal Total { get; set; } = 0;
        public DateTime? CreatedAt { get; set; }
    }
}
