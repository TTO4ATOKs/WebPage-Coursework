using Dapper;
using Microsoft.Data.SqlClient;
using WebPage_Coursework.Models;
using WebPage_Coursework.Services;

namespace WebPage_Coursework.Repository
{
    public class ProductRepository
    {
        private readonly string _connectionString;

        public ProductRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<Product>> GetProductsAsync(int? categoryId = null)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = @"
                    SELECT p.Id, p.Name, p.Price, p.CategoryId, c.Name AS CategoryName, 
                    p.ImageUrl, p.Screen, p.Processor, p.GraphicsСard, p.Memory, p.RAM, p.CameraResolution, 
                    p.OS, p.Battery
                    FROM Products p
                    LEFT JOIN Categories c ON p.CategoryId = c.Id";

                if (categoryId.HasValue)
                {
                    query += " WHERE p.CategoryId = @CategoryId";
                }

                var products = (await connection.QueryAsync<Product>(query, new { CategoryId = categoryId })).ToList();

                return products;
            }
        }

        public async Task<List<Category>> GetCategoriesAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT Id, Name FROM Categories";

                return (await connection.QueryAsync<Category>(query)).ToList();
            }
        }

        public async Task<Product> GetProductByIdAsync(int productId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var productQuery = @"
            SELECT p.Id, p.Name, p.Price, p.Description, p.ImageUrl, p.CategoryId, c.Name AS CategoryName,
                   , p.Screen, p.Processor, p.GraphicsСard, p.Memory, p.RAM, p.CameraResolution,
                   p.OS, p.Battery
            FROM Products p
            LEFT JOIN Categories c ON p.CategoryId = c.Id
            WHERE p.Id = @ProductId";

                var product = await connection.QueryFirstOrDefaultAsync<Product>(productQuery, new { ProductId = productId });

                return product;
            }
        }

        public async Task AddProductAsync(Product product)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var productQuery = @"
            INSERT INTO Products (Name, Price, Description, ImageUrl, CategoryId, Screen, Processor, GraphicsСard, Memory, RAM, CameraResolution, OS, Battery)
            VALUES (@Name, @Price, @Description, @ImageUrl, @CategoryId, @Screen, @Processor, @GraphicsСard, @Memory, @RAM, @CameraResolution, @OS, @Battery);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

                var productId = await connection.ExecuteScalarAsync<int>(productQuery, product);
                product.Id = productId; 
            }
        }

        public async Task UpdateProductAsync(Product product)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var productQuery = @"
            UPDATE Products
            SET Name = @Name, Price = @Price, Description = @Description, ImageUrl = @ImageUrl, 
                CategoryId = @CategoryId, Screen = @Screen, Processor = @Processor, GraphicsСard = @GraphicsСard
                Memory = @Memory, RAM = @RAM, CameraResolution = @CameraResolution, OS = @OS, Battery = @Battery
            WHERE Id = @Id";

                await connection.ExecuteAsync(productQuery, product);
            }
        }
    }
}