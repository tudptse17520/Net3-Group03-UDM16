using System;
using System.Collections.Generic;
using System.Drawing;

namespace CaroClient.Settings
{
    public static class BoardPaletteGenerator
    {
        public static BoardAppearanceSettings GeneratePreset(BoardThemePreset preset)
        {
            var settings = new BoardAppearanceSettings { Theme = preset };

            switch (preset)
            {
                // ================= WOOD =================
                case BoardThemePreset.ClassicWood:
                    settings.Category = BoardThemeCategory.Wood;
                    settings.SurfaceColor = Color.FromArgb(238, 219, 197);
                    settings.GridColor = Color.FromArgb(165, 126, 91);
                    settings.FramePrimaryColor = Color.FromArgb(165, 126, 91);
                    break;
                case BoardThemePreset.LightOak:
                    settings.Category = BoardThemeCategory.Wood;
                    settings.SurfaceColor = Color.FromArgb(245, 230, 210);
                    settings.GridColor = Color.FromArgb(180, 145, 110);
                    settings.FramePrimaryColor = Color.FromArgb(180, 145, 110);
                    break;
                case BoardThemePreset.DarkWalnut:
                    settings.Category = BoardThemeCategory.Wood;
                    settings.SurfaceColor = Color.FromArgb(120, 85, 60);
                    settings.GridColor = Color.FromArgb(60, 40, 25);
                    settings.FramePrimaryColor = Color.FromArgb(80, 55, 40);
                    break;
                case BoardThemePreset.Mahogany:
                    settings.Category = BoardThemeCategory.Wood;
                    settings.SurfaceColor = Color.FromArgb(160, 80, 60);
                    settings.GridColor = Color.FromArgb(100, 40, 30);
                    settings.FramePrimaryColor = Color.FromArgb(120, 50, 40);
                    break;
                case BoardThemePreset.Rosewood:
                    settings.Category = BoardThemeCategory.Wood;
                    settings.SurfaceColor = Color.FromArgb(140, 60, 50);
                    settings.GridColor = Color.FromArgb(80, 25, 20);
                    settings.FramePrimaryColor = Color.FromArgb(100, 35, 30);
                    break;

                // ================= NATURE =================
                case BoardThemePreset.Forest:
                    settings.Category = BoardThemeCategory.Nature;
                    settings.SurfaceColor = Color.FromArgb(190, 220, 190);
                    settings.GridColor = Color.FromArgb(90, 140, 90);
                    settings.FramePrimaryColor = Color.FromArgb(70, 120, 70);
                    break;
                case BoardThemePreset.Moss:
                    settings.Category = BoardThemeCategory.Nature;
                    settings.SurfaceColor = Color.FromArgb(160, 180, 140);
                    settings.GridColor = Color.FromArgb(80, 100, 60);
                    settings.FramePrimaryColor = Color.FromArgb(70, 90, 50);
                    break;
                case BoardThemePreset.Sandstone:
                    settings.Category = BoardThemeCategory.Nature;
                    settings.SurfaceColor = Color.FromArgb(230, 210, 180);
                    settings.GridColor = Color.FromArgb(170, 140, 110);
                    settings.FramePrimaryColor = Color.FromArgb(180, 150, 120);
                    break;

                // ================= OCEAN =================
                case BoardThemePreset.Ocean:
                    settings.Category = BoardThemeCategory.Ocean;
                    settings.SurfaceColor = Color.FromArgb(180, 210, 230);
                    settings.GridColor = Color.FromArgb(80, 130, 170);
                    settings.FramePrimaryColor = Color.FromArgb(60, 110, 150);
                    break;
                case BoardThemePreset.MidnightBlue:
                    settings.Category = BoardThemeCategory.Ocean;
                    settings.SurfaceColor = Color.FromArgb(50, 60, 100);
                    settings.GridColor = Color.FromArgb(20, 25, 50);
                    settings.FramePrimaryColor = Color.FromArgb(30, 40, 70);
                    break;

                // ================= PURPLE =================
                case BoardThemePreset.Lavender:
                    settings.Category = BoardThemeCategory.Purple;
                    settings.SurfaceColor = Color.FromArgb(220, 200, 230);
                    settings.GridColor = Color.FromArgb(140, 100, 160);
                    settings.FramePrimaryColor = Color.FromArgb(120, 80, 140);
                    break;
                case BoardThemePreset.Plum:
                    settings.Category = BoardThemeCategory.Purple;
                    settings.SurfaceColor = Color.FromArgb(120, 70, 120);
                    settings.GridColor = Color.FromArgb(60, 20, 60);
                    settings.FramePrimaryColor = Color.FromArgb(80, 40, 80);
                    break;

                // ================= DARK =================
                case BoardThemePreset.Obsidian:
                    settings.Category = BoardThemeCategory.Dark;
                    settings.SurfaceColor = Color.FromArgb(50, 50, 50);
                    settings.GridColor = Color.FromArgb(20, 20, 20);
                    settings.FramePrimaryColor = Color.FromArgb(35, 35, 35);
                    break;
                case BoardThemePreset.Charcoal:
                    settings.Category = BoardThemeCategory.Dark;
                    settings.SurfaceColor = Color.FromArgb(80, 85, 90);
                    settings.GridColor = Color.FromArgb(40, 45, 50);
                    settings.FramePrimaryColor = Color.FromArgb(60, 65, 70);
                    break;
                case BoardThemePreset.DeepNavy:
                    settings.Category = BoardThemeCategory.Dark;
                    settings.SurfaceColor = Color.FromArgb(30, 40, 60);
                    settings.GridColor = Color.FromArgb(10, 15, 30);
                    settings.FramePrimaryColor = Color.FromArgb(20, 30, 50);
                    break;

                // ================= LIGHT =================
                case BoardThemePreset.Ivory:
                    settings.Category = BoardThemeCategory.Light;
                    settings.SurfaceColor = Color.FromArgb(250, 245, 240);
                    settings.GridColor = Color.FromArgb(200, 190, 180);
                    settings.FramePrimaryColor = Color.FromArgb(220, 210, 200);
                    break;
                case BoardThemePreset.SoftMint:
                    settings.Category = BoardThemeCategory.Light;
                    settings.SurfaceColor = Color.FromArgb(220, 245, 235);
                    settings.GridColor = Color.FromArgb(150, 190, 175);
                    settings.FramePrimaryColor = Color.FromArgb(170, 210, 195);
                    break;

                // For the missing ones, we fallback to a generated default based on their name hash
                default:
                    GenerateDynamicFallback(preset, settings);
                    break;
            }

            // Auto-compute highlight and shadow for the frame
            settings.FrameHighlightColor = Lighten(settings.FramePrimaryColor, 1.2f);
            settings.FrameShadowColor = Darken(settings.FramePrimaryColor, 0.7f);

            return settings;
        }

        private static void GenerateDynamicFallback(BoardThemePreset preset, BoardAppearanceSettings settings)
        {
            // Just simple deterministic logic for the remaining presets to ensure 50+ presets render distinctly
            // without hardcoding 50+ case statements.
            int hash = preset.ToString().GetHashCode();
            
            int r = Math.Abs(hash % 200) + 30; // 30-230
            int g = Math.Abs((hash / 200) % 200) + 30;
            int b = Math.Abs((hash / 40000) % 200) + 30;

            settings.SurfaceColor = Color.FromArgb(r, g, b);
            
            // Generate harmonious grid and frame based on surface
            if (GetBrightness(settings.SurfaceColor) > 130)
            {
                settings.GridColor = Darken(settings.SurfaceColor, 0.6f);
                settings.FramePrimaryColor = Darken(settings.SurfaceColor, 0.7f);
            }
            else
            {
                settings.GridColor = Lighten(settings.SurfaceColor, 1.5f);
                settings.FramePrimaryColor = Lighten(settings.SurfaceColor, 1.3f);
            }

            // Assign category based on preset name
            string name = preset.ToString();
            if (name.Contains("Wood") || name.Contains("Oak") || name.Contains("Maple")) settings.Category = BoardThemeCategory.Wood;
            else if (name.Contains("Pine") || name.Contains("Sage") || name.Contains("Autumn")) settings.Category = BoardThemeCategory.Nature;
            else if (name.Contains("Ocean") || name.Contains("Aqua") || name.Contains("Sky")) settings.Category = BoardThemeCategory.Ocean;
            else if (name.Contains("Rose") || name.Contains("Lilac") || name.Contains("Berry")) settings.Category = BoardThemeCategory.Purple;
            else if (name.Contains("Black") || name.Contains("Midnight") || name.Contains("Dark")) settings.Category = BoardThemeCategory.Dark;
            else settings.Category = BoardThemeCategory.Light;
        }

        public static Color Darken(Color c, float factor)
        {
            return Color.FromArgb(
                c.A,
                Math.Max(0, (int)(c.R * factor)),
                Math.Max(0, (int)(c.G * factor)),
                Math.Max(0, (int)(c.B * factor))
            );
        }

        public static Color Lighten(Color c, float factor)
        {
            return Color.FromArgb(
                c.A,
                Math.Min(255, (int)(c.R * factor)),
                Math.Min(255, (int)(c.G * factor)),
                Math.Min(255, (int)(c.B * factor))
            );
        }

        private static float GetBrightness(Color c)
        {
            return (c.R * 0.299f + c.G * 0.587f + c.B * 0.114f);
        }
    }
}
