using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Objects;

namespace ZallWorldCatalogue;

public class ModEntry : Mod
{
    private const string ShopItemId = "zall.worldcatalogue_Shop";
    private const string BuilderItemId = "zall.worldcatalogue_Build";
    private Texture2D? _shopIcon, _builderIcon;

    public override void Entry(IModHelper helper)
    {
        _shopIcon = helper.ModContent.Load<Texture2D>("assets/item.png");
        _builderIcon = helper.ModContent.HasFile("assets/builder.png")
            ? helper.ModContent.Load<Texture2D>("assets/builder.png")
            : _shopIcon;

        helper.Events.Content.AssetRequested += OnAssetRequested;
        helper.Events.Input.ButtonPressed += OnButtonPressed;

        helper.ConsoleCommands.Add("give_shop_station", "Ambil Shop Station.", (_, _) => GiveItem(ShopItemId));
        helper.ConsoleCommands.Add("give_builder_station", "Ambil Builder Station.", (_, _) => GiveItem(BuilderItemId));
    }

    private void GiveItem(string id)
    {
        if (!Context.IsWorldReady) return;
        Game1.player.addItemToInventoryBool(ItemRegistry.Create($"(O){id}", 1));
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Mods/zall.worldcatalogue/shop") && _shopIcon != null)
            e.LoadFrom(() => _shopIcon, AssetLoadPriority.Medium);

        if (e.NameWithoutLocale.IsEquivalentTo("Mods/zall.worldcatalogue/build") && _builderIcon != null)
            e.LoadFrom(() => _builderIcon, AssetLoadPriority.Medium);

        if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
        {
            e.Edit(asset =>
            {
                var data = asset.AsDictionary<string, ObjectData>().Data;

                if (!data.ContainsKey(ShopItemId))
                {
                    data[ShopItemId] = new ObjectData
                    {
                        Name = "ShopStation",
                        DisplayName = "Shop Station",
                        Description = "Letakkan di lantai lalu klik untuk membuka katalog toko.",
                        Type = "Interactive",
                        Category = 0,
                        Price = 2500,
                        Texture = "Mods/zall.worldcatalogue/shop",
                        SpriteIndex = 0
                    };
                }

                if (!data.ContainsKey(BuilderItemId))
                {
                    data[BuilderItemId] = new ObjectData
                    {
                        Name = "BuilderStation",
                        DisplayName = "Builder Station",
                        Description = "Letakkan di lantai lalu klik untuk bangun, upgrade, dan pindah bangunan.",
                        Type = "Interactive",
                        Category = 0,
                        Price = 5000,
                        Texture = "Mods/zall.worldcatalogue/build",
                        SpriteIndex = 0
                    };
                }
            });
        }
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.activeClickableMenu != null || !e.Button.IsActionButton()) return;

        Vector2 tile = e.GrabTile;
        if (!Game1.currentLocation.Objects.TryGetValue(tile, out var obj))
            Game1.currentLocation.Objects.TryGetValue(Game1.player.GetGrabTile(), out obj);
        if (obj == null) return;

        if (obj.QualifiedItemId == $"(O){ShopItemId}")
        {
            Helper.Input.Suppress(e.Button);
            Game1.activeClickableMenu = new ShopMenu();
        }
        else if (obj.QualifiedItemId == $"(O){BuilderItemId}")
        {
            Helper.Input.Suppress(e.Button);
            BuilderMenu.Open("Robin");
        }
    }
}
