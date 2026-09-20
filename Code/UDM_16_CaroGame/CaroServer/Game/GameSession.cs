using System;
using CaroShared.Contracts;

namespace CaroServer.Game
{
    // Đại diện cho một ván đấu trong phòng
    public class GameSession : IDisposable
    {
        public string RoomId { get; private set; }
        public string PlayerXId { get; private set; }
        public string PlayerOId { get; private set; }
        
        public Guid MatchId { get; private set; }

        // Mỗi ván đấu sử dụng một CaroEngine riêng
        public CaroEngine Engine { get; private set; }
        public TurnTimer Timer { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private int _isCompleted = 0;

        // --- Draw Negotiation State ---
        private readonly object _drawLock = new object();
        public Guid? PendingDrawOfferId { get; private set; }
        public string? PendingDrawOfferedBy { get; private set; }
        public string? PendingDrawOfferedTo { get; private set; }
        public int? MovesAtLastDrawOffer { get; private set; }

        public GameSession(string roomId, string playerXId, string playerOId)
        {
            RoomId = roomId;
            PlayerXId = playerXId;
            PlayerOId = playerOId;
            MatchId = Guid.NewGuid();
            Engine = new CaroEngine();
            Timer = new TurnTimer();
            CreatedAt = DateTime.Now;
        }

        // Tạo lời mời hòa (nguyên tử)
        public bool TrySetDrawOffer(string offeredBy, string offeredTo, out Guid offerId)
        {
            lock (_drawLock)
            {
                if (PendingDrawOfferId.HasValue)
                {
                    offerId = Guid.Empty;
                    return false;
                }

                // Chống spam: Không được mời hòa lại nếu chưa có thêm nước cờ nào
                if (MovesAtLastDrawOffer.HasValue && Engine.MoveCount == MovesAtLastDrawOffer.Value)
                {
                    offerId = Guid.Empty;
                    return false;
                }

                offerId = Guid.NewGuid();
                PendingDrawOfferId = offerId;
                PendingDrawOfferedBy = offeredBy;
                PendingDrawOfferedTo = offeredTo;
                MovesAtLastDrawOffer = Engine.MoveCount;
                return true;
            }
        }

        public void ClearDrawOffer()
        {
            lock (_drawLock)
            {
                PendingDrawOfferId = null;
                PendingDrawOfferedBy = null;
                PendingDrawOfferedTo = null;
            }
        }

        public bool TryClaimCompletion()
        {
            return System.Threading.Interlocked.Exchange(ref _isCompleted, 1) == 0;
        }

        public bool IsPlayer(string playerId)
        {
            return playerId == PlayerXId || playerId == PlayerOId;
        }

        // X = 1, O = 2, không phải người chơi = 0
        public int GetPlayerSymbol(string playerId)
        {
            if (playerId == PlayerXId) return 1;
            if (playerId == PlayerOId) return 2;
            return 0;
        }

        public int GetRemainingTimeSeconds()
        {
            return Timer.GetRemainingSeconds();
        }

        public void StopTimer()
        {
            Timer.Stop();
        }

        public void PauseTimer()
        {
            Timer.Pause();
        }

        public void ResumeTimer(Action<int> onTimeout)
        {
            Timer.Resume(onTimeout);
        }

        public GameSessionDto ToDto()
        {
            return new GameSessionDto
            {
                Board = Engine.Board,
                CurrentTurn = Engine.CurrentTurn,
                Status = Engine.Status,
                RemainingTimeSeconds = GetRemainingTimeSeconds(),
                MatchIdentity = MatchId
            };
        }

        public void Dispose()
        {
            Timer.Dispose();
        }
    }
}
