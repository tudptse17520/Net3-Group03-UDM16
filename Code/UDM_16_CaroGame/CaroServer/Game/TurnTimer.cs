using System;
using System.Threading;

namespace CaroServer.Game
{
    public class TurnTimer : IDisposable
    {
        private readonly object _lock = new();
        private Timer? _timer;
        private DateTime _deadlineUtc;
        private int _currentTurnId;
        private bool _isDisposed;

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

        public int GetRemainingSeconds()
        {
            lock (_lock)
            {
                if (_timer == null || _deadlineUtc == DateTime.MinValue)
                {
                    return 0;
                }

                double remaining = (_deadlineUtc - DateTime.UtcNow).TotalSeconds;
                return remaining > 0 ? (int)Math.Ceiling(remaining) : 0;
            }
        }

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

        public void Stop()
        {
            lock (_lock)
            {
                _timer?.Dispose();
                _timer = null;
                _deadlineUtc = DateTime.MinValue;
            }
        }

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
            }
        }
    }
}
