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

        public DateTime? CreatedAt { get; set; }
    }
}
