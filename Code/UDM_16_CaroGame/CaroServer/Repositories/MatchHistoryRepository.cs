using System;
using System.Threading.Tasks;
using CaroServer.Data;
using CaroServer.Models;

namespace CaroServer.Repositories
{
    public class MatchHistoryRepository
    {
        private readonly CaroDbContext _dbContext;

        public MatchHistoryRepository(CaroDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SaveMatchAsync(MatchHistory match)
        {
            try
            {
                await _dbContext.MatchHistories.AddAsync(match);
                await _dbContext.SaveChangesAsync();
                Console.WriteLine($"[DB] Đã lưu trận đấu phòng {match.RoomId} vào CSDL.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB Error] Lỗi khi lưu trận đấu: {ex.Message}");
            }
        }
    }
}
