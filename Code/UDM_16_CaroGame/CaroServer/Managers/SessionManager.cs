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

        public void RemoveSession(string playerId)
        {
            if (_sessions.TryRemove(playerId, out PlayerSession? session))
            {
                session.Dispose();
                Console.WriteLine($"[SessionManager] Removed session: {playerId}. Total: {_sessions.Count}");
            }
        }

        public PlayerSession? GetSession(string playerId)
        {
            _sessions.TryGetValue(playerId, out var session);
            return session;
        }
    }
}
