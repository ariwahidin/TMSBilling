namespace TMSBilling.Models.ViewModels
{
    public class JobViewModel
    {
        public Job Header { get; set; }
        //public List<OrderDetail> Details { get; set; }

        public JobViewModel()
        {
            Header = new Job();
            //Details = new List<OrderDetail>();
        }
    }
}