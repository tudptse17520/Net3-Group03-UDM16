using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CaroClient
{
    public partial class MatchHistoryForm : Form
    {
        private readonly string _playerName;

        public MatchHistoryForm(string playerName)
        {
            InitializeComponent();
            _playerName = playerName;
            this.DoubleBuffered = true;

            LblPlayerName.Text = $"Người chơi: {_playerName}";

            // Đăng ký sự kiện vẽ bo góc cho buttons
            BtnRefresh.Paint += Button_Paint;
            BtnClose.Paint += Button_Paint;

            // Đăng ký sự kiện tô màu kết quả cho DataGridView
            DgvMatchHistory.CellFormatting += DgvMatchHistory_CellFormatting;

            LoadMatchHistory();
        }

        // ============================
        // Load dữ liệu lịch sử
        // ============================
        private void LoadMatchHistory()
        {
            // TODO: Thay bằng dữ liệu thật từ Server khi tích hợp
            // Xem hướng dẫn tích hợp tại walkthrough.md
            var mockData = GetMockData();
            PopulateDataGridView(mockData);
            UpdateStats(mockData);
        }

        // ============================
        // Populate DataGridView
        // ============================
        private void PopulateDataGridView(List<MatchHistoryItem> matches)
        {
            DgvMatchHistory.Rows.Clear();

            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];

                // Xác định đối thủ và quân cờ của mình
                string opponent;
                string myPiece;

                if (string.Equals(match.PlayerXName, _playerName, StringComparison.OrdinalIgnoreCase))
                {
                    opponent = match.PlayerOName;
                    myPiece = "X";
                }
                else
                {
                    opponent = match.PlayerXName;
                    myPiece = "O";
                }

                // Xác định kết quả hiển thị
                string resultDisplay;
                if (string.IsNullOrEmpty(match.WinnerName))
                {
                    resultDisplay = "Hòa ➖";
                }
                else if (string.Equals(match.WinnerName, _playerName, StringComparison.OrdinalIgnoreCase))
                {
                    resultDisplay = "Thắng ✅";
                }
                else
                {
                    resultDisplay = "Thua ❌";
                }

                // Format thời gian
                string startTimeStr = match.StartedAt.ToString("dd/MM/yyyy HH:mm");
                string durationStr = match.Duration.TotalMinutes >= 1
                    ? $"{(int)match.Duration.TotalMinutes} phút {match.Duration.Seconds:00} giây"
                    : $"{match.Duration.Seconds} giây";

                DgvMatchHistory.Rows.Add(
                    (i + 1).ToString(),
                    opponent,
                    myPiece,
                    resultDisplay,
                    startTimeStr,
                    durationStr
                );
            }
        }

        // ============================
        // Cập nhật panel thống kê
        // ============================
        private void UpdateStats(List<MatchHistoryItem> matches)
        {
            int total = matches.Count;
            int wins = 0;
            int losses = 0;

            foreach (var match in matches)
            {
                if (!string.IsNullOrEmpty(match.WinnerName))
                {
                    if (string.Equals(match.WinnerName, _playerName, StringComparison.OrdinalIgnoreCase))
                        wins++;
                    else
                        losses++;
                }
            }

            LblStatTotalValue.Text = total.ToString();
            LblStatWinValue.Text = wins.ToString();
            LblStatLossValue.Text = losses.ToString();
        }

        // ============================
        // Tô màu dòng theo kết quả
        // ============================
        private void DgvMatchHistory_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= DgvMatchHistory.Rows.Count)
                return;

            var row = DgvMatchHistory.Rows[e.RowIndex];
            var resultCell = row.Cells["ColResult"];
            if (resultCell.Value == null) return;

            string result = resultCell.Value.ToString() ?? "";

            if (result.Contains("Thắng"))
            {
                // Highlight nhẹ dòng thắng
                row.DefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#A7F3D0"); // xanh lá nhạt
            }
            else if (result.Contains("Thua"))
            {
                // Highlight nhẹ dòng thua
                row.DefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#FCA5A5"); // đỏ nhạt
            }
            else
            {
                row.DefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#D1D5DB"); // xám nhạt
            }

            // Tô màu đậm hơn cho cột Kết quả
            if (e.ColumnIndex == DgvMatchHistory.Columns["ColResult"]!.Index)
            {
                if (result.Contains("Thắng"))
                {
                    e.CellStyle!.ForeColor = ColorTranslator.FromHtml("#10B981");
                    e.CellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                }
                else if (result.Contains("Thua"))
                {
                    e.CellStyle!.ForeColor = ColorTranslator.FromHtml("#EF4444");
                    e.CellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                }
            }

            // Tô màu cho cột Quân cờ
            if (e.ColumnIndex == DgvMatchHistory.Columns["ColPiece"]!.Index)
            {
                string piece = e.Value?.ToString() ?? "";
                if (piece == "X")
                {
                    e.CellStyle!.ForeColor = ColorTranslator.FromHtml("#60A5FA"); // xanh dương
                    e.CellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                }
                else if (piece == "O")
                {
                    e.CellStyle!.ForeColor = ColorTranslator.FromHtml("#F87171"); // đỏ
                    e.CellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                }
            }
        }

        // ============================
        // Button handlers
        // ============================
        private void BtnRefresh_Click(object? sender, EventArgs e)
        {
            LoadMatchHistory();
        }

        private void BtnClose_Click(object? sender, EventArgs e)
        {
            this.Close();
        }

        // ============================
        // Vẽ bo góc cho buttons (giống LobbyForm)
        // ============================
        private void Button_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is not Button btn) return;

            int borderRadius = 8;
            Rectangle rect = new Rectangle(0, 0, btn.Width - 1, btn.Height - 1);

            using (GraphicsPath path = GetRoundedPath(rect, borderRadius))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                using (SolidBrush brush = new SolidBrush(btn.BackColor))
                {
                    e.Graphics.FillPath(brush, path);
                }

                using (Pen pen = new Pen(ColorTranslator.FromHtml("#E8C37B"), 1.5f))
                {
                    e.Graphics.DrawPath(pen, path);
                }

                TextRenderer.DrawText(
                    e.Graphics,
                    btn.Text,
                    btn.Font,
                    rect,
                    btn.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                );

                btn.Region = new Region(path);
            }
        }

        private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        // ============================
        // Mock Data
        // TODO: Xóa khi tích hợp Server thật
        // ============================
        private List<MatchHistoryItem> GetMockData()
        {
            return new List<MatchHistoryItem>
            {
                new()
                {
                    PlayerXName = _playerName,
                    PlayerOName = "An_Pro_Caro",
                    WinnerName = _playerName,
                    StartedAt = DateTime.Now.AddMinutes(-15),
                    Duration = TimeSpan.FromSeconds(252)
                },
                new()
                {
                    PlayerXName = "Binh_Master",
                    PlayerOName = _playerName,
                    WinnerName = "Binh_Master",
                    StartedAt = DateTime.Now.AddHours(-2),
                    Duration = TimeSpan.FromSeconds(525)
                },
                new()
                {
                    PlayerXName = _playerName,
                    PlayerOName = "Cuong_Noob",
                    WinnerName = _playerName,
                    StartedAt = DateTime.Now.AddDays(-1),
                    Duration = TimeSpan.FromSeconds(110)
                },
                new()
                {
                    PlayerXName = "Duy_Legend",
                    PlayerOName = _playerName,
                    WinnerName = _playerName,
                    StartedAt = DateTime.Now.AddDays(-2),
                    Duration = TimeSpan.FromSeconds(360)
                },
                new()
                {
                    PlayerXName = _playerName,
                    PlayerOName = "Em_NewPlayer",
                    WinnerName = "Em_NewPlayer",
                    StartedAt = DateTime.Now.AddDays(-3),
                    Duration = TimeSpan.FromSeconds(600)
                },
                new()
                {
                    PlayerXName = "Phuc_King",
                    PlayerOName = _playerName,
                    WinnerName = null, // Hòa
                    StartedAt = DateTime.Now.AddDays(-5),
                    Duration = TimeSpan.FromSeconds(900)
                },
            };
        }

        // ============================
        // Internal DTO — chỉ dùng trong CaroClient
        // TODO: Thay bằng MatchHistoryItemDto từ CaroShared khi tích hợp
        // ============================
        private class MatchHistoryItem
        {
            public string PlayerXName { get; set; } = "";
            public string PlayerOName { get; set; } = "";
            public string? WinnerName { get; set; }
            public DateTime StartedAt { get; set; }
            public TimeSpan Duration { get; set; }
        }
    }
}
