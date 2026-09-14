using System;
using System.Linq;
using System.Threading.Tasks;
using CaroServer.Managers;
using CaroServer.Models;
using CaroShared.Protocol;

namespace CaroServer.Core
{
    // Gửi message đến các Client
    public class EventBroadcaster
    {
        private readonly SessionManager _sessionManager;
        private readonly RoomManager _roomManager;

        public EventBroadcaster(SessionManager sessionManager, RoomManager roomManager)
        {
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _roomManager = roomManager ?? throw new ArgumentNullException(nameof(roomManager));
        }

        // Gửi message cho tất cả người trong phòng
        public async Task BroadcastToRoomAsync(string roomId, NetworkMessage message)
        {
            var room = _roomManager.GetRoom(roomId);
            if (room == null)
            {
                Console.WriteLine($"[EventBroadcaster] Room {roomId} not found for broadcasting.");
                return;
            }

            var participantIds = room.GetAllParticipantIds().ToList();

            // Gửi message cho các Client đồng thời
            var tasks = participantIds.Select(async id =>
            {
                var session = _sessionManager.GetSession(id);
                if (session != null)
                {
                    try
                    {
                        await session.SendMessageAsync(message);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[EventBroadcaster] Error sending to {id} (room {roomId}): {ex.Message}");
                    }
                }
            });

            await Task.WhenAll(tasks);
        }

        // Gửi message cho mọi người trong phòng, trừ một người
        public async Task BroadcastToRoomExceptAsync(string roomId, string excludedPlayerId, NetworkMessage message)
        {
            var room = _roomManager.GetRoom(roomId);
            if (room == null) return;

            var participantIds = room.GetAllParticipantIds()
                .Where(id => id != excludedPlayerId)
                .ToList();

            var tasks = participantIds.Select(async id =>
            {
                var session = _sessionManager.GetSession(id);
                if (session != null)
                {
                    try
                    {
                        await session.SendMessageAsync(message);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[EventBroadcaster] Error sending to {id} (room {roomId}): {ex.Message}");
                    }
                }
            });

            await Task.WhenAll(tasks);
        }

        // Gửi message cho tất cả Client đang online
        public async Task BroadcastToAllAsync(NetworkMessage message)
        {
            var allSessions = _sessionManager.GetAllSessions().ToList();

            var tasks = allSessions.Select(async session =>
            {
                try
                {
                    await session.SendMessageAsync(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EventBroadcaster] Error sending to all ({session.PlayerId}): {ex.Message}");
                }
            });

            await Task.WhenAll(tasks);
        }
    }
}
