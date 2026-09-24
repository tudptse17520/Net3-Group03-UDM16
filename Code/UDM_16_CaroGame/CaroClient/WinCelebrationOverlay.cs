using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CaroClient
{
    /// <summary>
    /// Overlay hiển thị hiệu ứng chiến thắng cao cấp (Shockwave, Burst, Confetti, Sparkles, Firework, Victory Title).
    /// Sử dụng Owned Form không viền trong suốt (TransparencyKey) + WS_EX_TRANSPARENT để truyền chuột 100%.
    /// Quản lý DUY NHẤT 1 Animation Timer (~33ms / 30 FPS), đồng bộ tiến trình với GameBoardForm.
    /// </summary>
    public class WinCelebrationOverlay : CaroForm
    {
        private readonly GameBoardForm _ownerForm;
        private readonly string _winnerName;
        private readonly Point _lastMoveScreenPoint;
        private readonly Rectangle _boardArea;

        private System.Windows.Forms.Timer? _animTimer;
        private int _elapsedMs = 0;
        private const int TotalDurationMs = 3000;
        private readonly System.Diagnostics.Stopwatch _duration = new();
        public long ElapsedMilliseconds => _duration.ElapsedMilliseconds;

        public event Action? CelebrationCompleted;

        // ── Particle Models ──────────────────────────────────────────────────
        private readonly List<CelebrationParticle> _particles = new();
        private readonly List<SparkleEffect> _sparkles = new();
        private bool _burstSpawned = false;
        private bool _confettiSpawned = false;
        private bool _firework1Spawned = false;
        private bool _firework2Spawned = false;

        public WinCelebrationOverlay(GameBoardForm owner, string winnerName, Point lastMoveClientPoint, Rectangle boardArea)
        {
            _ownerForm = owner ?? throw new ArgumentNullException(nameof(owner));
            _winnerName = winnerName ?? "Người chơi";
            _lastMoveScreenPoint = lastMoveClientPoint;
            _boardArea = boardArea;

            // Cấu hình Form trong suốt và không cản trở thao tác chuột
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = Color.Fuchsia;
            this.TransparencyKey = Color.Fuchsia;
            this.DoubleBuffered = true;

            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();

            UpdateBoundsToOwner();

            // Đồng bộ di chuyển / resize / đóng của Form cha
            _ownerForm.Move += Owner_MoveOrResize;
            _ownerForm.Resize += Owner_MoveOrResize;
            _ownerForm.FormClosing += Owner_FormClosing;
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW: không hiện trong Alt+Tab
                return cp;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
        }

        private void UpdateBoundsToOwner()
        {
            if (_ownerForm.IsDisposed || !_ownerForm.IsHandleCreated) return;
            try
            {
                if (_ownerForm.WindowState == FormWindowState.Minimized)
                {
                    this.Visible = false;
                    return;
                }
                var screenPt = _ownerForm.PointToScreen(Point.Empty);
                this.Location = screenPt;
                this.Size = _ownerForm.ClientSize;
                if (!this.Visible && _animTimer != null)
                {
                    this.Visible = true;
                }
            }
            catch
            {
                // Bỏ qua nếu form đang đóng
            }
        }

        private void Owner_MoveOrResize(object? sender, EventArgs e)
        {
            UpdateBoundsToOwner();
            this.Invalidate();
        }

        private void Owner_FormClosing(object? sender, FormClosingEventArgs e)
        {
            StopAndDispose();
        }

        public void Start()
        {
            StopAnimation();
            _elapsedMs = 0;
            _duration.Restart();
            _particles.Clear();
            _sparkles.Clear();
            _burstSpawned = false;
            _confettiSpawned = false;
            _firework1Spawned = false;
            _firework2Spawned = false;

            // Khởi tạo sparkle positions ngẫu nhiên quanh board area
            InitSparkles();

            UpdateBoundsToOwner();
            this.Show(_ownerForm);
            this.BringToFront();

            _animTimer = new System.Windows.Forms.Timer { Interval = 33 };
            _animTimer.Tick += AnimTimer_Tick;
            _animTimer.Start();
        }

        private void InitSparkles()
        {
            var rng = new Random();
            int count = 12;
            int cx = _boardArea.X + _boardArea.Width / 2;
            int cy = _boardArea.Y + _boardArea.Height / 2;

            for (int i = 0; i < count; i++)
            {
                int sx = cx + rng.Next(-_boardArea.Width / 2 + 30, _boardArea.Width / 2 - 30);
                int sy = cy + rng.Next(-_boardArea.Height / 2 + 30, _boardArea.Height / 2 - 30);
                int startT = 550 + i * 110;
                _sparkles.Add(new SparkleEffect
                {
                    X = sx,
                    Y = sy,
                    StartMs = startT,
                    DurationMs = 380,
                    MaxRadius = rng.Next(7, 13)
                });
            }
        }

        private void AnimTimer_Tick(object? sender, EventArgs e)
        {
            _elapsedMs = (int)_duration.ElapsedMilliseconds;

            // 1. T = 450ms: Spawn local burst tại last winning move
            if (_elapsedMs >= 450 && !_burstSpawned)
            {
                _burstSpawned = true;
                SpawnLastMoveBurst();
            }

            // 2. T = 500ms: Spawn confetti
            if (_elapsedMs >= 500 && !_confettiSpawned)
            {
                _confettiSpawned = true;
                SpawnConfetti();
            }

            // 3. T = 700ms: Mini firework 1 (upper-left)
            if (_elapsedMs >= 700 && !_firework1Spawned)
            {
                _firework1Spawned = true;
                SpawnFirework(_boardArea.Left + 50, _boardArea.Top + 60);
            }

            // 4. T = 1000ms: Mini firework 2 (upper-right)
            if (_elapsedMs >= 1000 && !_firework2Spawned)
            {
                _firework2Spawned = true;
                SpawnFirework(_boardArea.Right - 50, _boardArea.Top + 60);
            }

            // 5. Cập nhật physics cho particles
            UpdateParticles();

            // 6. Thông báo GameBoardForm cập nhật pulse và light sweep
            _ownerForm.UpdateCelebrationProgress(_elapsedMs);

            // 7. Yêu cầu vẽ lại overlay
            this.Invalidate();

            // 8. Kết thúc animation
            if (_elapsedMs >= TotalDurationMs)
            {
                StopAnimation();
                this.Hide();
                CelebrationCompleted?.Invoke();
            }
        }

        private void SpawnLastMoveBurst()
        {
            var rng = new Random();
            Color[] burstColors = new[]
            {
                CaroTheme.VictoryGoldLight,
                CaroTheme.VictoryGold,
                CaroTheme.VictoryIvory,
                CaroTheme.VictoryAmber,
                CaroTheme.VictoryCopper
            };

            int count = 18;
            for (int i = 0; i < count; i++)
            {
                double angle = (Math.PI * 2 * i / count) + (rng.NextDouble() * 0.3 - 0.15);
                float speed = (float)(rng.NextDouble() * 3.5 + 2.0);
                float w = rng.Next(6, 9);
                float h = rng.Next(6, 9);
                int life = rng.Next(28, 42);

                _particles.Add(new CelebrationParticle
                {
                    X = _lastMoveScreenPoint.X,
                    Y = _lastMoveScreenPoint.Y,
                    Vx = (float)(Math.Cos(angle) * speed),
                    Vy = (float)(Math.Sin(angle) * speed) - 1.0f,
                    Gravity = 0.12f,
                    Width = w,
                    Height = h,
                    CurrentWidth = w,
                    CurrentHeight = h,
                    TotalLife = life,
                    LifeRemaining = life,
                    Shape = (ParticleShape)rng.Next(0, 3),
                    ParticleColor = burstColors[rng.Next(burstColors.Length)]
                });
            }
        }

        private void SpawnConfetti()
        {
            var rng = new Random();
            Color[] confettiColors = new[]
            {
                CaroTheme.VictoryGold,
                CaroTheme.VictoryGoldLight,
                CaroTheme.VictoryIvory,
                CaroTheme.VictoryAmber,
                CaroTheme.VictoryCopper,
                CaroTheme.ConfettiCream,
                CaroTheme.ConfettiOrange,
                CaroTheme.ConfettiBrown
            };

            int count = 50;
            int minX = Math.Max(0, _boardArea.Left - 40);
            int maxX = Math.Min(this.ClientSize.Width, _boardArea.Right + 40);
            int startY = Math.Max(0, _boardArea.Top - 30);

            for (int i = 0; i < count; i++)
            {
                float w = rng.Next(7, 12);
                float h = rng.Next(5, 8);
                int life = rng.Next(45, 65);

                _particles.Add(new CelebrationParticle
                {
                    X = rng.Next(minX, Math.Max(minX + 1, maxX)),
                    Y = startY + rng.Next(-20, 80),
                    Vx = (float)(rng.NextDouble() * 2.8 - 1.4),
                    Vy = (float)(rng.NextDouble() * 2.5 + 1.2),
                    Gravity = 0.07f,
                    Rotation = rng.Next(0, 360),
                    RotSpeed = (float)(rng.NextDouble() * 8.0 - 4.0),
                    Width = w,
                    Height = h,
                    CurrentWidth = w,
                    CurrentHeight = h,
                    TotalLife = life,
                    LifeRemaining = life,
                    Shape = (ParticleShape)rng.Next(0, 3),
                    ParticleColor = confettiColors[rng.Next(confettiColors.Length)]
                });
            }
        }

        private void SpawnFirework(int cx, int cy)
        {
            var rng = new Random();
            Color[] fireworkColors = new[]
            {
                CaroTheme.VictoryGoldLight,
                CaroTheme.VictoryGoldHi,
                CaroTheme.VictoryAmber,
                CaroTheme.VictoryIvory
            };

            int count = 12;
            for (int i = 0; i < count; i++)
            {
                double angle = (Math.PI * 2 * i / count);
                float speed = (float)(rng.NextDouble() * 2.5 + 1.8);
                float size = rng.Next(5, 8);
                int life = rng.Next(24, 36);

                _particles.Add(new CelebrationParticle
                {
                    X = cx,
                    Y = cy,
                    Vx = (float)(Math.Cos(angle) * speed),
                    Vy = (float)(Math.Sin(angle) * speed),
                    Gravity = 0.08f,
                    Width = size,
                    Height = size,
                    CurrentWidth = size,
                    CurrentHeight = size,
                    TotalLife = life,
                    LifeRemaining = life,
                    Shape = ParticleShape.Circle,
                    ParticleColor = fireworkColors[rng.Next(fireworkColors.Length)]
                });
            }
        }

        private void UpdateParticles()
        {
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                p.X += p.Vx;
                p.Y += p.Vy;
                p.Vy += p.Gravity;
                p.Rotation += p.RotSpeed;
                p.LifeRemaining--;

                // Tự động co nhỏ dần khi sắp hết vòng đời
                float lifeFactor = Math.Clamp((float)p.LifeRemaining / Math.Max(1, p.TotalLife * 0.35f), 0f, 1f);
                p.CurrentWidth = p.Width * lifeFactor;
                p.CurrentHeight = p.Height * lifeFactor;

                if (p.LifeRemaining <= 0 || p.CurrentWidth < 0.5f || p.Y > this.ClientSize.Height + 50)
                {
                    _particles.RemoveAt(i);
                }
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Xóa nền toàn bộ bằng màu TransparencyKey
            using var brush = new SolidBrush(this.TransparencyKey);
            e.Graphics.FillRectangle(brush, this.ClientRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 1. Vẽ Shockwave tại nước đi cuối (T = 400ms – 900ms)
            DrawShockwave(g);

            // 2. Vẽ Particles (Burst, Confetti, Firework)
            DrawParticles(g);

            // 3. Vẽ Sparkles (T = 550ms – 2100ms)
            DrawSparkles(g);

            // 4. Vẽ Center Victory Title (T = 700ms – 2300ms)
            DrawVictoryTitle(g);
        }

        private void DrawShockwave(Graphics g)
        {
            if (_elapsedMs < 380 || _elapsedMs > 900) return;

            float progress = (_elapsedMs - 380) / 520f; // 0.0 -> 1.0
            float radius = 10f + progress * 80f; // 10px -> 90px
            float penWidth = Math.Max(1.0f, 3.2f * (1f - progress * 0.7f));

            var waveRect = new RectangleF(
                _lastMoveScreenPoint.X - radius,
                _lastMoveScreenPoint.Y - radius,
                radius * 2,
                radius * 2);

            using var pen = new Pen(CaroTheme.VictoryGoldLight, penWidth);
            g.DrawEllipse(pen, waveRect);

            // Subtle inner ring
            if (radius > 25 && progress < 0.75f)
            {
                var innerRect = new RectangleF(
                    _lastMoveScreenPoint.X - radius * 0.65f,
                    _lastMoveScreenPoint.Y - radius * 0.65f,
                    radius * 1.3f,
                    radius * 1.3f);
                using var innerPen = new Pen(CaroTheme.VictoryGoldHi, Math.Max(0.8f, penWidth * 0.6f));
                g.DrawEllipse(innerPen, innerRect);
            }
        }

        private void DrawParticles(Graphics g)
        {
            foreach (var p in _particles)
            {
                if (p.CurrentWidth <= 0.5f || p.CurrentHeight <= 0.5f) continue;
                using var brush = new SolidBrush(p.ParticleColor);

                var state = g.Save();
                g.TranslateTransform(p.X, p.Y);
                if (p.Rotation != 0) g.RotateTransform(p.Rotation);

                float hw = p.CurrentWidth / 2f;
                float hh = p.CurrentHeight / 2f;

                switch (p.Shape)
                {
                    case ParticleShape.Rectangle:
                        g.FillRectangle(brush, -hw, -hh, p.CurrentWidth, p.CurrentHeight);
                        break;

                    case ParticleShape.Diamond:
                        PointF[] diamond = new PointF[]
                        {
                            new PointF(0, -hh),
                            new PointF(hw, 0),
                            new PointF(0, hh),
                            new PointF(-hw, 0)
                        };
                        g.FillPolygon(brush, diamond);
                        break;

                    case ParticleShape.Circle:
                    default:
                        g.FillEllipse(brush, -hw, -hh, p.CurrentWidth, p.CurrentHeight);
                        break;
                }

                g.Restore(state);
            }
        }

        private void DrawSparkles(Graphics g)
        {
            foreach (var s in _sparkles)
            {
                if (_elapsedMs < s.StartMs || _elapsedMs > s.StartMs + s.DurationMs) continue;

                float prog = (_elapsedMs - s.StartMs) / (float)s.DurationMs; // 0.0 -> 1.0
                // Sine-based pulse: 0 -> 1 -> 0
                float pulse = (float)Math.Sin(prog * Math.PI);
                if (pulse <= 0.05f) continue;

                float r = s.MaxRadius * pulse;
                using var starBrush = new SolidBrush(CaroTheme.VictoryGoldHi);

                // Vẽ ngôi sao 4 cánh (Four-pointed star)
                PointF[] starPts = new PointF[]
                {
                    new PointF(s.X, s.Y - r),
                    new PointF(s.X + r * 0.22f, s.Y - r * 0.22f),
                    new PointF(s.X + r, s.Y),
                    new PointF(s.X + r * 0.22f, s.Y + r * 0.22f),
                    new PointF(s.X, s.Y + r),
                    new PointF(s.X - r * 0.22f, s.Y + r * 0.22f),
                    new PointF(s.X - r, s.Y),
                    new PointF(s.X - r * 0.22f, s.Y - r * 0.22f)
                };
                g.FillPolygon(starBrush, starPts);
            }
        }

        private void DrawVictoryTitle(Graphics g)
        {
            if (_elapsedMs < 650 || _elapsedMs > 2350) return;

            float scale;
            if (_elapsedMs < 1050)
            {
                // Entrance spring bounce: 650 -> 1050 (400ms)
                float t = (_elapsedMs - 650) / 400f; // 0.0 -> 1.0
                scale = (float)(Math.Sin(t * Math.PI * 0.5) * 1.05);
                if (scale > 1.05f) scale = 1.05f;
            }
            else if (_elapsedMs < 1250)
            {
                // Settle from 1.05 to 1.00 (200ms)
                float t = (_elapsedMs - 1050) / 200f;
                scale = 1.05f - (0.05f * t);
            }
            else if (_elapsedMs > 2050)
            {
                // Exit shrink: 2050 -> 2350 (300ms)
                float t = (_elapsedMs - 2050) / 300f;
                scale = Math.Max(0f, 1.0f - t);
            }
            else
            {
                scale = 1.0f;
            }

            if (scale <= 0.05f) return;

            int maxBannerW = Math.Max(280, _boardArea.Width - 40);
            int baseBannerW = Math.Min(360, maxBannerW);
            int bannerW = (int)(baseBannerW * scale);
            int bannerH = (int)(110 * scale);
            int cx = _boardArea.X + _boardArea.Width / 2;
            int cy = _boardArea.Y + _boardArea.Height / 2;
            var bannerRect = new Rectangle(cx - bannerW / 2, cy - bannerH / 2, bannerW, bannerH);
            if (bannerRect.Width < 20 || bannerRect.Height < 20) return;

            int cornerRadius = Math.Max(6, (int)(18 * scale));
            using (var path = CaroTheme.GetRoundedPath(bannerRect, cornerRadius))
            {
                // Nền thẻ: Màu gỗ tối sang trọng (#3D2015)
                using (var bgBrush = new SolidBrush(Color.FromArgb(255, 61, 32, 21)))
                {
                    g.FillPath(bgBrush, path);
                }

                // Viền vàng kim đôi (Double Gold Border)
                using (var goldPen = new Pen(CaroTheme.VictoryGold, Math.Max(1.5f, 2.4f * scale)))
                {
                    g.DrawPath(goldPen, path);
                }

                if (bannerRect.Width > 20 && bannerRect.Height > 20)
                {
                    var innerRect = new Rectangle(bannerRect.X + 3, bannerRect.Y + 3, bannerRect.Width - 6, bannerRect.Height - 6);
                    int innerRadius = Math.Max(4, cornerRadius - 3);
                    using (var innerPath = CaroTheme.GetRoundedPath(innerRect, innerRadius))
                    using (var innerPen = new Pen(CaroTheme.VictoryGoldLight, Math.Max(1.0f, 1.2f * scale)))
                    {
                        g.DrawPath(innerPen, innerPath);
                    }
                }
            }

            // Chữ "CHIẾN THẮNG!"
            float titleFontSize = Math.Max(10f, 22f * scale);
            using var titleFont = new Font("Segoe UI", titleFontSize, FontStyle.Bold);

            using var titleSf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            };

            // Bóng chữ sô-cô-la đậm
            var shadowRect = new Rectangle(bannerRect.X + 10, bannerRect.Y + (int)(12 * scale) + 2, bannerRect.Width - 20, (int)(42 * scale));
            using (var textShadowBrush = new SolidBrush(Color.FromArgb(255, 25, 12, 6)))
            {
                g.DrawString("CHIẾN THẮNG!", titleFont, textShadowBrush, shadowRect, titleSf);
            }

            // Mặt chữ vàng ngà lấp lánh (VictoryGoldHi)
            var textRect = new Rectangle(bannerRect.X + 10, bannerRect.Y + (int)(12 * scale), bannerRect.Width - 20, (int)(42 * scale));
            using (var textBrush = new SolidBrush(CaroTheme.VictoryGoldHi))
            {
                g.DrawString("CHIẾN THẮNG!", titleFont, textBrush, textRect, titleSf);
            }

            // Subtitle "Chúc mừng <PlayerName>!"
            float subFontSize = Math.Max(8f, 12f * scale);
            using var subFont = new Font("Segoe UI", subFontSize, FontStyle.Bold);
            var subRect = new Rectangle(bannerRect.X + 14, bannerRect.Y + (int)(58 * scale), bannerRect.Width - 28, (int)(34 * scale));
            using (var subBrush = new SolidBrush(CaroTheme.VictoryIvory))
            {
                using var subSf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                g.DrawString($"Chúc mừng {_winnerName}!", subFont, subBrush, subRect, subSf);
            }
        }

        public void StopAndDispose()
        {
            StopAnimation();
            try
            {
                _ownerForm.Move -= Owner_MoveOrResize;
                _ownerForm.Resize -= Owner_MoveOrResize;
                _ownerForm.FormClosing -= Owner_FormClosing;
            }
            catch { }

            if (!this.IsDisposed)
            {
                this.Close();
                this.Dispose();
            }
        }

        private void StopAnimation()
        {
            if (_animTimer != null)
            {
                _animTimer.Stop();
                _animTimer.Tick -= AnimTimer_Tick;
                _animTimer.Dispose();
                _animTimer = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopAnimation();
            }
            base.Dispose(disposing);
        }
    }

    internal enum ParticleShape
    {
        Rectangle,
        Diamond,
        Circle
    }

    internal class CelebrationParticle
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Vx { get; set; }
        public float Vy { get; set; }
        public float Gravity { get; set; }
        public float Rotation { get; set; }
        public float RotSpeed { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float CurrentWidth { get; set; }
        public float CurrentHeight { get; set; }
        public int TotalLife { get; set; }
        public int LifeRemaining { get; set; }
        public ParticleShape Shape { get; set; }
        public Color ParticleColor { get; set; }
    }

    internal class SparkleEffect
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int StartMs { get; set; }
        public int DurationMs { get; set; }
        public int MaxRadius { get; set; }
    }
}
