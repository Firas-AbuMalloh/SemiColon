namespace SemiColon.Models
{
    public class bestOrderSellerViewModels
    {
        public int CardId { get; set; }
        public string CardName { get; set; } = null!;
        public decimal CardPrice { get; set; }
        public string CardImage { get; set; } = null!;
        public int CardCount { get; set; } // عدد مرات تكرار البطاقة
    }

}
