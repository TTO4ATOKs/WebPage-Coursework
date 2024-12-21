using Dapper;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using WebPage_Coursework.Models;
using System.Transactions;
using System.Data.Common;

namespace WebPage_Coursework.Repository
{
    public class CartRepository
    {
        private readonly string _connectionString;

        public CartRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<Cart> GetCartByUserIdentifierAsync(string userIdentifier)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var queryCart = "SELECT * FROM Carts WHERE UserIdentifier = @UserIdentifier";
                var cart = await connection.QueryFirstOrDefaultAsync<Cart>(queryCart, new { UserIdentifier = userIdentifier });

                if (cart != null)
                {
                    var queryCartItems = @"
                SELECT ci.*, p.Id as ProductId, p.Name, p.Price, p.ImageUrl
                FROM CartItems ci
                INNER JOIN Products p ON ci.ProductId = p.Id
                WHERE ci.CartId = @CartId";

                    var items = await connection.QueryAsync<CartItem, Product, CartItem>(
                        queryCartItems,
                        (cartItem, product) =>
                        {
                            cartItem.Product = product;
                            return cartItem;
                        },
                        new { CartId = cart.Id },
                        splitOn: "ProductId"
                    );

                    cart.Items = items.ToList();
                }

                return cart;
            }
        }

        public async Task AddToCart(string userIdentifier, int productId, int quantity)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var checkStockQuery = "SELECT Stock FROM Products WHERE Id = @ProductId";
                        var stock = await connection.ExecuteScalarAsync<int>(
                            checkStockQuery, new { ProductId = productId }, transaction);

                        if (stock < quantity)
                        {
                            throw new InvalidOperationException("Недостаточно товара на складе.");
                        }

                        var queryCart = "SELECT * FROM Carts WHERE UserIdentifier = @UserIdentifier";
                        var cart = await connection.QueryFirstOrDefaultAsync<Cart>(
                            queryCart, new { UserIdentifier = userIdentifier }, transaction);

                        if (cart == null)
                        {
                            var insertCart = "INSERT INTO Carts (UserIdentifier) OUTPUT INSERTED.Id VALUES (@UserIdentifier)";
                            var cartId = await connection.ExecuteScalarAsync<int>(
                                insertCart, new { UserIdentifier = userIdentifier }, transaction);
                            cart = new Cart { Id = cartId, UserIdentifier = userIdentifier };
                        }

                        var queryCartItem = "SELECT * FROM CartItems WHERE CartId = @CartId AND ProductId = @ProductId";
                        var existingItem = await connection.QueryFirstOrDefaultAsync<CartItem>(
                            queryCartItem, new { CartId = cart.Id, ProductId = productId }, transaction);

                        if (existingItem != null)
                        {
                            var updateItem = "UPDATE CartItems SET Quantity = Quantity + @Quantity WHERE Id = @Id";
                            await connection.ExecuteAsync(
                                updateItem, new { Quantity = quantity, Id = existingItem.Id }, transaction);
                        }
                        else
                        {
                            var insertItem = "INSERT INTO CartItems (CartId, ProductId, Quantity) VALUES (@CartId, @ProductId, @Quantity)";
                            await connection.ExecuteAsync(
                                insertItem, new { CartId = cart.Id, ProductId = productId, Quantity = quantity }, transaction);
                        }

                        var updateStockQuery = "UPDATE Products SET Stock = Stock - @Quantity WHERE Id = @ProductId";
                        await connection.ExecuteAsync(
                            updateStockQuery, new { Quantity = quantity, ProductId = productId }, transaction);

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<IEnumerable<CartItem>> GetCartItemsAsync(int cartId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT * FROM CartItems WHERE CartId = @CartId";
                return await connection.QueryAsync<CartItem>(query, new { CartId = cartId });
            }
        }

        public async Task UpdateCartItemQuantity(string userIdentifier, int cartItemId, int quantity)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var queryCart = "SELECT * FROM Carts WHERE UserIdentifier = @UserIdentifier";
                        var cart = await connection.QueryFirstOrDefaultAsync<Cart>(queryCart, new { UserIdentifier = userIdentifier }, transaction);

                        if (cart == null)
                        {
                            throw new InvalidOperationException("Корзина не найдена.");
                        }

                        var queryCartItem = "SELECT * FROM CartItems WHERE Id = @CartItemId AND CartId = @CartId";
                        var cartItem = await connection.QueryFirstOrDefaultAsync<CartItem>(
                            queryCartItem, new { CartItemId = cartItemId, CartId = cart.Id }, transaction);

                        if (cartItem == null)
                        {
                            throw new InvalidOperationException("Товар не найден в корзине.");
                        }

                        // Обновляем количество товара
                        var updateQuantityQuery = "UPDATE CartItems SET Quantity = @Quantity WHERE Id = @CartItemId";
                        await connection.ExecuteAsync(updateQuantityQuery, new { Quantity = quantity, CartItemId = cartItemId }, transaction);

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task ClearCartAsync(string userIdentifier)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var queryCart = "SELECT Id FROM Carts WHERE UserIdentifier = @UserIdentifier";
                        var cartId = await connection.QueryFirstOrDefaultAsync<int?>(
                            queryCart,
                            new { UserIdentifier = userIdentifier },
                            transaction);

                        if (cartId == null)
                        {
                            throw new Exception("Корзина для указанного пользователя не найдена.");
                        }

                        var queryCartItems = @"
                    SELECT ProductId, Quantity
                    FROM CartItems
                    WHERE CartId = @CartId";
                        var cartItems = await connection.QueryAsync<(int ProductId, int Quantity)>(
                            queryCartItems,
                            new { CartId = cartId },
                            transaction);

                        var updateStockQuery = @"
                    UPDATE Products
                    SET Stock = Stock + @Quantity
                    WHERE Id = @ProductId";
                        foreach (var item in cartItems)
                        {
                            await connection.ExecuteAsync(
                                updateStockQuery,
                                new { item.ProductId, item.Quantity },
                                transaction);
                        }

                        var deleteCartItemsQuery = "DELETE FROM CartItems WHERE CartId = @CartId";
                        await connection.ExecuteAsync(
                            deleteCartItemsQuery,
                            new { CartId = cartId },
                            transaction);

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task RemoveCartItemAsync(string userIdentifier, int productId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var queryCart = "SELECT Id FROM Carts WHERE UserIdentifier = @UserIdentifier";
                        var cartId = await connection.QueryFirstOrDefaultAsync<int?>(
                            queryCart,
                            new { UserIdentifier = userIdentifier },
                            transaction);

                        if (cartId == null)
                        {
                            throw new Exception("Корзина для указанного пользователя не найдена.");
                        }

                        var queryCartItem = @"
                SELECT Quantity
                FROM CartItems
                WHERE CartId = @CartId AND ProductId = @ProductId";
                        var quantity = await connection.QueryFirstOrDefaultAsync<int?>(
                            queryCartItem,
                            new { CartId = cartId, ProductId = productId },
                            transaction);

                        if (quantity == null)
                        {
                            throw new Exception("Товар не найден в корзине.");
                        }

                        var updateStockQuery = @"
                UPDATE Products
                SET Stock = Stock + @Quantity
                WHERE Id = @ProductId";
                        await connection.ExecuteAsync(
                            updateStockQuery,
                            new { ProductId = productId, Quantity = quantity },
                            transaction);

                        var deleteCartItemQuery = "DELETE FROM CartItems WHERE CartId = @CartId AND ProductId = @ProductId";
                        await connection.ExecuteAsync(
                            deleteCartItemQuery,
                            new { CartId = cartId, ProductId = productId },
                            transaction);

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}