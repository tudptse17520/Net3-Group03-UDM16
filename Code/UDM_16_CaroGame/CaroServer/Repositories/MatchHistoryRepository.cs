using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using CaroServer.Data;
using CaroServer.Models;

namespace CaroServer.Repositories
{
    public class MatchHistoryRepository
    {
        private readonly DbContextOptions<CaroDbContext> _dbOptions;

        public MatchHistoryRepository(DbContextOptions<CaroDbContext> dbOptions)
        {
            _dbOptions = dbOptions;
        }

        public async Task SaveMatchAsync(MatchHistory match)
        {
            try
            {
                using var dbContext = new CaroDbContext(_dbOptions);
                await dbContext.MatchHistories.AddAsync(match);
                await dbContext.SaveChangesAsync();
                Console.WriteLine($"[DB] Match saved for room {match.RoomId}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB Error] Save match failed: {ex.Message}");
            }
        }

        public async Task<List<MatchHistory>> GetMatchHistoryAsync(string playerId)
        {
            try
            {
                // Sử dụng EF Core LINQ để truy vấn với AsNoTracking để tối ưu hiệu suất đọc
                using var dbContext = new CaroDbContext(_dbOptions);
                return await dbContext.MatchHistories
                    .AsNoTracking()
                    .Where(m => m.PlayerXId == playerId || m.PlayerOId == playerId)
                    .OrderByDescending(m => m.PlayedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB Error] Query history failed for {playerId}: {ex.Message}");
                return new List<MatchHistory>();
            }
        }
    }
}
