namespace CityBuilder.Presentation;

/// <summary>
/// The approved visual identity ("Aegean Marble", option 1a) as engine-neutral design tokens.
/// Source of truth: docs/design/main-menu/README.md — every value here mirrors that handoff so
/// any frontend (Unity, Godot, web) recreates the menus pixel-perfectly from ONE place.
/// Colors are <see cref="Color32"/>; typography/spacing are plain numbers (px at 1× UI scale).
/// </summary>
public static class AegeanMarbleTheme
{
    // --- Surfaces ---

    /// <summary>Parchment panel surface (#F7F3E9 at 96% opacity over the scene).</summary>
    public static readonly Color32 Parchment = new(247, 243, 233, 245);

    /// <summary>Input field background (#FFFDF6).</summary>
    public static readonly Color32 InputBackground = new(255, 253, 246);

    /// <summary>Modal scrim rgba(38,34,24,0.45).</summary>
    public static readonly Color32 ModalScrim = new(38, 34, 24, 115);

    // --- Text ---

    /// <summary>Ink for headings (#232A35).</summary>
    public static readonly Color32 Ink = new(0x23, 0x2A, 0x35);

    /// <summary>Body/menu text (#4A4331); darkens to <see cref="InkHover"/> on hover.</summary>
    public static readonly Color32 BodyText = new(0x4A, 0x43, 0x31);

    public static readonly Color32 InkHover = new(0x1D, 0x25, 0x31);

    /// <summary>Muted label gold-brown (#8A7A4D) — uppercase field labels, the title kicker.</summary>
    public static readonly Color32 Label = new(0x8A, 0x7A, 0x4D);

    /// <summary>Footer/meta text (#97896B).</summary>
    public static readonly Color32 Meta = new(0x97, 0x89, 0x6B);

    /// <summary>Secondary text (#6B6250) — save-row metadata.</summary>
    public static readonly Color32 Secondary = new(0x6B, 0x62, 0x50);

    /// <summary>Secondary text variant (#6D6449) — the tagline.</summary>
    public static readonly Color32 Tagline = new(0x6D, 0x64, 0x49);

    /// <summary>Input text (#2B3140).</summary>
    public static readonly Color32 InputText = new(0x2B, 0x31, 0x40);

    // --- Accents & borders ---

    /// <summary>Accent gold (#A08339) — rules, diamonds, heading underlines.</summary>
    public static readonly Color32 AccentGold = new(0xA0, 0x83, 0x39);

    /// <summary>Hairline gold (#B9A878) — hover borders.</summary>
    public static readonly Color32 HairlineGold = new(0xB9, 0xA8, 0x78);

    /// <summary>Panel border (#CBBF9F).</summary>
    public static readonly Color32 PanelBorder = new(0xCB, 0xBF, 0x9F);

    /// <summary>Row divider (#DDD3B8).</summary>
    public static readonly Color32 RowDivider = new(0xDD, 0xD3, 0xB8);

    /// <summary>Input border (#C9BD9E).</summary>
    public static readonly Color32 InputBorder = new(0xC9, 0xBD, 0x9E);

    /// <summary>Map-size card border (#B9AB80).</summary>
    public static readonly Color32 CardBorder = new(0xB9, 0xAB, 0x80);

    // --- Buttons ---

    /// <summary>Primary action deep Aegean blue (#24506E); also LOAD outline color + control accent.</summary>
    public static readonly Color32 PrimaryBlue = new(0x24, 0x50, 0x6E);

    /// <summary>Primary button text (#F2EEE1).</summary>
    public static readonly Color32 PrimaryText = new(0xF2, 0xEE, 0xE1);

    /// <summary>Primary hover (#1B3E57).</summary>
    public static readonly Color32 PrimaryHover = new(0x1B, 0x3E, 0x57);

    /// <summary>Selected map-size card text (#F4EFE4).</summary>
    public static readonly Color32 SelectedCardText = new(0xF4, 0xEF, 0xE4);

    /// <summary>Secondary button border (#A5966D) and text (#6B6046).</summary>
    public static readonly Color32 SecondaryBorder = new(0xA5, 0x96, 0x6D);

    public static readonly Color32 SecondaryText = new(0x6B, 0x60, 0x46);

    public static readonly Color32 SecondaryHoverText = new(0x4A, 0x40, 0x30);

    // --- Typography (family names are Google Fonts, OFL-licensed) ---

    public const string DisplayFontFamily = "Marcellus"; // 400 only
    public const string BodyFontFamily = "Lora";         // 400/500/600 + italic
    public const string FallbackFontFamily = "EB Garamond";

    public const float TitleSize = 104f;
    public const float TitleTracking = 0.12f;       // em
    public const float PanelHeadingSize = 30f;
    public const float PanelHeadingTracking = 0.04f;
    public const float SectionHeadingSize = 17f;
    public const float SaveNameSize = 19f;
    public const float BodySizeMin = 14f;
    public const float BodySizeMax = 16f;
    public const float LabelSize = 11f;             // uppercase
    public const float LabelTracking = 0.18f;
    public const float MenuItemSize = 14f;          // uppercase
    public const float MenuItemTracking = 0.22f;
    public const float FooterSize = 10f;            // uppercase
    public const float FooterTracking = 0.24f;
    public const float KickerSize = 13f;
    public const float KickerTracking = 0.5f;

    // --- Shape & layout metrics (px at 1× UI scale) ---

    public const float PanelCornerRadius = 3f;      // 2–4px, nearly square
    public const float ControlCornerRadius = 2f;
    public const float PanelPaddingVertical = 36f;
    public const float PanelPaddingHorizontal = 42f;
    public const float HeadingDividerWidth = 56f;
    public const float HeadingDividerHeight = 2f;
    public const float NewCityPanelWidth = 560f;
    public const float LoadPanelWidth = 640f;
    public const float SettingsPanelWidth = 620f;
    public const float MenuItemWidth = 280f;
    public const float MinTouchTarget = 44f;

    /// <summary>Screen transitions: fade + 10px rise.</summary>
    public const float TransitionRisePx = 10f;
    public const int TransitionMsMin = 300;
    public const int TransitionMsMax = 400;
}
