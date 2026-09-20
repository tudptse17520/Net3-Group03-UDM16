using System;
using System.Drawing;
using System.Text.Json.Serialization;

namespace CaroClient.Settings
{
    public class BoardAppearanceSettings : ICloneable
    {
        public BoardThemePreset Theme { get; set; } = BoardThemePreset.ClassicWood;
        public BoardThemeCategory Category { get; set; } = BoardThemeCategory.Wood;

        public Color SurfaceColor { get; set; } = Color.FromArgb(238, 219, 197);
        public Color GridColor { get; set; } = Color.FromArgb(165, 126, 91);
        public GridStyle GridStyle { get; set; } = GridStyle.Classic;

        public FrameStyle FrameStyle { get; set; } = FrameStyle.ClassicWood;
        public Color FramePrimaryColor { get; set; } = Color.FromArgb(165, 126, 91);
        public Color FrameHighlightColor { get; set; } = Color.FromArgb(190, 150, 115);
        public Color FrameShadowColor { get; set; } = Color.FromArgb(120, 85, 55);

        // Required for deep copy during Save/Cancel transactions
        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
