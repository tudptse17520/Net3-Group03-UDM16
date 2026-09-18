using Microsoft.EntityFrameworkCore;
using CaroServer.Models;

namespace CaroServer.Data
{
    public class CaroDbContext : DbContext
    {
        // Bảng MatchHistories
        public DbSet<MatchHistory> MatchHistories { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Cấu hình kết nối SQL Server (Local)
                // Cờ TrustServerCertificate=True cho phép chạy không cần config chứng chỉ SSL trên máy dev
                string connectionString = @"Server=localhost\SQLEXPRESS;Database=CaroGameDb;Trusted_Connection=True;TrustServerCertificate=True";
                optionsBuilder.UseSqlServer(connectionString);
            }
        }
    }
}
