using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using CaroServer.Game;
using CaroServer.Models;
using CaroShared.Constants;
using CaroShared.Enums;

namespace CaroServer.Managers
{
    // Quản lý các phòng chơi
    public class RoomManager
    {
        private readonly ConcurrentDictionary<string, Room> _rooms = new();

        // Sự kiện khi hết thời gian của lượt đánh
        public event Func<string, MoveResult, Task>? OnRoomTimeout;

        // Tạo phòng mới
        public string CreateRoom(string playerXId, string playerOId)
        {
            if (string.IsNullOrWhiteSpace(playerXId))
            {
                Console.WriteLine("[RoomManager] CreateRoom failed: playerXId is empty");
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(playerOId))
            {
                Console.WriteLine("[RoomManager] CreateRoom failed: playerOId is empty");
                return string.Empty;
            }

            if (string.Equals(playerXId, playerOId, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("[RoomManager] CreateRoom failed: cannot play against self");
                return string.Empty;
            }

            string roomId;
            Room room;
            do
            {
                roomId = "ROOM-" + System.Security.Cryptography.RandomNumberGenerator.GetInt32(1000000).ToString("D6");
                room = new Room(roomId, playerXId, playerOId);
                if (_rooms.TryAdd(roomId, room)) break;
                room.Session.Dispose();
            } while (true);

            {
                Console.WriteLine(
                    $"[RoomManager] Room {roomId} created: {playerXId} (X) vs {playerOId} (O)");

                // Bắt đầu đếm giờ cho lượt đầu tiên của Player X
                StartTurnTimer(
                    room.Session,
                    1,
                    room.Session.Engine.MoveCount + 1);

                // GAME EVENT LOG
                WriteGameEvent(
                    roomId,
                    "RoomCreated",
                    playerXId,
                    message: $"Phòng được tạo: {playerXId} (X) vs {playerOId} (O)");
            }

            return roomId;
        }

        // Xử lý nước đi của người chơi
        public MoveResult HandleMove(string roomId, string playerId, int x, int y)
        {
            if (string.IsNullOrWhiteSpace(roomId))
            {
                var invalidResult = new MoveResult
                {
                    IsValid = false,
                    ErrorCode = ErrorCode.InvalidRequest,
                    ErrorMessage = "roomId không hợp lệ",
                    X = x,
                    Y = y
                };

                WriteGameEvent(
                    roomId,
                    "InvalidMove",
                    playerId,
                    x,
                    y,
                    isValid: false,
                    message: invalidResult.ErrorMessage);

                return invalidResult;
            }

            if (string.IsNullOrWhiteSpace(playerId))
            {
                var invalidResult = new MoveResult
                {
                    IsValid = false,
                    ErrorCode = ErrorCode.InvalidRequest,
                    ErrorMessage = "playerId không hợp lệ",
                    X = x,
                    Y = y
                };

                WriteGameEvent(
                    roomId,
                    "InvalidMove",
                    playerId,
                    x,
                    y,
                    isValid: false,
                    message: invalidResult.ErrorMessage);

                return invalidResult;
            }

            var room = GetRoom(roomId);

            if (room == null)
            {
                var invalidResult = new MoveResult
                {
                    IsValid = false,
                    ErrorCode = ErrorCode.RoomNotFound,
                    ErrorMessage = "Phòng không tồn tại",
                    X = x,
                    Y = y
                };

                WriteGameEvent(
                    roomId,
                    "InvalidMove",
                    playerId,
                    x,
                    y,
                    isValid: false,
                    message: invalidResult.ErrorMessage);

                return invalidResult;
            }

            // Khán giả không được phép thực hiện nước đi
            if (!room.IsPlayer(playerId))
            {
                var invalidResult = new MoveResult
                {
                    IsValid = false,
                    ErrorCode = ErrorCode.PlayerNotInRoom,
                    ErrorMessage = "Bạn không phải người chơi trong phòng này",
                    X = x,
                    Y = y
                };

                WriteGameEvent(
                    roomId,
                    "InvalidMove",
                    playerId,
                    x,
                    y,
                    isValid: false,
                    message: invalidResult.ErrorMessage);

                return invalidResult;
            }

            int playerSymbol = room.GetPlayerSymbol(playerId);

            MoveResult result =
                room.Session.Engine.MakeMove(x, y, playerSymbol);

            if (result.IsValid)
            {
                room.Session.LastMoveX = x;
                room.Session.LastMoveY = y;
                // GAME EVENT LOG: nước đi hợp lệ
                WriteGameEvent(
                    roomId,
                    "MoveMade",
                    playerId,
                    x,
                    y,
                    playerSymbol,
                    isValid: true,
                    result: result.IsGameOver
                        ? (result.IsDraw ? "Draw" : "GameOver")
                        : "Playing");

                if (result.IsGameOver)
                {
                    room.Session.StopTimer();

                    // GAME EVENT LOG: kết thúc trận
                    WriteGameEvent(
                        roomId,
                        result.IsDraw ? "GameDraw" : "GameOver",
                        playerId,
                        x,
                        y,
                        playerSymbol,
                        isValid: true,
                        result: result.IsDraw
                            ? "Draw"
                            : $"Winner: {result.WinnerSymbol}",
                        message: result.IsDraw
                            ? "Trận đấu kết thúc với kết quả hòa"
                            : $"Người chơi {result.WinnerSymbol} thắng");
                }
                else
                {
                    // Bắt đầu đếm giờ cho lượt kế tiếp
                    StartTurnTimer(
                        room.Session,
                        result.NextTurn,
                        room.Session.Engine.MoveCount + 1);
                }
            }
            else
            {
                // GAME EVENT LOG: nước đi không hợp lệ
                WriteGameEvent(
                    roomId,
                    "InvalidMove",
                    playerId,
                    x,
                    y,
                    playerSymbol,
                    isValid: false,
                    message: result.ErrorMessage);
            }

            return result;
        }

        public MoveResult? HandleTimeout(
            string roomId,
            int timedOutPlayerSymbol)
        {
            var room = GetRoom(roomId);

            if (room == null)
            {
                return null;
            }

            MoveResult result =
                room.Session.Engine.HandleTimeout(timedOutPlayerSymbol);

            if (!result.IsValid)
            {
                return null;
            }

            room.Session.StopTimer();

            Console.WriteLine(
                $"[RoomManager] Room {roomId} timeout: Player {timedOutPlayerSymbol} lost");

            // GAME EVENT LOG: timeout
            WriteGameEvent(
                roomId,
                "GameTimeout",
                playerSymbol: timedOutPlayerSymbol,
                isValid: true,
                result: $"Winner: {result.WinnerSymbol}",
                message: $"Người chơi {timedOutPlayerSymbol} hết thời gian");

            return result;
        }

        public MoveResult? LeaveRoom(string roomId, string playerId)
        {
            if (string.IsNullOrWhiteSpace(roomId))
            {
                WriteGameEvent(
                    roomId,
                    "InvalidLeave",
                    playerId,
                    isValid: false,
                    message: "roomId không hợp lệ");

                return new MoveResult
                {
                    IsValid = false,
                    ErrorCode = ErrorCode.InvalidRequest,
                    ErrorMessage = "roomId không hợp lệ"
                };
            }

            if (string.IsNullOrWhiteSpace(playerId))
            {
                WriteGameEvent(
                    roomId,
                    "InvalidLeave",
                    playerId,
                    isValid: false,
                    message: "playerId không hợp lệ");

                return new MoveResult
                {
                    IsValid = false,
                    ErrorCode = ErrorCode.InvalidRequest,
                    ErrorMessage = "playerId không hợp lệ"
                };
            }

            var room = GetRoom(roomId);

            if (room == null)
            {
                WriteGameEvent(
                    roomId,
                    "InvalidLeave",
                    playerId,
                    isValid: false,
                    message: "Phòng không tồn tại");

                return new MoveResult
                {
                    IsValid = false,
                    ErrorCode = ErrorCode.RoomNotFound,
                    ErrorMessage = "Phòng không tồn tại"
                };
            }

            // Nếu khán giả rời phòng
            if (room.IsSpectator(playerId))
            {
                room.RemoveSpectator(playerId);

                Console.WriteLine(
                    $"[RoomManager] Spectator {playerId} left room {roomId}");

                WriteGameEvent(
                    roomId,
                    "SpectatorLeft",
                    playerId,
                    isValid: true,
                    message: "Khán giả đã rời phòng");

                return null;
            }

            if (!room.IsPlayer(playerId))
            {
                WriteGameEvent(
                    roomId,
                    "InvalidLeave",
                    playerId,
                    isValid: false,
                    message: "Người chơi không thuộc phòng này");

                return new MoveResult
                {
                    IsValid = false,
                    ErrorCode = ErrorCode.PlayerNotInRoom,
                    ErrorMessage = "Người chơi không thuộc phòng này"
                };
            }

            // Dừng timer khi người chơi chính rời phòng
            room.Session.StopTimer();

            int leavingSymbol = room.GetPlayerSymbol(playerId);

            // Người chơi còn lại được tính là người thắng
            int winnerSymbol = leavingSymbol == 1 ? 2 : 1;

            // GAME EVENT LOG
            WriteGameEvent(
                roomId,
                "PlayerLeft",
                playerId,
                playerSymbol: leavingSymbol,
                isValid: true,
                result: $"Winner: {winnerSymbol}",
                message: "Đối thủ đã rời phòng");

            return new MoveResult
            {
                IsValid = true,
                IsGameOver = true,
                WinnerSymbol = winnerSymbol,
                IsDraw = false,
                ErrorMessage = "Đối thủ đã rời phòng"
            };
        }

        public void RemoveRoom(string roomId)
        {
            if (string.IsNullOrWhiteSpace(roomId))
            {
                Console.WriteLine(
                    "[RoomManager] RemoveRoom: roomId is empty, skipped");

                WriteGameEvent(
                    roomId,
                    "InvalidRoomRemove",
                    isValid: false,
                    message: "roomId trống");

                return;
            }

            if (_rooms.TryRemove(roomId, out var room))
            {
                room.Session.Dispose();

                Console.WriteLine(
                    $"[RoomManager] Room {roomId} removed");

                // GAME EVENT LOG
                WriteGameEvent(
                    roomId,
                    "RoomRemoved",
                    isValid: true,
                    message: "Phòng đã bị xóa khỏi RoomManager");
            }
        }

        // Khởi tạo lại ván mới trong cùng một phòng và khởi động lại TurnTimer
        public bool ResetRoom(string roomId)
        {
            var room = GetRoom(roomId);
            if (room == null) return false;

            room.ResetSession();
            StartTurnTimer(room.Session, 1, room.Session.Engine.MoveCount + 1);
            Console.WriteLine($"[RoomManager] Room {roomId} reset for new game");
            return true;
        }

        // Tìm Room theo ID
        public Room? GetRoom(string roomId)
        {
            _rooms.TryGetValue(roomId, out var room);
            return room;
        }

        public GameSession? GetSession(string roomId)
        {
            return GetRoom(roomId)?.Session;
        }

        // Hỗ trợ thêm/xóa khán giả qua RoomManager
        public bool AddSpectator(string roomId, string spectatorId)
        {
            var room = GetRoom(roomId);

            if (room == null)
            {
                WriteGameEvent(
                    roomId,
                    "InvalidSpectatorJoin",
                    spectatorId,
                    isValid: false,
                    message: "Phòng không tồn tại");

                return false;
            }

            bool added = room.AddSpectator(spectatorId);

            WriteGameEvent(
                roomId,
                added ? "SpectatorJoined" : "InvalidSpectatorJoin",
                spectatorId,
                isValid: added,
                message: added
                    ? "Khán giả đã tham gia phòng"
                    : "Không thể thêm khán giả");

            return added;
        }

        public bool RemoveSpectator(string roomId, string spectatorId)
        {
            var room = GetRoom(roomId);

            if (room == null)
            {
                return false;
            }

            bool removed = room.RemoveSpectator(spectatorId);

            if (removed)
            {
                WriteGameEvent(
                    roomId,
                    "SpectatorLeft",
                    spectatorId,
                    isValid: true,
                    message: "Khán giả đã rời phòng");
            }

            return removed;
        }

        // Kích hoạt sự kiện hết giờ của phòng
        public Task TriggerRoomTimeout(
            string roomId,
            MoveResult moveResult)
        {
            return OnRoomTimeout?.Invoke(roomId, moveResult) ?? Task.CompletedTask;
        }

        public int GetActiveRoomCount()
        {
            return _rooms.Count;
        }

        /// <summary>
        /// Trả về danh sách tất cả phòng đang hoạt động dưới dạng RoomDto.
        /// </summary>
        public System.Collections.Generic.List<Models.Room> GetActiveRooms()
        {
            return _rooms.Values.ToList();
        }

        private void StartTurnTimer(
            GameSession session,
            int playerSymbol,
            int turnNumber)
        {
            session.Timer.StartTurn(
                turnNumber,
                GameConstants.TurnTimeoutSeconds,
                ignored => _ = CompleteTurnTimeoutAsync(session, playerSymbol, turnNumber));
        }

        public void ResumeTurnTimer(GameSession session)
        {
            int symbol = session.Engine.CurrentTurn;
            int turn = session.Engine.MoveCount + 1;
            session.ResumeTimer(ignored => _ = CompleteTurnTimeoutAsync(session, symbol, turn));
        }

        private async Task CompleteTurnTimeoutAsync(GameSession session, int symbol, int turn)
        {
            var room = GetRoom(session.RoomId);
            if (room == null) return;
            await room.Actions.WaitAsync();
            try
            {
                // A queued callback from an old turn, a paused game or a rematch is stale.
                if (!ReferenceEquals(room.Session, session) || room.HasDisconnectedPlayers ||
                    session.Engine.MoveCount + 1 != turn || session.Engine.CurrentTurn != symbol ||
                    session.Timer.DeadlineUtc == DateTime.MinValue || session.Timer.DeadlineUtc > DateTime.UtcNow) return;
                var result = HandleTimeout(session.RoomId, symbol);
                if (result != null) await TriggerRoomTimeout(session.RoomId, result);
            }
            catch (Exception ex) { Console.WriteLine($"[TurnTimeout] {ex.Message}"); }
            finally { room.Actions.Release(); }
        }

        // ============================================================
        // GAME EVENT LOG
        // ============================================================

        private static readonly object _logLock = new();

        private static readonly string _logDirectory =
            Path.Combine(AppContext.BaseDirectory, "Logs");

        private static readonly string _logFile =
            Path.Combine(_logDirectory, "game-events.jsonl");

        private static void WriteGameEvent(
            string roomId,
            string eventType,
            string? playerId = null,
            int? x = null,
            int? y = null,
            int? playerSymbol = null,
            bool? isValid = null,
            string? result = null,
            string? message = null)
        {
            try
            {
                Directory.CreateDirectory(_logDirectory);

                var gameEvent = new
                {
                    TimestampUtc = DateTime.UtcNow,
                    RoomId = roomId,
                    EventType = eventType,
                    PlayerId = playerId,
                    X = x,
                    Y = y,
                    PlayerSymbol = playerSymbol,
                    IsValid = isValid,
                    Result = result,
                    Message = message
                };

                string json = JsonSerializer.Serialize(gameEvent);

                lock (_logLock)
                {
                    File.AppendAllText(
                        _logFile,
                        json + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                // Logging error must not affect game.
                Console.WriteLine(
                    $"[GameEventLog] Failed to write log: {ex.Message}");
            }
        }
    }
}
