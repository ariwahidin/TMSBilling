namespace TMSBilling.Models.ViewModels
{
    public class OrderViewModel
    {
        public Order Header { get; set; }
        public List<OrderDetail> Details { get; set; }

        public string? OperationType { get; set; } 

        public OrderViewModel()
        {
            OperationType = string.Empty;
            Header = new Order();
            Details = new List<OrderDetail>();
        }
    }
}