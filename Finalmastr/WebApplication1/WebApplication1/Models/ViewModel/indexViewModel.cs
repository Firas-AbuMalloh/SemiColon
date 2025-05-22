namespace SemiColon.Models.ViewModel
{
    public class indexViewModel
    {
        public IEnumerable<Card> Products { get; set; } = null!;
        public IEnumerable<Category> Categories { get; set; } = null!;
        public IEnumerable<MainCategory> MainCategories { get; set; } = null!;
        public IEnumerable<bestOrderSellerViewModels> BestOrderSellers { get; set; } = null!;
        public IEnumerable<Testimonial> Testimonials { get; set; } = null!;

    }
}