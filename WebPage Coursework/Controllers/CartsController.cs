using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebPage_Coursework.Models;
using WebPage_Coursework.Repository;

namespace WebPage_Coursework.Controllers
{
    [AllowAnonymous]
    public class CartsController : Controller
    {
        private readonly CartRepository _cartRepository;

        public CartsController(CartRepository cartRepository)
        {
            _cartRepository = cartRepository;
        }

        public async Task<IActionResult> Index()
        {
            var userIdentifier = HttpContext.Session.GetString("UserIdentifier");

            if (string.IsNullOrEmpty(userIdentifier))
            {
                userIdentifier = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("UserIdentifier", userIdentifier);
            }

            var cart = await _cartRepository.GetCartByUserIdentifierAsync(userIdentifier);
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
            var userIdentifier = HttpContext.Session.GetString("UserIdentifier");

            if (string.IsNullOrEmpty(userIdentifier))
            {
                userIdentifier = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("UserIdentifier", userIdentifier);
            }

            await _cartRepository.AddToCart(userIdentifier, productId, quantity);

            return Redirect(Request.Headers["Referer"].ToString());
        }

        [HttpGet]
        public async Task<IActionResult> GetCartItems()
        {
            var cartItems = await _cartRepository.GetCartByUserIdentifierAsync(User.Identity.Name);
            return Json(cartItems);
        }

        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            var userIdentifier = HttpContext.Session.GetString("UserIdentifier");

            if (string.IsNullOrEmpty(userIdentifier))
            {
                userIdentifier = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("UserIdentifier", userIdentifier);
            }

            try
            {
                await _cartRepository.ClearCartAsync(userIdentifier);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ошибка очистки корзины: " + ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveCartItem(int productId)
        {
            var userIdentifier = HttpContext.Session.GetString("UserIdentifier");

            if (string.IsNullOrEmpty(userIdentifier))
            {
                return StatusCode(400, "Идентификатор пользователя отсутствует.");
            }

            try
            {
                await _cartRepository.RemoveCartItemAsync(userIdentifier, productId);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ошибка удаления товара из корзины: " + ex.Message);
            }
        }
    }
}