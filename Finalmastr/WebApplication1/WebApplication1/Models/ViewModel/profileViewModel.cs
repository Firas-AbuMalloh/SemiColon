using System.Collections.Generic;

namespace SemiColon.Models.ViewModel
{
    public class profileViewModel
    {
        public User _user { set; get; }
        public updateProfile _update { set; get; }
        public IEnumerable<Order> _orders { set; get; }


    }
}
