using System;
using System.Threading;

namespace CaroServer.Game
{
    // Bộ đếm giờ cho mỗi lượt đánh
    public class TurnTimer : IDisposable
    {
        private readonly object _lock = new();
        private Timer? _timer;
        private DateTime _deadlineUtc;
        private int _currentTurnId;
        private bool _isDisposed;
        private int _pausedTurnId;
        private int _pausedRemainingSeconds;

        public DateTime DeadlineUtc
        {
            get
            {
                lock (_lock)
                {
                    return _deadlineUtc;
                }
            }
        }

        // Lấy số giây còn lại của lượt hiện tại
        public int GetRemainingSeconds()
        {
            lock (_lock)
            {
                if (_timer == null || _deadlineUtc == DateTime.MinValue)
                {
                    return _pausedRemainingSeconds;
                }

                double remaining = (_deadlineUtc - DateTime.UtcNow).TotalSeconds;
                return remaining > 0 ? (int)Math.Ceiling(remaining) : 0;
            }
        }

        // Bắt đầu đếm giờ cho một lượt mới
        public void StartTurn(int turnId, int durationSeconds, Action<int> onTimeout)
        {
            if (durationSeconds <= 0)
            {
                return;
            }

            lock (_lock)
            {
                if (_isDisposed)
                {
                    return;
                }

                Stop();

                _currentTurnId = turnId;
                _deadlineUtc = DateTime.UtcNow.AddSeconds(durationSeconds);

                int capturedTurnId = turnId;
                _timer = new Timer(_ =>
                {
                    lock (_lock)
                    {
                        if (_isDisposed || _currentTurnId != capturedTurnId)
                        {
                            return;
                        }
                    }

                    onTimeout(capturedTurnId);
                }, null, TimeSpan.FromSeconds(durationSeconds), Timeout.InfiniteTimeSpan);
            }
        }

        // Dừng đếm giờ hiện tại và bỏ trạng thái lượt đang chạy.
        public void Stop()
        {
            lock (_lock)
            {
                _timer?.Dispose();
                _timer = null;
                _deadlineUtc = DateTime.MinValue;
                _currentTurnId = 0;
            }
        }

        // Tạm dừng đồng hồ để trận đấu được giữ nguyên khi Client mất kết nối.
        public void Pause()
        {
            lock (_lock)
            {
                if (_isDisposed || _currentTurnId == 0 || _deadlineUtc == DateTime.MinValue)
                {
                    return;
                }

                _pausedTurnId = _currentTurnId;
                _pausedRemainingSeconds = Math.Max(1, (int)Math.Ceiling((_deadlineUtc - DateTime.UtcNow).TotalSeconds));

                _timer?.Dispose();
                _timer = null;
                _deadlineUtc = DateTime.MinValue;
            }
        }

        // Tiếp tục đồng hồ với số giây còn lại trước khi mất kết nối.
        public void Resume(Action<int> onTimeout)
        {
            lock (_lock)
            {
                if (_isDisposed || _pausedTurnId == 0 || _pausedRemainingSeconds <= 0)
                {
                    return;
                }

                int turnId = _pausedTurnId;
                int remaining = _pausedRemainingSeconds;
                _pausedTurnId = 0;
                _pausedRemainingSeconds = 0;

                _currentTurnId = turnId;
                _deadlineUtc = DateTime.UtcNow.AddSeconds(remaining);
                _timer = new Timer(_ =>
                {
                    lock (_lock)
                    {
                        if (_isDisposed || _currentTurnId != turnId)
                        {
                            return;
                        }
                    }

                    onTimeout(turnId);
                }, null, TimeSpan.FromSeconds(remaining), Timeout.InfiniteTimeSpan);
            }
        }

        // Giải phóng bộ đếm giờ khi không còn sử dụng
        public void Dispose()
        {
            lock (_lock)
            {
                if (_isDisposed)
                {
                    return;
                }

                _isDisposed = true;
                _timer?.Dispose();
                _timer = null;
                _deadlineUtc = DateTime.MinValue;
                _currentTurnId = 0;
                _pausedTurnId = 0;
                _pausedRemainingSeconds = 0;
            }
        }
    }
}
