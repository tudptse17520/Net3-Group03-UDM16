using System;
using System.Collections.Concurrent;
using CaroServer.Models;

namespace CaroServer.Managers
{
    // Quản lý các session đang kết nối
    public class SessionManager
    {
        // Lưu trữ session theo PlayerId
        private readonly ConcurrentDictionary<string, PlayerSession> _sessions = new();

        public void AddSession(PlayerSession session)
        {
            _sessions.TryAdd(session.PlayerId, session);
            Console.WriteLine($"[SessionManager] Added session: {session.PlayerId}. Total: {_sessions.Count}");
        }

        // dispose = true khi Client thật sự ngắt kết nối
        // dispose = false khi chỉ đổi ID (Login) → giữ socket sống
        public void RemoveSession(string playerId, bool dispose = true)
        {
            if (_sessions.TryRemove(playerId, out PlayerSession? session))
            {
                if (dispose)
                {
                    session.Dispose();
                }
                Console.WriteLine($"[SessionManager] Removed session: {playerId} (dispose={dispose}). Total: {_sessions.Count}");
            }
        }

        public PlayerSession? GetSession(string playerId)
        {
            _sessions.TryGetValue(playerId, out var session);
            return session;
        }
    }
}
