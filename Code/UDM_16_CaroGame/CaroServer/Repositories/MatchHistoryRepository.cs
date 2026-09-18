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
                // Sử dụng EF Core LINQ để truy vấn
                return await _dbContext.MatchHistories
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
