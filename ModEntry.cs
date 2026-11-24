using System.IO;
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

    private Texture2D? _shopTexture;
    private Texture2D? _buildTexture;

    public override void Entry(IModHelper helper)
    {
        _shopTexture = LoadOptionalTexture(helper, "assets/shop.png");
        _buildTexture = LoadOptionalTexture(helper, "assets/build.png") ?? _shopTexture;

        helper.Events.Content.AssetRequested += OnAssetRequested;
        helper.Events.Input.ButtonPressed += OnButtonPressed;

        helper.ConsoleCommands.Add("give_shop_station", "Ambil Shop Station.", (_, _) => GiveItem(ShopItemId));
        helper.ConsoleCommands.Add("give_builder_station", "Ambil Builder Station.", (_, _) => GiveItem(BuilderItemId));
    }

    private Texture2D? LoadOptionalTexture(IModHelper helper, string relativePath)
    {
        string fullPath = Path.Combine(helper.DirectoryPath, relativePath);
        if (!File.Exists(fullPath))
        {
            Monitor.Log($"File '{relativePath}' tidak ditemukan di folder mod.", LogLevel.Warn);
            return null;
        }

        try
        {
            return helper.ModContent.Load<Texture2D>(relativePath);
        }
        catch (System.Exception ex)
        {
            Monitor.Log($"Gagal memuat '{relativePath}': {ex.Message}", LogLevel.Error);
            return null;
        }
    }

    private void GiveItem(string id)
    {
        if (!Context.IsWorldReady) return;
        Game1.player.addItemToInventoryBool(ItemRegistry.Create($"(O){id}", 1));
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (_shopTexture != null && e.NameWithoutLocale.IsEquivalentTo("Mods/zall.worldcatalogue/shop"))
        {
            e.LoadFrom(() => _shopTexture, AssetLoadPriority.Medium);
        }

        if (_buildTexture != null && e.NameWithoutLocale.IsEquivalentTo("Mods/zall.worldcatalogue/build"))
        {
            e.LoadFrom(() => _buildTexture, AssetLoadPriority.Medium);
        }

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
                        Texture = _shopTexture != null ? "Mods/zall.worldcatalogue/shop" : null,
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
                        Texture = _buildTexture != null ? "Mods/zall.worldcatalogue/build" : null,
                        SpriteIndex = 0
                    };
                }
            });
        }
    }

    private static bool IsStationId(string? qualifiedId) =>
        qualifiedId == $"(O){ShopItemId}" || qualifiedId == $"(O){BuilderItemId}";

    private StardewValley.Object? FindStationAt(GameLocation location, Vector2 cursorTile, Vector2 grabTile, Vector2 playerTile)
    {
        Vector2[] candidates = { cursorTile, grabTile, playerTile };
        foreach (var tile in candidates)
        {
            if (location.Objects.TryGetValue(tile, out var obj) && IsStationId(obj.QualifiedItemId))
                return obj;
        }
        return null;
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.activeClickableMenu != null) return;
        if (!e.Button.IsActionButton() && !e.Button.IsUseToolButton()) return;

        var location = Game1.currentLocation;
        if (location == null) return;

        Vector2 cursorTile = e.Cursor.Tile;
        Vector2 grabTile = e.Cursor.GrabTile;
        Vector2 playerTile = Game1.player.GetGrabTile();

        // 1. Cek apakah ada station yang dipencet/diklik
        var stationObj = FindStationAt(location, cursorTile, grabTile, playerTile);
        if (stationObj != null)
        {
            Helper.Input.Suppress(e.Button);
            if (stationObj.QualifiedItemId == $"(O){ShopItemId}")
            {
                Game1.activeClickableMenu = new ShopMenu();
            }
            else if (stationObj.QualifiedItemId == $"(O){BuilderItemId}")
            {
                BuilderMenu.Open("Robin");
            }
            return;
        }

        // 2. Kalau sedang memegang station dan tile kosong, letakkan ke lantai
        var held = Game1.player.ActiveObject;
        Vector2 placeTile = grabTile;
        if (held != null && IsStationId(held.QualifiedItemId) && !location.Objects.ContainsKey(placeTile))
        {
            if (location.IsTileOccupiedBy(placeTile, CollisionMask.All))
                return;

            var placed = ItemRegistry.Create<StardewValley.Object>(held.QualifiedItemId, 1);
            placed.TileLocation = placeTile;
            location.Objects[placeTile] = placed;

            held.Stack--;
            if (held.Stack <= 0)
                Game1.player.removeItemFromInventory(held);

            location.playSound("woodyStep");
            Helper.Input.Suppress(e.Button);
        }
    }
}
