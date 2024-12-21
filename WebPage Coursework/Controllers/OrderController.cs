using Dapper;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data.Common;
using System.Transactions;
using WebPage_Coursework.Models;
using WebPage_Coursework.Repository;
using WebPage_Coursework.Services;

namespace WebPage_Coursework.Controllers
{
    public class OrderController : Controller
    {
        private readonly string _connectionString;

        public OrderController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public IActionResult OrderForm()
        {
            var model = new OrderViewModel();
            return PartialView("OrderForm", model);
        }

        [HttpGet]
        public IActionResult GetOrderForm()
        {
            var orderViewModel = new OrderViewModel
            {
                CustomerName = "",
                Phone = "",
                Address = ""
            };

            return PartialView("OrderForm", orderViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitOrder(Order orderRequest)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var userIdentifier = HttpContext.Session.GetString("UserIdentifier");

                        var queryCart = "SELECT * FROM Carts WHERE UserIdentifier = @UserIdentifier";
                        var cart = await connection.QueryFirstOrDefaultAsync<Cart>(
                            queryCart,
                            new { UserIdentifier = userIdentifier },
                            transaction
                        );

                        if (cart == null)
                        {
                            throw new Exception("Корзина не найдена для текущего пользователя.");
                        }

                        orderRequest.CartId = cart.Id;

                        var queryInsertOrder = @"
                    INSERT INTO Orders (CustomerName, Phone, Address, OrderDate, CartId) 
                    VALUES (@Name, @Phone, @Address, @OrderDate, @CartId); 
                    SELECT CAST(SCOPE_IDENTITY() as int)";

                        var orderId = await connection.ExecuteScalarAsync<int>(
                            queryInsertOrder,
                            new
                            {
                                orderRequest.Name,
                                orderRequest.Phone,
                                orderRequest.Address,
                                orderRequest.OrderDate,
                                orderRequest.CartId
                            },
                            transaction
                        );

                        var queryCartItems = "SELECT * FROM CartItems WHERE CartId = @CartId";
                        var cartItems = await connection.QueryAsync<CartItem>(
                            queryCartItems,
                            new { CartId = orderRequest.CartId },
                            transaction
                        );

                        foreach (var cartItem in cartItems)
                        {
                            var queryProductPrice = "SELECT Price FROM Products WHERE Id = @ProductId";
                            var productPrice = await connection.ExecuteScalarAsync<decimal>(
                                queryProductPrice,
                                new { ProductId = cartItem.ProductId },
                                transaction
                            );

                            var queryInsertOrderItem = @"
                        INSERT INTO OrderItems (OrderId, ProductId, Quantity, Price) 
                        VALUES (@OrderId, @ProductId, @Quantity, @Price)";

                            await connection.ExecuteAsync(
                                queryInsertOrderItem,
                                new
                                {
                                    OrderId = orderId,
                                    ProductId = cartItem.ProductId,
                                    Quantity = cartItem.Quantity,
                                    Price = productPrice 
                                },
                                transaction
                            );
                        }

                        var queryDeleteCartItems = "DELETE FROM CartItems WHERE CartId = @CartId";
                        await connection.ExecuteAsync(
                            queryDeleteCartItems,
                            new { CartId = orderRequest.CartId },
                            transaction
                        );

                        transaction.Commit();
                        return RedirectToAction("Index", "Carts");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return StatusCode(500, $"Ошибка оформления заказа: {ex.Message}");
                    }
                }
            }
        }
    }
}