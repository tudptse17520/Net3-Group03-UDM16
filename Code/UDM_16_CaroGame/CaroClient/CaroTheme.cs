using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace CaroClient
{
    /// <summary>
    /// Bảng màu và thông số giao diện chuẩn cho phong cách Warm Beige + Classic Wood + Claymorphism.
    /// Tuân thủ quy tắc GDI: Chỉ lưu trữ hằng số màu sắc, kích thước; không giữ static IDisposable.
    /// </summary>
    public static class CaroTheme
    {
        // ── Form Background ─────────────────────────────────────────────────
        public static readonly Color Background = Color.FromArgb(232, 217, 197); // #E8D9C5

        // ── Card (Soft 3D / Claymorphism) ──────────────────────────────────
        public static readonly Color Card = Color.FromArgb(223, 202, 176);          // #DFCAB0
        public static readonly Color CardHighlight = Color.FromArgb(239, 227, 211); // #EFE3D3
        public static readonly Color CardShadow = Color.FromArgb(138, 96, 69);       // #8A6045
        public static readonly Color CardInnerBox = Color.FromArgb(214, 190, 161);     // #D6BEA1

        // ── Wood Frame ──────────────────────────────────────────────────────
        public static readonly Color WoodFrame = Color.FromArgb(131, 84, 53);        // #835435
        public static readonly Color WoodHighlight = Color.FromArgb(167, 116, 80);   // #A77450
        public static readonly Color WoodShadow = Color.FromArgb(95, 56, 37);         // #5F3825
        public static readonly Color BoardSurface = Color.FromArgb(223, 202, 176);    // #DFCAB0
        public static readonly Color Grid = Color.FromArgb(165, 123, 88);            // #A57B58

        // ── X & O Pieces ────────────────────────────────────────────────────
        public static readonly Color XPiece = Color.FromArgb(61, 32, 21);            // #3D2015
        public static readonly Color OPieceBase = Color.FromArgb(244, 232, 215);     // #F4E8D7
        public static readonly Color OPieceHighlight = Color.FromArgb(255, 245, 232);// #FFF5E8
        public static readonly Color OPieceShadow = Color.FromArgb(157, 118, 88);     // #9D7658
        public static readonly Color OPieceInnerShadow = Color.FromArgb(184, 149, 119); // #B89577

        // ── Buttons ─────────────────────────────────────────────────────────
        public static readonly Color ButtonNormal = Color.FromArgb(158, 108, 70);    // #9E6C46
        public static readonly Color ButtonHover = Color.FromArgb(173, 125, 87);     // #AD7D57
        public static readonly Color ButtonPressed = Color.FromArgb(131, 87, 56);    // #835738
        public static readonly Color ButtonSurrender = Color.FromArgb(126, 80, 49);  // #7E5031
        public static readonly Color ButtonSurrenderHover = Color.FromArgb(142, 92, 58);
        public static readonly Color ButtonSurrenderPressed = Color.FromArgb(110, 68, 40);
        public static readonly Color ButtonText = Color.FromArgb(253, 248, 242);     // #FDF8F2

        // ── Typography Colors ───────────────────────────────────────────────
        public static readonly Color TextDark = Color.FromArgb(51, 32, 24);          // #332018
        public static readonly Color TextMuted = Color.FromArgb(92, 64, 51);         // #5C4033

        // ── Turn Indicators ─────────────────────────────────────────────────
        public static readonly Color BadgeActiveBg = Color.FromArgb(158, 108, 70);   // #9E6C46
        public static readonly Color BadgeActiveText = Color.FromArgb(245, 232, 215);
        public static readonly Color BadgeInactiveBg = Color.FromArgb(205, 178, 151); // #CDB297
        public static readonly Color BadgeInactiveText = Color.FromArgb(96, 68, 49);

        // ── Timer Pill ──────────────────────────────────────────────────────
        public static readonly Color TimerPillBg = Color.FromArgb(185, 140, 104);    // #B98C68
        public static readonly Color TimerPillText = Color.FromArgb(51, 32, 24);     // #332018
        public static readonly Color TimerAlertText = Color.FromArgb(192, 57, 43);

        // ── Win Celebration ──────────────────────────────────────────────────
        public static readonly Color WinGoldGlow = Color.FromArgb(218, 165, 32);       // Warm gold
        public static readonly Color WinGoldLight = Color.FromArgb(255, 223, 120);      // Light gold highlight
        public static readonly Color ConfettiCream = Color.FromArgb(255, 248, 230);     // Cream particle
        public static readonly Color ConfettiOrange = Color.FromArgb(205, 133, 63);     // Muted orange particle
        public static readonly Color ConfettiBrown = Color.FromArgb(160, 100, 60);      // Muted brown particle
        public static readonly Color ConfettiGold = Color.FromArgb(210, 170, 80);       // Gold particle

        // ── Victory Palette (Prompt Specifications) ───────────────────────────
        public static readonly Color VictoryGold = Color.FromArgb(216, 166, 71);        // #D8A647
        public static readonly Color VictoryGoldLight = Color.FromArgb(244, 213, 141);   // #F4D58D
        public static readonly Color VictoryGoldHi = Color.FromArgb(255, 232, 163);      // #FFE8A3
        public static readonly Color VictoryAmber = Color.FromArgb(201, 139, 54);        // #C98B36
        public static readonly Color VictoryWarmGlow = Color.FromArgb(234, 196, 107);    // #EAC46B
        public static readonly Color VictoryIvory = Color.FromArgb(244, 232, 215);       // #F4E8D7
        public static readonly Color VictoryCopper = Color.FromArgb(166, 106, 63);       // #A66A3F

        // ── Backdrop Dim Effect (Modal & Important Notices) ─────────────────
        public static readonly Color BackdropDimBase = Color.FromArgb(51, 32, 24);        // Warm dark-brown (#332018)
        public const double BackdropDimOpacity = 0.22;                                    // ~22% perceived darkness (18–28% range)
        public static readonly Color BackdropDimColor = Color.FromArgb((int)(BackdropDimOpacity * 255), 51, 32, 24);

        // ── Helper: Tạo GraphicsPath bo tròn 4 góc ──────────────────────────
        public static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            int diameter = radius * 2;
            var arc = new Rectangle(rect.X, rect.Y, diameter, diameter);

            // Góc trên - trái
            path.AddArc(arc, 180, 90);

            // Góc trên - phải
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);

            // Góc dưới - phải
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // Góc dưới - trái
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }
    }
}
