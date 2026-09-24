using System;
using System.Collections.Concurrent;
using CaroServer.Models;

namespace CaroServer.Managers
{
    // Quản lý session đang kết nối và các session tạm giữ để Reconnect
    public class SessionManager
    {
        private readonly ConcurrentDictionary<string, PlayerSession> _sessions = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, PendingReconnectSession> _pendingReconnects = new();
        private readonly object _loginLock = new();

        public bool TryLogin(PlayerSession session, string nickname)
        {
            lock (_loginLock)
            {
                if (session.IsAuthenticated || _pendingReconnects.Values.Any(p =>
                    p.PlayerId.Equals(nickname, StringComparison.OrdinalIgnoreCase))) return false;
                // Reserve the name atomically before removing the temporary connection ID.
                if (!_sessions.TryAdd(nickname, session)) return false;
                RemoveSession(session.PlayerId, session, dispose: false);
                session.PlayerId = nickname;
                session.IsAuthenticated = true;
                return true;
            }
        }

        public void AddSession(PlayerSession session)
        {
            _sessions[session.PlayerId] = session;
            Console.WriteLine($"[SessionManager] Added session: {session.PlayerId}. Total: {_sessions.Count}");
        }

        // Chỉ xóa đúng instance được chỉ định để tránh một connection cũ
        // vô tình xóa connection mới sau khi Reconnect.
        public bool RemoveSession(string playerId, PlayerSession session, bool dispose = true)
        {
            if (!_sessions.TryGetValue(playerId, out var current) || !ReferenceEquals(current, session))
            {
                return false;
            }

            if (((ICollection<KeyValuePair<string, PlayerSession>>)_sessions).Remove(new KeyValuePair<string, PlayerSession>(playerId, session)))
            {
                if (dispose)
                {
                    session.Dispose();
                }

                Console.WriteLine($"[SessionManager] Removed session: {playerId} (dispose={dispose}). Total: {_sessions.Count}");
                return true;
            }

            return false;
        }

        // Compatibility overload cho các luồng cũ.
        public void RemoveSession(string playerId, bool dispose = true)
        {
            if (_sessions.TryRemove(playerId, out var session))
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

        public IEnumerable<PlayerSession> GetAllSessions()
        {
            return _sessions.Values;
        }

        // Lưu thông tin phiên sau khi Client mất kết nối trong thời gian ReconnectWindowSeconds.
        public PendingReconnectSession HoldForReconnect(PlayerSession session)
        {
            var pending = new PendingReconnectSession(
                session.PlayerId,
                session.SessionToken,
                session.CurrentRoomId,
                DateTime.UtcNow);

            _pendingReconnects[pending.SessionToken] = pending;
            return pending;
        }

        // Lấy và đồng thời xóa pending session để token chỉ được sử dụng một lần.
        public bool TryTakeReconnectSession(
            string sessionToken,
            TimeSpan reconnectWindow,
            out PendingReconnectSession pending)
        {
            pending = null!;

            if (string.IsNullOrWhiteSpace(sessionToken) ||
                !_pendingReconnects.TryGetValue(sessionToken, out var existing))
            {
                return false;
            }

            if (DateTime.UtcNow - existing.DisconnectedAtUtc > reconnectWindow)
            {
                _pendingReconnects.TryRemove(sessionToken, out _);
                return false;
            }

            if (((ICollection<KeyValuePair<string, PendingReconnectSession>>)_pendingReconnects).Remove(new KeyValuePair<string, PendingReconnectSession>(sessionToken, existing)))
            {
                pending = existing;
                return true;
            }

            return false;
        }

        // Dùng bởi tác vụ hết hạn của Server để lấy pending session bất kể
        // độ trễ của scheduler, sau đó tự xử lý việc xử thua.
        public bool TryTakePendingReconnectSession(
            string sessionToken,
            out PendingReconnectSession pending)
        {
            pending = null!;

            if (string.IsNullOrWhiteSpace(sessionToken) ||
                !_pendingReconnects.TryGetValue(sessionToken, out var existing))
            {
                return false;
            }

            if (((ICollection<KeyValuePair<string, PendingReconnectSession>>)_pendingReconnects).Remove(new KeyValuePair<string, PendingReconnectSession>(sessionToken, existing)))
            {
                pending = existing;
                return true;
            }

            return false;
        }

        public void RemovePendingReconnect(string sessionToken)
        {
            if (!string.IsNullOrWhiteSpace(sessionToken))
            {
                _pendingReconnects.TryRemove(sessionToken, out _);
            }
        }
    }

    public sealed record PendingReconnectSession(
        string PlayerId,
        string SessionToken,
        string? RoomId,
        DateTime DisconnectedAtUtc);
}
