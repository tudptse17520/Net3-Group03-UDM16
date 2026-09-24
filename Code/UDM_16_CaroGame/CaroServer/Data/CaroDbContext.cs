using Microsoft.EntityFrameworkCore;
using CaroServer.Models;

namespace CaroServer.Data
{
    public class CaroDbContext : DbContext
    {
        // Bảng MatchHistories
        public DbSet<MatchHistory> MatchHistories { get; set; } = null!;

        public CaroDbContext() { }

        public CaroDbContext(DbContextOptions<CaroDbContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Cấu hình kết nối SQL Server (Local) - Fallback cho EF Core tools (ví dụ: dotnet ef migrations)
                // Cờ TrustServerCertificate=True cho phép chạy không cần config chứng chỉ SSL trên máy dev
                string connectionString = @"Server=localhost;Database=CaroGameDb;Trusted_Connection=True;TrustServerCertificate=True";
                optionsBuilder.UseSqlServer(connectionString);
            }
        }
    }
}
