using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace LiquidLib
{
    internal class LiquidWorld : ModSystem
    {
        const string TAG_UNLOADED_LIQUIDS = "unloaded_liquids";
        const string TAG_TYPES = "types";
        readonly HashSet<int> list = new();
        readonly HashSet<int> list2 = new();

        public override void SaveWorldData(TagCompound tag)
        {
            var byteArray = new byte[Main.maxTilesX * Main.maxTilesY];
            int index = 0;
            for (int i = 0; i < Main.maxTilesX; i++)
            {
                for (int j = 0; j < Main.maxTilesY; j++)
                {
                    var tile = Main.tile[i, j];

                    byteArray[index++] = (byte)tile.LiquidType;
                    list.Add(tile.LiquidType);

                    if (tile.TileType == LiquidLib.unloadedTileID)
                        list2.Add(tile.TileFrameX);
                }
            }

            var t = new TagCompound();

            foreach (var liquidType in list)
                if (LiquidLoader.liquids.TryGetValue(liquidType, out var modLiquid))
                    t[modLiquid.FullName] = liquidType;

            foreach (var liquidType in list2)
                if (LiquidLoader.unloadedLiquids.TryGetValue(liquidType, out var name))
                    t[name] = liquidType;

            tag[TAG_UNLOADED_LIQUIDS] = t;
            tag[TAG_TYPES] = byteArray;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            if (!tag.ContainsKey(TAG_TYPES) || !tag.ContainsKey(TAG_UNLOADED_LIQUIDS))
                return;

            LiquidLoader.ReloadTypes(tag.GetCompound(TAG_UNLOADED_LIQUIDS));

            var byteArray = (byte[])tag[TAG_TYPES];
            int index = 0;

            for (int i = 0; i < Main.maxTilesX; i++)
            {
                for (int j = 0; j < Main.maxTilesY; j++)
                {
                    var tile = Main.tile[i, j];
                    tile.LiquidType = byteArray[index++];

                    if (!LiquidLoader.liquids.TryGetValue(tile.LiquidType, out _) &&
                        LiquidLoader.unloadedLiquids.TryGetValue(tile.LiquidType, out _)) //Pack
                    {
                        tile.HasTile = true;
                        tile.TileType = LiquidLib.unloadedTileID;
                        tile.TileFrameX = (short)tile.LiquidType;
                        tile.TileFrameY = tile.LiquidAmount;
                        tile.LiquidType = 0;
                        tile.LiquidAmount = 0;
                    }
                    else if (tile.TileType == LiquidLib.unloadedTileID) //UnPack
                    {
                        if (LiquidLoader.unloadedLiquids.TryGetValue(tile.TileFrameX, out var name) &&
                            LiquidLoader.liquids.TryGetValue(name, out _))
                        {
                            tile.HasTile = false;
                            tile.TileType = 0;
                            tile.LiquidType = tile.TileFrameX;
                            tile.LiquidAmount = (byte)tile.TileFrameY;
                            tile.TileFrameX = 0;
                            tile.TileFrameY = 0;
                        }
                    }
                }
            }
        }
    }
}