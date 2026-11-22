using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace ZallWorldCatalogue;

internal static class BuilderMenu
{
    public static void Open(string builderName = "Robin")
    {
        if (!Context.IsWorldReady) return;
        Game1.activeClickableMenu = new CarpenterMenu(builderName);
    }
}
