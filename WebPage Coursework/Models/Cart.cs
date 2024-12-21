namespace WebPage_Coursework.Models
{
    public class Cart
    {
        public int Id { get; set; }
        public string UserIdentifier { get; set; }
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        public decimal TotalAmount => Items.Sum(item => item.Product.Price * item.Quantity);
    }
}
