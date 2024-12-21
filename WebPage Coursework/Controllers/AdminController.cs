using Microsoft.AspNetCore.Mvc;
using WebPage_Coursework.Repository;

namespace WebPage_Coursework.Controllers
{
    public class AdminController : Controller
    {
        private readonly AdminRepository _adminRepository;
        private readonly OrderRepository _orderRepository;

        public AdminController(AdminRepository adminRepository, OrderRepository orderRepository)
        {
            _adminRepository = adminRepository;
            _orderRepository = orderRepository;
        }

        public async Task<IActionResult> Orders()
        {
            var orders = await _orderRepository.GetAllOrdersAsync();
            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _orderRepository.GetOrderByIdAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string password)
        {
            var hashedPassword = ComputeSha256Hash(password);

            var isValidPassword = await _adminRepository.ValidatePasswordAsync(hashedPassword);

            if (isValidPassword) 
            {
                HttpContext.Session.SetString("IsAdmin", "true");
                return RedirectToAction("Index", "Admin"); 
            }
            else 
            {
                ViewBag.ErrorMessage = "Неверный пароль.";
                return View(); 
            }
        }

        [HttpGet]
        public IActionResult Index()
        {
            var isAdmin = HttpContext.Session.GetString("IsAdmin") == "true";

            if (!isAdmin)
                return RedirectToAction("Login"); 

            return View();
        }

        private string ComputeSha256Hash(string rawData)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawData));
                var builder = new System.Text.StringBuilder();
                foreach (var b in bytes)
                    builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }


    }
}
