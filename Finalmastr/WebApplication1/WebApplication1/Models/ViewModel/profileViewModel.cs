using System.Collections.Generic;

namespace SemiColon.Models.ViewModel
{
    public class profileViewModel
    {
        public User _user { set; get; }
        public IEnumerable<Order> _orders { set; get; }
        public IEnumerable<OrderDetail> _ordersDetails { set; get; }

    }
}
