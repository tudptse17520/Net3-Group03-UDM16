using System;

namespace CaroClient.Settings
{
    public class PersonalizationPreset
    {
        public string PresetName { get; set; } = "My Preset";
        public BoardAppearanceSettings BoardSettings { get; set; } = new BoardAppearanceSettings();
        public PieceAppearanceSettings PieceSettings { get; set; } = new PieceAppearanceSettings();
    }
}
