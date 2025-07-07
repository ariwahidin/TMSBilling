namespace TMSBilling.Models.ViewModels
{
    public class OrderViewModel
    {
        public Order Header { get; set; }
        public List<OrderDetail> Details { get; set; }

        public OrderViewModel()
        {
            Header = new Order();
            Details = new List<OrderDetail>();
        }
    }
}