using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.RuntimeDetour.HookGen;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace LiquidLib
{
    internal static class OnHooks
    {
        static event hook_OnInitialize OnInitialize
        {
            add => HookEndpointManager.Add(typeof(Mod).Assembly
                    .GetType("Terraria.ModLoader.UI.UIModItem")
                    .GetMethod("OnInitialize", BindingFlags.Public | BindingFlags.Instance), value);
            remove => HookEndpointManager.Remove(typeof(Mod).Assembly
                    .GetType("Terraria.ModLoader.UI.UIModItem")
                    .GetMethod("OnInitialize", BindingFlags.Public | BindingFlags.Instance), value);
        }
        delegate void hook_OnInitialize(orig_OnInitialize orig, object self);
        delegate void orig_OnInitialize(object self);

        static event hook_RandomUpdate RandomUpdate
        {
            add => HookEndpointManager.Add(typeof(TileLoader)
                    .GetMethod(nameof(TileLoader.RandomUpdate), BindingFlags.Public | BindingFlags.Static), value);
            remove => HookEndpointManager.Remove(typeof(TileLoader)
                    .GetMethod(nameof(TileLoader.RandomUpdate), BindingFlags.Public | BindingFlags.Static), value);
        }
        delegate void hook_RandomUpdate(orig_RandomUpdate orig, int i, int j, int type);
        delegate void orig_RandomUpdate(int i, int j, int type);

        public static void Load()
        {
            On.Terraria.Tile.liquidType += Tile_liquidType;
            On.Terraria.Tile.liquidType_int += Tile_liquidType_int;
			On.Terraria.Tile.lava += Tile_lava;
            On.Terraria.Tile.lava_bool += Tile_lava_bool;
            On.Terraria.Tile.honey += Tile_honey;
            On.Terraria.Tile.honey_bool += Tile_honey_bool;
            //HookEndpointManager.Add(typeof(Tile).GetMethod("get_LiquidType"), get_LiquidType);
            //HookEndpointManager.Add(typeof(Tile).GetMethod("set_LiquidType"), set_LiquidType);
            On.Terraria.Main.DoUpdate += Main_DoUpdate;
            On.Terraria.Liquid.Update += Liquid_Update;
            On.Terraria.Liquid.LavaCheck += Liquid_LavaCheck;
            On.Terraria.Liquid.HoneyCheck += Liquid_HoneyCheck;
            On.Terraria.NetMessage.CompressTileBlock_Inner += NetMessage_CompressTileBlock_Inner;
            On.Terraria.NetMessage.DecompressTileBlock_Inner += NetMessage_DecompressTileBlock_Inner;
            OnInitialize += OnHooks_OnInitialize;
            On.Terraria.Wiring.XferWater += Wiring_XferWater;
            On.Terraria.IO.WorldFile.SaveWorld_Version2 += WorldFile_SaveWorld_Version2;
            RandomUpdate += OnHooks_RandomUpdate;
        }

		public static void Unload()
        {
            On.Terraria.Tile.liquidType -= Tile_liquidType;
            On.Terraria.Tile.liquidType_int -= Tile_liquidType_int;
            On.Terraria.Tile.lava -= Tile_lava;
            On.Terraria.Tile.lava_bool -= Tile_lava_bool;
            On.Terraria.Tile.honey -= Tile_honey;
            On.Terraria.Tile.honey_bool -= Tile_honey_bool;
            //HookEndpointManager.Remove(typeof(Tile).GetMethod("get_LiquidType"), get_LiquidType);
            //HookEndpointManager.Remove(typeof(Tile).GetMethod("set_LiquidType"), set_LiquidType);
            On.Terraria.Main.DoUpdate -= Main_DoUpdate;
            On.Terraria.Liquid.Update -= Liquid_Update;
            On.Terraria.Liquid.LavaCheck -= Liquid_LavaCheck;
            On.Terraria.Liquid.HoneyCheck -= Liquid_HoneyCheck;
            On.Terraria.NetMessage.CompressTileBlock_Inner -= NetMessage_CompressTileBlock_Inner;
            On.Terraria.NetMessage.DecompressTileBlock_Inner -= NetMessage_DecompressTileBlock_Inner;
            OnInitialize -= OnHooks_OnInitialize;
            On.Terraria.Wiring.XferWater -= Wiring_XferWater;
            On.Terraria.IO.WorldFile.SaveWorld_Version2 -= WorldFile_SaveWorld_Version2;
            RandomUpdate -= OnHooks_RandomUpdate;
        }

		static byte Tile_liquidType(On.Terraria.Tile.orig_liquidType orig, ref Tile self) =>
            GetLiquidType(ref self);

        static void Tile_liquidType_int(On.Terraria.Tile.orig_liquidType_int orig, ref Tile self, int liquidType) =>
            SetLiquidType(ref self, (byte)liquidType);

        static bool Tile_lava(On.Terraria.Tile.orig_lava orig, ref Tile self) =>
            GetLiquidType(ref self) == LiquidID.Lava;

        static void Tile_lava_bool(On.Terraria.Tile.orig_lava_bool orig, ref Tile self, bool lava)
        {
            if (lava)
                SetLiquidType(ref  self, LiquidID.Lava);
            else
                SetLiquidType(ref self, 0);
        }

        static bool Tile_honey(On.Terraria.Tile.orig_honey orig, ref Tile self) =>
            GetLiquidType(ref self) == LiquidID.Honey;

        static void Tile_honey_bool(On.Terraria.Tile.orig_honey_bool orig, ref Tile self, bool honey)
        {
            if (honey)
                SetLiquidType(ref self, LiquidID.Honey);
            else
                SetLiquidType(ref self, 0);
        }

        static Func<Tile, int> get_LiquidType =
            (tile) => GetLiquidType(ref tile);

        static Action<Tile, int> set_LiquidType =
            (tile, type) => SetLiquidType(ref tile, (byte)type);

        static void Main_DoUpdate(On.Terraria.Main.orig_DoUpdate orig, Main self, ref GameTime gameTime)
        {
            orig(self, ref gameTime);
            LiquidLoader.OnUpdate();
        }

        static void Liquid_Update(On.Terraria.Liquid.orig_Update orig, Liquid self)
        {
            if (LiquidLoader.OnUpdate(self))
                orig(self);
        }

        static void Liquid_HoneyCheck(On.Terraria.Liquid.orig_HoneyCheck orig, int x, int y)
        {
        }

        static void Liquid_LavaCheck(On.Terraria.Liquid.orig_LavaCheck orig, int x, int y)
        {
        }

        static void NetMessage_CompressTileBlock_Inner(On.Terraria.NetMessage.orig_CompressTileBlock_Inner orig, BinaryWriter writer, int xStart, int yStart, int width, int height)
        {
            orig(writer, xStart, yStart, width, height);

            var byteArray = new byte[width * height];
            int index = 0;

            for (int i = xStart; i < xStart + width; i++)
                for (int j = yStart; j < yStart + height; j++)
                    byteArray[index++] = (byte)Main.tile[i, j].LiquidType;
            
            writer.Write(byteArray);
        }

        static void NetMessage_DecompressTileBlock_Inner(On.Terraria.NetMessage.orig_DecompressTileBlock_Inner orig, BinaryReader reader, int xStart, int yStart, int width, int height)
        {
            orig(reader, xStart, yStart, width, height);

            var byteArray = reader.ReadBytes(width * height);
            int index = 0;

            for (int i = xStart; i < xStart + width; i++)
            {
                for (int j = yStart; j < yStart + height; j++)
                {
                    Tile tile = Main.tile[i, j];
                    tile.LiquidType = byteArray[index++];
                }
            }
        }

        static void OnHooks_OnInitialize(orig_OnInitialize orig, object self)
        {
            orig(self);
            var ass = typeof(Mod).Assembly;
            var type_UIModItem = ass.GetType("Terraria.ModLoader.UI.UIModItem");
            string ModName = (string)type_UIModItem.GetProperty("ModName", BindingFlags.Public | BindingFlags.Instance).GetValue(self);

            if (ModLoader.TryGetMod(ModName, out var loadedMod))
            {
                int liquidCount = loadedMod.GetContent<ModLiquid>().Count();

                if (liquidCount > 0)
                {
                    int baseOffset = -40;
                    void ChangeOffset(int modCount)
                    {
                        if (modCount > 0)
                            baseOffset -= 18;
                    }
                    ChangeOffset(loadedMod.GetContent<ModItem>().Count());
                    ChangeOffset(loadedMod.GetContent<ModNPC>().Count());
                    ChangeOffset(loadedMod.GetContent<ModTile>().Count());
                    ChangeOffset(loadedMod.GetContent<ModWall>().Count());
                    ChangeOffset(loadedMod.GetContent<ModBuff>().Count());
                    ChangeOffset(loadedMod.GetContent<ModMount>().Count());

                    var type_UIHoverImage = ass.GetType("Terraria.ModLoader.UI.UIHoverImage");
                    var UIHoverImage = Activator.CreateInstance(type_UIHoverImage, Main.Assets.Request<Texture2D>(TextureAssets.InfoIcon[6].Name), liquidCount + " liquids");
                    var field_Left = type_UIHoverImage.GetField("Left", BindingFlags.Public | BindingFlags.Instance);
                    field_Left.SetValue(UIHoverImage, new StyleDimension { Percent = 1f, Pixels = baseOffset });
                    type_UIModItem.GetMethod("Append", BindingFlags.Public | BindingFlags.Instance).Invoke(self, new object[] { UIHoverImage });
                }
            }
        }

        static void Wiring_XferWater(On.Terraria.Wiring.orig_XferWater orig)
        {
            for (int i = 0; i < Wiring._numInPump; i++)
            {
                int inPumpX = Wiring._inPumpX[i];
                int inPumpY = Wiring._inPumpY[i];
                var inPumpTile = Main.tile[inPumpX, inPumpY];

                if (inPumpTile.LiquidAmount > 0)
                {

                    for (int j = 0; j < Wiring._numOutPump; j++)
                    {
                        int outPumpX = Wiring._outPumpX[j];
                        int outPumpY = Wiring._outPumpY[j];
                        var outPumpTile = Main.tile[outPumpX, outPumpY];

                        if (outPumpTile.LiquidAmount < 255 && (outPumpTile.LiquidType == inPumpTile.LiquidType || outPumpTile.LiquidAmount == 0))
                        {
                            if (outPumpTile.LiquidAmount == 0)
                                outPumpTile.LiquidType = inPumpTile.LiquidType;

                            int toTransfer = inPumpTile.LiquidAmount;
                            if (toTransfer + outPumpTile.LiquidAmount > 255)
                                toTransfer = 255 - outPumpTile.LiquidAmount;

                            inPumpTile.LiquidAmount -= (byte)toTransfer;
                            outPumpTile.LiquidAmount += (byte)toTransfer;

                            WorldGen.SquareTileFrame(outPumpX, outPumpY, true);
                            if (inPumpTile.LiquidAmount == 0)
                            {
                                WorldGen.SquareTileFrame(inPumpX, inPumpY, true);
                                break;
                            }
                        }
                    }
                }
            }
        }

        static void WorldFile_SaveWorld_Version2(On.Terraria.IO.WorldFile.orig_SaveWorld_Version2 orig, BinaryWriter writer)
        {
            //LiquidWorld.PreSaveWorld();
            orig(writer);
        }

        static void OnHooks_RandomUpdate(orig_RandomUpdate orig, int i, int j, int type)
        {
            if (Main.tile[i, j].LiquidAmount > 0)
                LiquidLoader.OnRandomUpdate(i, j, Main.tile[i, j].LiquidType);
            orig(i, j, type);
        }

        //            HL
        //           7654_3210 - Pos
        // 1000_0000_0000_0000 - sTileHeader
        //           0110_0000 - bTileHeader
        //           1110_0000 - bTileHeader3
        //           0011_1111 - Liquid Byte (64)
        static byte GetLiquidType(ref Tile tile)
        {
            byte result = 0;
			result.SetBit(0, TileDataPacking.GetBit(5, 0));
            result.SetBit(1, TileDataPacking.GetBit(6, 0));
            result.SetBit(2, TileDataPacking.GetBit(15, 0));
            result.SetBit(3, TileDataPacking.GetBit(5, 0));
            result.SetBit(4, TileDataPacking.GetBit(6, 0));
            result.SetBit(5, TileDataPacking.GetBit(7, 0));
            return result;
        }
        static void SetLiquidType(ref Tile tile, byte v)
        {
            TileDataPacking.SetBit(v.IsBit(0), 5, 0);
            TileDataPacking.SetBit(v.IsBit(1), 6, 1);
            TileDataPacking.SetBit(v.IsBit(2), 15, 2);
            TileDataPacking.SetBit(v.IsBit(3), 5, 3);
            TileDataPacking.SetBit(v.IsBit(4), 6, 4);
            TileDataPacking.SetBit(v.IsBit(5), 7, 5);
        }

        static void SetBit(this ref byte b, int pos, bool v)
        {
            if (v)
                b = (byte)(b | 1 << pos);
            else
                b = (byte)(b & ~(1 << pos));
        }
        static bool IsBit(this byte b, int pos) =>
            (b & 1 << pos) != 0;

        static void SetBit(this ref ushort b, int pos, bool v)
        {
            if (v)
                b = (ushort)(b | 1 << pos);
            else
                b = (ushort)(b & ~(1 << pos));
        }
        static bool IsBit(this ushort b, int pos) =>
            (b & 1 << pos) != 0;
    }
}
