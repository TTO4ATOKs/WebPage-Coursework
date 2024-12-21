using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using WebPage_Coursework.Models;
using WebPage_Coursework.Repository;
using WebPage_Coursework.Services;
using Dapper;

namespace WebPage_Coursework.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ProductRepository _productRepository;
        private readonly string _connectionString;

        public HomeController(ProductRepository productRepository, ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _productRepository = productRepository;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IActionResult> Index(int? categoryId)
        {
            var products = await _productRepository.GetProductsAsync(categoryId);
            var categories = await _productRepository.GetCategoriesAsync();

            ViewBag.Categories = categories;
            ViewBag.SelectedCategoryId = categoryId;

            return View(products);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> Details(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT * FROM Products WHERE Id = @Id"; 
                var product = await connection.QueryFirstOrDefaultAsync<Product>(query, new { Id = id });

                if (product == null)
                {
                    return NotFound(); 
                }

                return PartialView("Details", product);
            }
        }
    }
}
