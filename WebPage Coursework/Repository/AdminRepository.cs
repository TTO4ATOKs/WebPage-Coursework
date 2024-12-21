using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using WebPage_Coursework.Models;

namespace WebPage_Coursework.Repository
{
    public class AdminRepository
    {
        private readonly string _connectionString;

        public AdminRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<bool> ValidatePasswordAsync(string hashedPassword)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "SELECT COUNT(*) FROM Admin WHERE [PasswordHash] = @Password";
                var count = await connection.ExecuteScalarAsync<int>(query, new { Password = hashedPassword });
                return count > 0;
            }
        }
    }
}
