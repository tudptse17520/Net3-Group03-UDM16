using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CaroShared.Contracts;
using CaroShared.Enums;
using CaroShared.Protocol;
using CaroClient.Network;

namespace CaroClient
{
    public partial class MatchHistoryForm : CaroForm
    {
        private readonly string _playerName;
        private bool _isLoading = false;
        private readonly SoftLoadingIndicator _historyLoading = new() { Visible = false };
        private TaskCompletionSource? _historyRequest;
        private readonly ToolTip _sharedToolTip = new ToolTip();

        public MatchHistoryForm(string playerName)
        {
            InitializeComponent();
            _historyLoading.SetBounds(BtnRefresh.Left, BtnRefresh.Bottom + 2, BtnRefresh.Width, 10);
            _historyLoading.Anchor = BtnRefresh.Anchor;
            BtnRefresh.Parent!.Controls.Add(_historyLoading);
            _playerName = playerName;
            this.DoubleBuffered = true;

            LblPlayerName.Text = $"Người chơi: {_playerName}";
            _sharedToolTip.SetToolTip(LblPlayerName, $"Người chơi: {_playerName}");

            // Đăng ký sự kiện tô màu kết quả cho DataGridView
            DgvMatchHistory.CellFormatting += DgvMatchHistory_CellFormatting;

            // Đăng ký sự kiện nhận lịch sử đấu từ Server
            NetworkClient.Instance.OnMatchHistoryReceived += OnMatchHistoryReceivedHandler;
            NetworkClient.Instance.OnError += OnErrorHandler;
            NetworkClient.Instance.OnDisconnected += OnDisconnectedHandler;
            this.FormClosed += MatchHistoryForm_FormClosed;

            this.Shown += (_, _) => RefreshMatchHistoryAsync();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using var bgBrush = new SolidBrush(CaroTheme.Background);
            e.Graphics.FillRectangle(bgBrush, e.ClipRectangle);
        }

        private void MatchHistoryForm_FormClosed(object? sender, FormClosedEventArgs e)
        {
            NetworkClient.Instance.OnMatchHistoryReceived -= OnMatchHistoryReceivedHandler;
            NetworkClient.Instance.OnError -= OnErrorHandler;
            NetworkClient.Instance.OnDisconnected -= OnDisconnectedHandler;
            _sharedToolTip.Dispose();
            _historyRequest?.TrySetCanceled();
        }

        private void OnMatchHistoryReceivedHandler(List<MatchDto> matches)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnMatchHistoryReceivedHandler(matches)));
                return;
            }

            PopulateDataGridView(matches);
            CalculateStatistics(matches);
            _historyRequest?.TrySetResult();
            ResetLoadingState();
            
            if (matches.Count == 0)
            {
                ToastNotification.Show(this, "Chưa có trận đấu nào.", ToastType.Info);
            }
        }

        private void OnErrorHandler(Exception ex)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnErrorHandler(ex)));
                return;
            }
            _historyRequest?.TrySetException(ex);
            ResetLoadingState();
        }

        private void OnDisconnectedHandler()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(OnDisconnectedHandler));
                return;
            }
            _historyRequest?.TrySetException(new IOException("Mất kết nối tới máy chủ."));
            ResetLoadingState();
        }

        // ============================
        // Load dữ liệu lịch sử
        // ============================
        private async void RefreshMatchHistoryAsync()
        {
            if (_isLoading) return;

            try
            {
                _isLoading = true;
                _historyLoading.Visible = true;
                _historyRequest = new(TaskCreationOptions.RunContinuationsAsynchronously);
                BtnRefresh.Text = "ĐANG TẢI...";
                BtnRefresh.Enabled = false;

                if (!NetworkClient.Instance.IsConnected)
                {
                    ResetLoadingState();
                    CaroDialogForm.Show(this, "Không thể tải lịch sử trận đấu do mất kết nối.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var request = new MatchHistoryRequest { PlayerId = _playerName };
                var message = new NetworkMessage(MessageType.MatchHistoryRequest, request);
                await NetworkClient.Instance.SendMessageAsync(message);
                await _historyRequest.Task.WaitAsync(TimeSpan.FromSeconds(15));
            }
            catch (Exception ex)
            {
                if (IsDisposed || Disposing) return;
                ResetLoadingState();
                CaroDialogForm.Show(this, $"Lỗi gửi yêu cầu lịch sử: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { if (!IsDisposed) ResetLoadingState(); }
        }

        private void ResetLoadingState()
        {
            _isLoading = false;
            _historyLoading.Visible = false;
            BtnRefresh.Text = "LÀM MỚI";
            BtnRefresh.Enabled = true;
        }

        // ============================
        // Populate DataGridView
        // ============================
        private void PopulateDataGridView(List<MatchDto> matches)
        {
            DgvMatchHistory.Rows.Clear();

            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];

                // Xác định đối thủ và quân cờ của mình
                string opponent;
                string myPiece;

                if (string.Equals(match.PlayerXId, _playerName, StringComparison.OrdinalIgnoreCase))
                {
                    opponent = match.PlayerOId;
                    myPiece = "X";
                }
                else
                {
                    opponent = match.PlayerXId;
                    myPiece = "O";
                }

                // Xác định kết quả hiển thị (WinnerSymbol: 1 = X thắng, 2 = O thắng, 0 = Hòa)
                string resultDisplay;
                if (match.WinnerSymbol == 0)
                {
                    resultDisplay = "Hòa ➖";
                }
                else if ((myPiece == "X" && match.WinnerSymbol == 1) || (myPiece == "O" && match.WinnerSymbol == 2))
                {
                    resultDisplay = "Thắng ✅";
                }
                else
                {
                    resultDisplay = "Thua ❌";
                }

                // Format thời gian
                string startTimeStr = match.PlayedAt.ToString("dd/MM/yyyy HH:mm");
                string durationStr = $"{match.TotalMoves} nước";

                DgvMatchHistory.Rows.Add(
                    (i + 1).ToString(),
                    opponent,
                    myPiece,
                    resultDisplay,
                    startTimeStr,
                    durationStr
                );
            }

            DgvMatchHistory.ClearSelection();
        }

        private void CalculateStatistics(List<MatchDto> matches)
        {
            int total = matches.Count;
            int wins = 0;
            int losses = 0;
            int draws = 0;

            foreach (var match in matches)
            {
                if (match.WinnerSymbol == 0)
                {
                    draws++;
                    continue; // Hòa
                }

                bool isX = string.Equals(match.PlayerXId, _playerName, StringComparison.OrdinalIgnoreCase);
                
                if ((isX && match.WinnerSymbol == 1) || (!isX && match.WinnerSymbol == 2))
                {
                    wins++;
                }
                else
                {
                    losses++;
                }
            }

            LblStatTotalValue.Text = total.ToString();
            LblStatWinValue.Text = wins.ToString();
            LblStatLossValue.Text = losses.ToString();
            LblStatDrawValue.Text = draws.ToString();
        }

        // ============================
        // Tô màu dòng theo phong cách Warm Beige / Restrained Semantic Accents
        // ============================
        private void DgvMatchHistory_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= DgvMatchHistory.Rows.Count)
                return;

            var row = DgvMatchHistory.Rows[e.RowIndex];
            var resultCell = row.Cells["ColResult"];
            if (resultCell.Value == null) return;

            string result = resultCell.Value.ToString() ?? "";

            // Màu cột Kết quả
            if (e.ColumnIndex == DgvMatchHistory.Columns["ColResult"]!.Index)
            {
                if (result.Contains("Thắng"))
                {
                    e.CellStyle!.ForeColor = Color.FromArgb(85, 107, 63); // Muted Forest
                    e.CellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                }
                else if (result.Contains("Thua"))
                {
                    e.CellStyle!.ForeColor = Color.FromArgb(147, 76, 61); // Muted Brick
                    e.CellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                }
                else
                {
                    e.CellStyle!.ForeColor = CaroTheme.TextMuted;
                    e.CellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                }
            }

            // Màu cột Quân cờ
            if (e.ColumnIndex == DgvMatchHistory.Columns["ColPiece"]!.Index)
            {
                string piece = e.Value?.ToString() ?? "";
                if (piece == "X")
                {
                    e.CellStyle!.ForeColor = CaroTheme.XPiece; // Dark Chocolate
                    e.CellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                }
                else if (piece == "O")
                {
                    e.CellStyle!.ForeColor = CaroTheme.WoodFrame; // Warm Wood
                    e.CellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                }
            }
        }

        // ============================
        // Button handlers
        // ============================
        private void BtnRefresh_Click(object? sender, EventArgs e)
        {
            RefreshMatchHistoryAsync();
        }

        private void BtnClose_Click(object? sender, EventArgs e)
        {
            this.Close();
        }
    }
}
