namespace MemoApp.UI.MauiApp.Utilities;

/// <summary>
/// FontAwesome v6 Pro icons with Unicode values
/// </summary>
public static class FontAwesome
{
    /// <summary>
    /// Solid style icons (FontAwesome-Solid font family)
    /// </summary>
    public static class Solid
    {
        public const string Trash = "\uf2ed";                    // Delete/Trash can
        public const string Download = "\uf019";                 // Download arrow
        public const string ArrowsRotate = "\uf021";            // Refresh/Sync
        public const string MagnifyingGlass = "\uf002";         // Search
        public const string Box = "\uf466";                     // Package/Box
        public const string Xmark = "\uf00d";                   // X mark/Close
        public const string Check = "\uf00c";                   // Checkmark
        public const string Plus = "\uf067";                    // Plus sign
        public const string ArrowDown = "\uf063";               // Arrow down
        public const string ArrowUp = "\uf062";                 // Arrow up
        public const string Cog = "\uf013";                     // Settings gear
        public const string CircleInfo = "\uf05a";              // Info circle
        public const string TriangleExclamation = "\uf071";     // Warning
        public const string CircleCheck = "\uf058";             // Success circle
        public const string CircleXmark = "\uf057";             // Error circle
        public const string Spinner = "\uf110";                 // Loading spinner
    }
    
    /// <summary>
    /// Regular style icons (FontAwesome-Regular font family)
    /// </summary>
    public static class Regular
    {
        public const string Trash = "\uf2ed";
        public const string Download = "\uf019";
        public const string ArrowsRotate = "\uf021";
        public const string MagnifyingGlass = "\uf002";
        public const string Box = "\uf466";
        public const string Xmark = "\uf00d";
        public const string Check = "\uf00c";
        public const string Plus = "\uf067";
        public const string CircleInfo = "\uf05a";
    }
    
    /// <summary>
    /// Light style icons (FontAwesome-Light font family) - Pro only
    /// </summary>
    public static class Light
    {
        public const string Trash = "\uf2ed";
        public const string Download = "\uf019";
        public const string ArrowsRotate = "\uf021";
        public const string MagnifyingGlass = "\uf002";
        public const string Box = "\uf466";
        public const string Xmark = "\uf00d";
        public const string Check = "\uf00c";
        public const string Plus = "\uf067";
    }
    
    /// <summary>
    /// Thin style icons (FontAwesome-Thin font family) - Pro only
    /// </summary>
    public static class Thin
    {
        public const string Trash = "\uf2ed";
        public const string Download = "\uf019";
        public const string ArrowsRotate = "\uf021";
        public const string MagnifyingGlass = "\uf002";
        public const string Box = "\uf466";
        public const string Xmark = "\uf00d";
        public const string Check = "\uf00c";
        public const string Plus = "\uf067";
    }
    
    /// <summary>
    /// Font family names for use in XAML
    /// </summary>
    public static class FontFamilies
    {
        public const string Solid = "FontAwesomeSolid";
        public const string Regular = "FontAwesomeRegular";
        public const string Light = "FontAwesomeLight";
        public const string Thin = "FontAwesomeThin";
        public const string Duotone = "FontAwesomeDuotone";
        public const string Brands = "FontAwesomeBrands";
    }
}