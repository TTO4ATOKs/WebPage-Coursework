namespace WebPage_Coursework.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public int CartId { get; set; } 
        public decimal Price { get; set; }

        public Product Product { get; set; } 
    }
}
