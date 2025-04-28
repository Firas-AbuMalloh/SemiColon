namespace SemiColon.Models.ViewModel
{
    public class cartViewModel
    {
        public IEnumerable<CartItem> _cartItem { set; get; }
        public IEnumerable<temporaryCart> _tempCart { set; get; }

    }
}
