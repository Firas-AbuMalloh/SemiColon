namespace SemiColon.Models.ViewModel
{
    public class adminProfileViewModel
    {
        public User _user { set; get; }
        public updateProfile _update { set; get; }
        public IEnumerable<Order> _orders { set; get; }
        public IEnumerable<Card> _allCards { set; get; }
        public IEnumerable<User> _allUsers { set; get; }
        public IEnumerable<Discount> _allDiscounts { set; get; }
        public IEnumerable<Blog> _allBlogs { set; get; }
        public IEnumerable<ContactFeedBack> _allFeedbacks { set; get; }
        public IEnumerable<Testimonial> Testimonials { get; set; }
        public IEnumerable<Favorite> _allFavorites { set; get; }
        public IEnumerable<Cart> _allCarts { set; get; }
    }
}
