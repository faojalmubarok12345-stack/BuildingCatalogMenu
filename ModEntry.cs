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
        _shopIcon = helper.ModContent.Load<Texture2D>("assets/shop.png");
        _builderIcon = helper.ModContent.Load<Texture2D>("assets/build.png");

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
                        Type = "Basic",
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
                        Type = "Basic",
                        Category = 0,
                        Price = 5000,
                        Texture = "Mods/zall.worldcatalogue/build",
                        SpriteIndex = 0
                    };
                }
            });
        }
    }

    private bool IsStationId(string? qualifiedId) =>
        qualifiedId == $"(O){ShopItemId}" || qualifiedId == $"(O){BuilderItemId}";

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.activeClickableMenu != null) return;
        if (!e.Button.IsActionButton() && !e.Button.IsUseToolButton()) return;

        Vector2 tile = e.Cursor.GrabTile;
        if (!Game1.currentLocation.Objects.TryGetValue(tile, out var existingObj))
        {
            tile = Game1.player.GetGrabTile();
            Game1.currentLocation.Objects.TryGetValue(tile, out existingObj);
        }

        // 1. Kalau di tile tersebut sudah ada station kita, klik untuk buka menu
        if (existingObj != null && IsStationId(existingObj.QualifiedItemId) && e.Button.IsActionButton())
        {
            Helper.Input.Suppress(e.Button);
            if (existingObj.QualifiedItemId == $"(O){ShopItemId}")
                Game1.activeClickableMenu = new ShopMenu();
            else
                BuilderMenu.Open("Robin");
            return;
        }

        // 2. Kalau pemain sedang memegang station di tangan dan tile kosong, letakkan ke lantai
        var held = Game1.player.ActiveObject;
        if (held != null && IsStationId(held.QualifiedItemId) && existingObj == null)
        {
            if (Game1.currentLocation.IsTileOccupiedBy(tile))
                return;

            var placed = (StardewValley.Object)ItemRegistry.Create(held.QualifiedItemId, 1);
            placed.TileLocation = tile;
            Game1.currentLocation.Objects[tile] = placed;

            held.Stack--;
            if (held.Stack <= 0)
                Game1.player.removeItemFromInventory(held);

            Game1.currentLocation.playSound("woodyStep");
            Helper.Input.Suppress(e.Button);
        }
    }
}
