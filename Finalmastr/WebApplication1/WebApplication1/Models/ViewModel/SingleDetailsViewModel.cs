namespace SemiColon.Models.ViewModel
{
    public class SingleDetailsViewModel
    {
        public SingleDetailsViewModel() { }
        public Card singleCard{ set; get; }
        public IEnumerable<Card> cards { set; get; }
    
    }
}
