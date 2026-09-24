using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CaroClient.Settings
{
    public class PlayerPersonalizationSettings : ICloneable
    {
        public int Version { get; set; } = 1;

        public BoardAppearanceSettings BoardAppearance { get; set; } = new BoardAppearanceSettings();
        public PieceAppearanceSettings PieceAppearance { get; set; } = new PieceAppearanceSettings();

        // Custom local presets (up to 20)
        public List<PersonalizationPreset> CustomPresets { get; set; } = new List<PersonalizationPreset>();

        public object Clone()
        {
            var clone = (PlayerPersonalizationSettings)this.MemberwiseClone();
            clone.BoardAppearance = (BoardAppearanceSettings)this.BoardAppearance.Clone();
            clone.PieceAppearance = (PieceAppearanceSettings)this.PieceAppearance.Clone();
            clone.CustomPresets = new List<PersonalizationPreset>();
            foreach (var preset in this.CustomPresets)
            {
                clone.CustomPresets.Add(new PersonalizationPreset
                {
                    PresetName = preset.PresetName,
                    BoardSettings = (BoardAppearanceSettings)preset.BoardSettings.Clone(),
                    PieceSettings = (PieceAppearanceSettings)preset.PieceSettings.Clone()
                });
            }
            return clone;
        }
    }
}
