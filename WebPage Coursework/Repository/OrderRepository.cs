using Dapper;
using Microsoft.Data.SqlClient;
using WebPage_Coursework.Models;

namespace WebPage_Coursework.Repository
{
    public class OrderRepository
    {
        private readonly string _connectionString;

        public OrderRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IEnumerable<OrderViewModel>> GetAllOrdersAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var queryOrders = "SELECT * FROM Orders";
                var orders = await connection.QueryAsync<OrderViewModel>(queryOrders);

                foreach (var order in orders)
                {
                    var queryOrderItems = "SELECT oi.ProductId, p.Name AS ProductName, oi.Quantity " +
                                          "FROM OrderItems oi " +
                                          "JOIN Products p ON oi.ProductId = p.Id " +
                                          "WHERE oi.OrderId = @OrderId";

                    var orderItems = await connection.QueryAsync<OrderItemViewModel>(
                        queryOrderItems, new { OrderId = order.Id });

                    order.OrderItems = orderItems.ToList();
                }

                return orders;
            }
        }

        public async Task<OrderViewModel> GetOrderByIdAsync(int orderId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var queryOrder = @"
            SELECT Id, CustomerName, Phone, Address, OrderDate 
            FROM Orders 
            WHERE Id = @OrderId";
                var order = await connection.QueryFirstOrDefaultAsync<OrderViewModel>(
                    queryOrder, new { OrderId = orderId });

                if (order == null)
                {
                    return null;
                }

                var queryOrderItems = @"
            SELECT 
                oi.ProductId, 
                p.Name AS ProductName, 
                oi.Quantity, 
                oi.Price 
            FROM OrderItems oi
            JOIN Products p ON oi.ProductId = p.Id
            WHERE oi.OrderId = @OrderId";

                var orderItems = await connection.QueryAsync<OrderItemViewModel>(
                    queryOrderItems, new { OrderId = orderId });

                order.OrderItems = orderItems.ToList();

                return order;
            }
        }
    }
}
