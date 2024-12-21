namespace WebPage_Coursework.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int CartId { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string UserIdentifier { get; set; }
        public List<OrderProduct> Products { get; set; } = new List<OrderProduct>();
        public DateTime OrderDate { get; set; } = DateTime.Now;
    }
}
