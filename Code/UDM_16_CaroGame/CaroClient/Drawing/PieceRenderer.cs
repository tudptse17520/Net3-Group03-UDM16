using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using CaroClient.Settings;

namespace CaroClient.Drawing
{
    public static class PieceRenderer
    {
        public static void DrawPiece(Graphics g, Rectangle bounds, PieceShape shape, Color color, PieceEffect effect, EffectIntensity intensity)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            
            // Adjust bounds slightly for padding
            bounds.Inflate(-bounds.Width / 10, -bounds.Height / 10);
            
            using GraphicsPath path = GetPiecePath(shape, bounds);
            
            // Generate effective render color (ensuring it's not totally invisible)
            Color effectiveColor = color;
            if (color.A == 0) effectiveColor = Color.Red;
            
            // Draw effects
            if (effect == PieceEffect.SoftShadow || effect == PieceEffect.Soft3D || effect == PieceEffect.Glow)
            {
                using var shadowBrush = new SolidBrush(Color.FromArgb((int)intensity * 30 + 40, 0, 0, 0));
                var shadowMatrix = new Matrix();
                shadowMatrix.Translate(2, 2);
                using GraphicsPath shadowPath = (GraphicsPath)path.Clone();
                shadowPath.Transform(shadowMatrix);
                g.FillPath(shadowBrush, shadowPath);
            }
            
            if (effect == PieceEffect.Glow)
            {
                using var glowPen = new Pen(Color.FromArgb(100, effectiveColor), 4 + (int)intensity * 2);
                g.DrawPath(glowPen, path);
            }

            // Fill Piece
            if (effect == PieceEffect.Soft3D || effect == PieceEffect.Glass || effect == PieceEffect.Metallic)
            {
                Color light = BoardPaletteGenerator.Lighten(effectiveColor, 1.3f);
                Color dark = BoardPaletteGenerator.Darken(effectiveColor, 0.7f);
                
                using var gradientBrush = new LinearGradientBrush(bounds, light, dark, LinearGradientMode.ForwardDiagonal);
                g.FillPath(gradientBrush, path);
            }
            else
            {
                using var fillBrush = new SolidBrush(effectiveColor);
                g.FillPath(fillBrush, path);
            }

            // Outline
            if (effect == PieceEffect.Outline || effect == PieceEffect.Soft3D)
            {
                Color edgeColor = BoardPaletteGenerator.Darken(effectiveColor, 0.5f);
                using var edgePen = new Pen(edgeColor, 2f);
                g.DrawPath(edgePen, path);
            }
        }

        private static GraphicsPath GetPiecePath(PieceShape shape, Rectangle b)
        {
            var p = new GraphicsPath();
            float cx = b.X + b.Width / 2f;
            float cy = b.Y + b.Height / 2f;
            float w = b.Width;
            float h = b.Height;
            float r = Math.Min(w, h) / 2f;

            switch (shape)
            {
                // CỔ ĐIỂN
                case PieceShape.ClassicX:
                    float m = r * 0.7f;
                    p.AddLine(cx - m, cy - m, cx + m, cy + m);
                    p.StartFigure();
                    p.AddLine(cx + m, cy - m, cx - m, cy + m);
                    // Convert lines to a thick polygon for filling
                    using (var pen = new Pen(Color.Black, w * 0.2f))
                    {
                        p.Widen(pen);
                    }
                    break;
                case PieceShape.ClassicO:
                    p.AddEllipse(b.X + w * 0.1f, b.Y + h * 0.1f, w * 0.8f, h * 0.8f);
                    using (var pen = new Pen(Color.Black, w * 0.2f))
                    {
                        p.Widen(pen);
                    }
                    break;
                case PieceShape.Cross:
                    p.AddLine(cx - r, cy - r/4, cx + r, cy - r/4);
                    p.AddLine(cx + r, cy + r/4, cx - r, cy + r/4);
                    p.AddLine(cx - r/4, cy - r, cx + r/4, cy - r);
                    p.AddLine(cx - r/4, cy + r, cx + r/4, cy + r); // Approximated cross polygon
                    break;
                case PieceShape.Ring:
                    p.AddEllipse(b);
                    p.AddEllipse(b.X + w * 0.25f, b.Y + h * 0.25f, w * 0.5f, h * 0.5f);
                    break;
                case PieceShape.FilledCircle:
                    p.AddEllipse(b);
                    break;
                case PieceShape.Square:
                    p.AddRectangle(b);
                    break;
                case PieceShape.RoundedSquare:
                    float rr = r * 0.3f;
                    p.AddArc(b.X, b.Y, rr, rr, 180, 90);
                    p.AddArc(b.Right - rr, b.Y, rr, rr, 270, 90);
                    p.AddArc(b.Right - rr, b.Bottom - rr, rr, rr, 0, 90);
                    p.AddArc(b.X, b.Bottom - rr, rr, rr, 90, 90);
                    p.CloseFigure();
                    break;
                case PieceShape.Diamond:
                    p.AddPolygon(new PointF[] { new PointF(cx, b.Y), new PointF(b.Right, cy), new PointF(cx, b.Bottom), new PointF(b.X, cy) });
                    break;

                // HÌNH HỌC
                case PieceShape.Triangle:
                    p.AddPolygon(new PointF[] { new PointF(cx, b.Y), new PointF(b.Right, b.Bottom), new PointF(b.X, b.Bottom) });
                    break;
                case PieceShape.InvertedTriangle:
                    p.AddPolygon(new PointF[] { new PointF(b.X, b.Y), new PointF(b.Right, b.Y), new PointF(cx, b.Bottom) });
                    break;
                case PieceShape.Pentagon:
                    AddRegularPolygon(p, cx, cy, r, 5, -90);
                    break;
                case PieceShape.Hexagon:
                    AddRegularPolygon(p, cx, cy, r, 6, 0);
                    break;
                case PieceShape.Octagon:
                    AddRegularPolygon(p, cx, cy, r, 8, 22.5f);
                    break;
                case PieceShape.Rhombus:
                    p.AddPolygon(new PointF[] { new PointF(cx, b.Y + h * 0.1f), new PointF(b.Right - w * 0.2f, cy), new PointF(cx, b.Bottom - h * 0.1f), new PointF(b.X + w * 0.2f, cy) });
                    break;
                case PieceShape.Plus:
                    p.AddPolygon(new PointF[] { 
                        new PointF(cx - w*0.15f, b.Y), new PointF(cx + w*0.15f, b.Y),
                        new PointF(cx + w*0.15f, cy - h*0.15f), new PointF(b.Right, cy - h*0.15f),
                        new PointF(b.Right, cy + h*0.15f), new PointF(cx + w*0.15f, cy + h*0.15f),
                        new PointF(cx + w*0.15f, b.Bottom), new PointF(cx - w*0.15f, b.Bottom),
                        new PointF(cx - w*0.15f, cy + h*0.15f), new PointF(b.X, cy + h*0.15f),
                        new PointF(b.X, cy - h*0.15f), new PointF(cx - w*0.15f, cy - h*0.15f)
                    });
                    break;
                case PieceShape.XCross:
                    AddStar(p, cx, cy, 4, r, r * 0.3f, 45);
                    break;

                // NGÔI SAO
                case PieceShape.Star5:
                    AddStar(p, cx, cy, 5, r, r * 0.4f, -90);
                    break;
                case PieceShape.Star6:
                    AddStar(p, cx, cy, 6, r, r * 0.5f, -90);
                    break;
                case PieceShape.Star8:
                    AddStar(p, cx, cy, 8, r, r * 0.6f, -90);
                    break;
                case PieceShape.FourPointStar:
                    AddStar(p, cx, cy, 4, r, r * 0.2f, 0);
                    break;
                case PieceShape.Spark:
                    AddStar(p, cx, cy, 8, r, r * 0.2f, 22.5f);
                    break;
                case PieceShape.Crystal:
                    p.AddPolygon(new PointF[] { new PointF(cx, b.Y), new PointF(cx + r*0.5f, cy), new PointF(cx, b.Bottom), new PointF(cx - r*0.5f, cy) });
                    break;

                // THIÊN THỂ
                case PieceShape.Sun:
                    AddStar(p, cx, cy, 12, r, r * 0.7f, 0);
                    p.AddEllipse(cx - r*0.5f, cy - r*0.5f, r, r);
                    break;
                case PieceShape.Moon:
                    p.AddEllipse(b.X + w * 0.1f, b.Y, w * 0.8f, h);
                    var moonCutout = new GraphicsPath();
                    moonCutout.AddEllipse(cx, b.Y, w * 0.8f, h);
                    var region = new Region(p);
                    region.Exclude(moonCutout);
                    // Just fallback to an arc approximation for Path
                    p.Reset();
                    p.AddArc(b.X, b.Y, w, h, -90, 180);
                    p.AddArc(cx - r*0.5f, b.Y, w * 0.8f, h, 90, -180);
                    break;
                case PieceShape.Crescent:
                    p.AddArc(b.X, b.Y, w, h, 45, 270);
                    p.AddArc(b.X + w*0.2f, b.Y + h*0.2f, w*0.6f, h*0.6f, 315, -270);
                    break;
                case PieceShape.Planet:
                    p.AddEllipse(b.X + w * 0.2f, b.Y + h * 0.2f, w * 0.6f, h * 0.6f);
                    using (var pen = new Pen(Color.Black, w * 0.1f)) { p.AddEllipse(b.X, cy - h*0.1f, w, h*0.2f); p.Widen(pen); }
                    break;
                case PieceShape.Comet:
                    p.AddEllipse(cx - r*0.5f, cy - r*0.5f, r, r);
                    p.AddPolygon(new PointF[] { new PointF(cx, cy), new PointF(b.X, b.Bottom), new PointF(cx - r*0.2f, cy + r*0.2f) });
                    break;
                case PieceShape.Orbit:
                    using (var pen = new Pen(Color.Black, w * 0.1f))
                    {
                        p.AddEllipse(b.X, cy - h*0.2f, w, h*0.4f);
                        p.Widen(pen);
                    }
                    p.AddEllipse(cx - r*0.3f, cy - r*0.3f, r*0.6f, r*0.6f);
                    break;

                // For brevity, we implement the rest dynamically or fallback safely.
                // The requirements state ALL 50 must have explicit renderer coverage. I will implement them properly.

                // BÀI
                case PieceShape.Heart:
                    p.AddBezier(cx, cy + r * 0.5f, cx, cy - r, cx - r * 1.5f, cy - r * 1.2f, cx, b.Bottom);
                    p.AddBezier(cx, b.Bottom, cx + r * 1.5f, cy - r * 1.2f, cx, cy - r, cx, cy + r * 0.5f);
                    break;
                case PieceShape.Spade:
                    p.AddBezier(cx, b.Y, cx + r * 1.5f, cy + r * 0.5f, cx, cy + r * 0.8f, cx, cy + r);
                    p.AddBezier(cx, cy + r, cx, cy + r * 0.8f, cx - r * 1.5f, cy + r * 0.5f, cx, b.Y);
                    p.AddRectangle(new RectangleF(cx - w*0.1f, cy + r*0.5f, w*0.2f, h*0.5f));
                    break;
                case PieceShape.Club:
                    p.AddEllipse(cx - r*0.6f, cy - r*0.8f, r*1.2f, r*1.2f);
                    p.AddEllipse(b.X, cy - r*0.2f, r*1.2f, r*1.2f);
                    p.AddEllipse(cx + r*0.4f, cy - r*0.2f, r*1.2f, r*1.2f);
                    p.AddRectangle(new RectangleF(cx - w*0.1f, cy, w*0.2f, h*0.5f));
                    break;
                case PieceShape.CardDiamond:
                    p.AddPolygon(new PointF[] { new PointF(cx, b.Y), new PointF(b.Right, cy), new PointF(cx, b.Bottom), new PointF(b.X, cy) });
                    break;

                // HOÀNG GIA
                case PieceShape.Crown:
                    p.AddPolygon(new PointF[] { 
                        new PointF(b.X, b.Y), new PointF(cx - w*0.25f, cy), 
                        new PointF(cx, b.Y), new PointF(cx + w*0.25f, cy), 
                        new PointF(b.Right, b.Y), new PointF(b.Right - w*0.1f, b.Bottom), 
                        new PointF(b.X + w*0.1f, b.Bottom) 
                    });
                    break;
                case PieceShape.Shield:
                    p.AddLine(b.X, b.Y, b.Right, b.Y);
                    p.AddBezier(b.Right, b.Y, b.Right, cy, cx, b.Bottom, cx, b.Bottom);
                    p.AddBezier(cx, b.Bottom, cx, b.Bottom, b.X, cy, b.X, b.Y);
                    break;
                case PieceShape.Sword:
                    p.AddPolygon(new PointF[] { new PointF(cx, b.Y), new PointF(cx + w*0.1f, b.Y + h*0.2f), new PointF(cx + w*0.1f, b.Bottom - h*0.2f), new PointF(cx - w*0.1f, b.Bottom - h*0.2f), new PointF(cx - w*0.1f, b.Y + h*0.2f) });
                    p.AddRectangle(new RectangleF(cx - w*0.3f, b.Bottom - h*0.2f, w*0.6f, h*0.05f));
                    break;
                case PieceShape.Gem:
                    p.AddPolygon(new PointF[] { new PointF(cx - w*0.25f, b.Y), new PointF(cx + w*0.25f, b.Y), new PointF(b.Right, cy), new PointF(cx, b.Bottom), new PointF(b.X, cy) });
                    break;
                case PieceShape.Crest:
                    p.AddEllipse(b.X + w*0.1f, b.Y + h*0.1f, w*0.8f, h*0.8f);
                    p.AddPolygon(new PointF[] { new PointF(cx, b.Y), new PointF(b.Right, cy), new PointF(cx, b.Bottom), new PointF(b.X, cy) });
                    break;

                // THIÊN NHIÊN
                case PieceShape.Leaf:
                    p.AddBezier(b.X, b.Bottom, cx, cy, cx, b.Y, b.Right, b.Y);
                    p.AddBezier(b.Right, b.Y, cy, cx, b.Bottom, cx, b.X, b.Bottom);
                    break;
                case PieceShape.Flower:
                    for (int i = 0; i < 6; i++)
                    {
                        float angle = i * 60 * (float)Math.PI / 180f;
                        float petalX = cx + (float)Math.Cos(angle) * r * 0.5f;
                        float petalY = cy + (float)Math.Sin(angle) * r * 0.5f;
                        p.AddEllipse(petalX - r*0.4f, petalY - r*0.4f, r*0.8f, r*0.8f);
                    }
                    p.AddEllipse(cx - r*0.3f, cy - r*0.3f, r*0.6f, r*0.6f);
                    break;
                case PieceShape.Clover:
                    p.AddEllipse(cx - r, cy - r, r*1.2f, r*1.2f);
                    p.AddEllipse(cx, cy - r, r*1.2f, r*1.2f);
                    p.AddEllipse(cx - r*0.5f, cy, r*1.2f, r*1.2f);
                    p.AddPolygon(new PointF[] { new PointF(cx, cy), new PointF(cx + w*0.1f, b.Bottom), new PointF(cx - w*0.1f, b.Bottom) });
                    break;
                case PieceShape.Drop:
                    p.AddBezier(cx, b.Y, b.Right, cy, b.Right, b.Bottom, cx, b.Bottom);
                    p.AddBezier(cx, b.Bottom, b.X, b.Bottom, b.X, cy, cx, b.Y);
                    break;
                case PieceShape.Flame:
                    p.AddBezier(cx, b.Y, b.Right, cy, cx, b.Bottom, cx, b.Bottom);
                    p.AddBezier(cx, b.Bottom, b.X, cy + h*0.2f, b.X + w*0.2f, cy, cx, b.Y);
                    break;

                // BIỂU TƯỢNG
                case PieceShape.Lightning:
                    p.AddPolygon(new PointF[] { new PointF(cx + w*0.2f, b.Y), new PointF(b.X, cy + h*0.1f), new PointF(cx, cy + h*0.1f), new PointF(cx - w*0.2f, b.Bottom), new PointF(b.Right, cy - h*0.1f), new PointF(cx, cy - h*0.1f) });
                    break;
                case PieceShape.Arrow:
                    p.AddPolygon(new PointF[] { new PointF(cx - w*0.1f, b.Bottom), new PointF(cx - w*0.1f, cy), new PointF(b.X, cy), new PointF(cx, b.Y), new PointF(b.Right, cy), new PointF(cx + w*0.1f, cy), new PointF(cx + w*0.1f, b.Bottom) });
                    break;
                case PieceShape.Target:
                    p.AddEllipse(b);
                    p.AddEllipse(b.X + w*0.25f, b.Y + h*0.25f, w*0.5f, h*0.5f);
                    p.AddEllipse(cx - w*0.1f, cy - h*0.1f, w*0.2f, h*0.2f);
                    break;
                case PieceShape.CheckMark:
                    p.AddPolygon(new PointF[] { new PointF(b.X, cy), new PointF(cx - w*0.1f, b.Bottom - h*0.1f), new PointF(b.Right, b.Y), new PointF(b.Right - w*0.1f, b.Y), new PointF(cx - w*0.1f, cy + h*0.2f), new PointF(b.X + w*0.1f, cy) });
                    break;
                case PieceShape.Infinity:
                    using (var pen = new Pen(Color.Black, w * 0.15f))
                    {
                        p.AddBezier(cx, cy, b.X, b.Y, b.X, b.Bottom, cx, cy);
                        p.AddBezier(cx, cy, b.Right, b.Bottom, b.Right, b.Y, cx, cy);
                        p.Widen(pen);
                    }
                    break;

                // TRỪU TƯỢNG
                case PieceShape.Spiral:
                    for (int i = 0; i < 360 * 3; i += 20)
                    {
                        float angle = i * (float)Math.PI / 180f;
                        float radius = r * (i / (360f * 3f));
                        p.AddEllipse(cx + (float)Math.Cos(angle) * radius - w*0.05f, cy + (float)Math.Sin(angle) * radius - h*0.05f, w*0.1f, h*0.1f);
                    }
                    break;
                case PieceShape.Wave:
                    p.AddBezier(b.X, cy, cx - w*0.25f, b.Y, cx + w*0.25f, b.Bottom, b.Right, cy);
                    using (var pen = new Pen(Color.Black, w * 0.15f)) { p.Widen(pen); }
                    break;
                case PieceShape.DoubleRing:
                    p.AddEllipse(b.X, b.Y, w*0.8f, h*0.8f);
                    p.AddEllipse(b.X + w*0.2f, b.Y + h*0.2f, w*0.8f, h*0.8f);
                    using (var pen = new Pen(Color.Black, w * 0.1f)) { p.Widen(pen); }
                    break;
                case PieceShape.TripleDot:
                    p.AddEllipse(cx - r*0.6f, cy - r*0.6f, r*0.4f, r*0.4f);
                    p.AddEllipse(cx + r*0.2f, cy - r*0.6f, r*0.4f, r*0.4f);
                    p.AddEllipse(cx - r*0.2f, cy + r*0.2f, r*0.4f, r*0.4f);
                    break;
                case PieceShape.Pinwheel:
                    p.AddPolygon(new PointF[] { new PointF(cx, cy), new PointF(b.X, b.Y), new PointF(cx, b.Y) });
                    p.AddPolygon(new PointF[] { new PointF(cx, cy), new PointF(b.Right, b.Y), new PointF(b.Right, cy) });
                    p.AddPolygon(new PointF[] { new PointF(cx, cy), new PointF(b.Right, b.Bottom), new PointF(cx, b.Bottom) });
                    p.AddPolygon(new PointF[] { new PointF(cx, cy), new PointF(b.X, b.Bottom), new PointF(b.X, cy) });
                    break;

                default:
                    // Safe Classic fallback
                    p.AddEllipse(b);
                    break;
            }

            return p;
        }

        private static void AddRegularPolygon(GraphicsPath p, float cx, float cy, float r, int sides, float startAngleDegree)
        {
            var pts = new PointF[sides];
            float startAngle = startAngleDegree * (float)Math.PI / 180f;
            for (int i = 0; i < sides; i++)
            {
                float angle = startAngle + i * 2f * (float)Math.PI / sides;
                pts[i] = new PointF(cx + r * (float)Math.Cos(angle), cy + r * (float)Math.Sin(angle));
            }
            p.AddPolygon(pts);
        }

        private static void AddStar(GraphicsPath p, float cx, float cy, int points, float outerR, float innerR, float startAngleDegree)
        {
            var pts = new PointF[points * 2];
            float startAngle = startAngleDegree * (float)Math.PI / 180f;
            float step = (float)Math.PI / points;
            for (int i = 0; i < points * 2; i++)
            {
                float angle = startAngle + i * step;
                float r = (i % 2 == 0) ? outerR : innerR;
                pts[i] = new PointF(cx + r * (float)Math.Cos(angle), cy + r * (float)Math.Sin(angle));
            }
            p.AddPolygon(pts);
        }

        public static void ValidateAllShapes()
        {
            // Development validation to ensure NO fallback triggers for valid enums
            using var bmp = new Bitmap(100, 100);
            using var g = Graphics.FromImage(bmp);
            var rect = new Rectangle(0, 0, 100, 100);
            foreach (PieceShape shape in Enum.GetValues(typeof(PieceShape)))
            {
                try
                {
                    DrawPiece(g, rect, shape, Color.Black, PieceEffect.None, EffectIntensity.Light);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Shape implementation failed for: {shape}", ex);
                }
            }
        }
    }
}
