using System;
using System.Drawing;

namespace CaroClient.Settings
{
    public class PieceAppearanceSettings : ICloneable
    {
        public PieceShape MyShape { get; set; } = PieceShape.ClassicX;
        public PieceShape OpponentShape { get; set; } = PieceShape.ClassicO;

        public Color MyColor { get; set; } = Color.FromArgb(220, 53, 69); // Bootstrap Red
        public Color OpponentColor { get; set; } = Color.FromArgb(13, 110, 253); // Bootstrap Blue

        public PieceEffect Effect { get; set; } = PieceEffect.Soft3D;
        public EffectIntensity Intensity { get; set; } = EffectIntensity.Medium;

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
