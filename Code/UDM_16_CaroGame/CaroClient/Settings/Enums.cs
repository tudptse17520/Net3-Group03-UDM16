using System;

namespace CaroClient.Settings
{
    public enum PieceShape
    {
        // CỔ ĐIỂN
        ClassicX,
        ClassicO,
        Cross,
        Ring,
        FilledCircle,
        Square,
        RoundedSquare,
        Diamond,

        // HÌNH HỌC
        Triangle,
        InvertedTriangle,
        Pentagon,
        Hexagon,
        Octagon,
        Rhombus,
        Plus,
        XCross,

        // NGÔI SAO
        Star5,
        Star6,
        Star8,
        FourPointStar,
        Spark,
        Crystal,

        // THIÊN THỂ
        Sun,
        Moon,
        Crescent,
        Planet,
        Comet,
        Orbit,

        // BÀI
        Heart,
        Spade,
        Club,
        CardDiamond,

        // HOÀNG GIA
        Crown,
        Shield,
        Sword,
        Gem,
        Crest,

        // THIÊN NHIÊN
        Leaf,
        Flower,
        Clover,
        Drop,
        Flame,

        // BIỂU TƯỢNG
        Lightning,
        Arrow,
        Target,
        CheckMark,
        Infinity,

        // TRỪU TƯỢNG
        Spiral,
        Wave,
        DoubleRing,
        TripleDot,
        Pinwheel
    }

    public enum PieceEffect
    {
        None,
        Matte,
        Outline,
        SoftShadow,
        Soft3D,
        Glow,
        Glass,
        Metallic
    }

    public enum EffectIntensity
    {
        Light,  // NHẸ
        Medium, // VỪA
        High    // RÕ
    }

    public enum BoardThemeCategory
    {
        Wood,      // GỖ
        Nature,    // THIÊN NHIÊN
        Ocean,     // LẠNH / OCEAN
        Purple,    // TÍM / HỒNG
        Dark,      // TỐI
        Light      // SÁNG
    }

    public enum BoardThemePreset
    {
        // Wood
        ClassicWood, LightOak, HoneyOak, GoldenOak, Maple, Bamboo, Walnut, DarkWalnut, Mahogany, Rosewood, VintageChess, RusticWood,
        
        // Nature
        Forest, Pine, Moss, Sage, Olive, Emerald, Autumn, Sandstone, Desert, Terracotta,
        
        // Ocean
        Ocean, DeepSea, Aqua, Teal, Glacier, Arctic, MistBlue, Sky, Navy, MidnightBlue, Storm,
        
        // Purple
        Lavender, Lilac, Mauve, Plum, Grape, DeepPurple, Rose, DustyRose, Sakura, Berry, Wine, Orchid,
        
        // Dark
        Charcoal, Graphite, Obsidian, Midnight, Espresso, DarkCocoa, BlackGold, DeepForest, DeepNavy, Slate,
        
        // Light
        Ivory, Cream, Vanilla, Linen, Pearl, WarmGray, SoftPeach, SoftMint, SoftBlue, SoftLavender
    }

    public enum GridStyle
    {
        Classic,
        Thin,
        Soft,
        Medium,
        Subtle
    }

    public enum FrameStyle
    {
        ClassicWood,
        Soft3D,
        Flat,
        DarkEdge,
        LightEdge
    }
}
