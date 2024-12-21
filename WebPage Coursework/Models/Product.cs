namespace WebPage_Coursework.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public int Stock {  get; set; }

        public int CategoryId { get; set; }
        public string CategoryName { get; set; }

        public string Screen { get; set; }
        public string Processor { get; set; }
        public string GraphicsСard { get; set; }
        public int Memory { get; set; }
        public int RAM { get; set; }
        public string CameraResolution { get; set; }
        public string OS { get; set; }
        public int Battery { get; set; }
    }
}
