﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Liquid;
using Terraria.ID;

namespace LiquidLib
{
    internal static class ILHooks
    {
        static List<string> errors = new();
        public static void Load()
        {
           // IL.Terraria.GameContent.Liquid.LiquidRenderer.InternalPrepareDraw += LiquidRenderer_InternalPrepareDraw;
            //IL.Terraria.GameContent.Liquid.LiquidRenderer.InternalDraw += LiquidRenderer_InternalDraw;

            IL.Terraria.GameContent.Drawing.TileDrawing.DrawTile_LiquidBehindTile += TileDrawing_DrawTile_LiquidBehindTile;
            IL.Terraria.GameContent.Drawing.TileDrawing.DrawPartialLiquid += TileDrawing_DrawPartialLiquid;

            IL.Terraria.WaterfallManager.FindWaterfalls += WaterfallManager_FindWaterfalls;
            IL.Terraria.WaterfallManager.DrawWaterfall += WaterfallManager_DrawWaterfall;

           // IL.Terraria.Player.ItemCheck_UseBuckets += Player_ItemCheck_UseBuckets;

            IL.Terraria.Player.Update += Player_Update;
            IL.Terraria.NPC.Collision_WaterCollision += NPC_Collision_WaterCollision;
            IL.Terraria.Projectile.Update += Projectile_Update;
            IL.Terraria.Item.MoveInWorld += Item_MoveInWorld;

            IL.Terraria.Collision.WetCollision += Collision_WetCollision;

            IL.Terraria.Liquid.Update += Liquid_Update;

            IL.Terraria.GameContent.Tile_Entities.TELogicSensor.GetState += TELogicSensor_GetState;

            IL.Terraria.Collision.DrownCollision += Collision_DrownCollision;

            IL.Terraria.Projectile.GetFishingPondState += Projectile_GetFishingPondState;
            IL.Terraria.Projectile.FishingCheck += Projectile_FishingCheck;

            if (errors.Count > 0)
                foreach (var error in errors)
                    LiquidLib.Instance.Logger.Error("!!! IL Error: \"" + error + "\" !!!");
        }

        public static void Unload()
        {
            //IL.Terraria.GameContent.Liquid.LiquidRenderer.InternalPrepareDraw -= LiquidRenderer_InternalPrepareDraw;
            //IL.Terraria.GameContent.Liquid.LiquidRenderer.InternalDraw -= LiquidRenderer_InternalDraw;

            IL.Terraria.GameContent.Drawing.TileDrawing.DrawTile_LiquidBehindTile -= TileDrawing_DrawTile_LiquidBehindTile;
            IL.Terraria.GameContent.Drawing.TileDrawing.DrawPartialLiquid -= TileDrawing_DrawPartialLiquid;

            IL.Terraria.WaterfallManager.FindWaterfalls -= WaterfallManager_FindWaterfalls;
            IL.Terraria.WaterfallManager.DrawWaterfall -= WaterfallManager_DrawWaterfall;

            //IL.Terraria.Player.ItemCheck_UseBuckets -= Player_ItemCheck_UseBuckets;

            IL.Terraria.Player.Update -= Player_Update;
            IL.Terraria.NPC.Collision_WaterCollision -= NPC_Collision_WaterCollision;
            IL.Terraria.Projectile.Update -= Projectile_Update;
            IL.Terraria.Item.MoveInWorld -= Item_MoveInWorld;

            IL.Terraria.Collision.WetCollision -= Collision_WetCollision;

            IL.Terraria.Liquid.Update -= Liquid_Update;

            IL.Terraria.GameContent.Tile_Entities.TELogicSensor.GetState -= TELogicSensor_GetState;

            IL.Terraria.Collision.DrownCollision -= Collision_DrownCollision;

            IL.Terraria.Projectile.GetFishingPondState -= Projectile_GetFishingPondState;
            IL.Terraria.Projectile.FishingCheck -= Projectile_FishingCheck;
        }

        static int waterfallLength;
        static byte waveMaskStrength;
        static byte viscosityMask;
        static void LiquidRenderer_InternalPrepareDraw(ILContext il)
        {
            var c = new ILCursor(il);

            c.Index = 0;
            if (c.TryGotoNext(i => i.MatchLdsfld<LiquidRenderer>("WAVE_MASK_STRENGTH")))
            {
                c.RemoveRange(4);
                c.EmitDelegate<Func<byte>>(() => waveMaskStrength);
            }
            else
                errors.Add("LiquidRenderer_InternalPrepareDraw");

            if (c.TryGotoNext(i => i.MatchLdsfld<LiquidRenderer>("WAVE_MASK_STRENGTH")))
            {
                c.RemoveRange(3);
                c.EmitDelegate<Func<byte>>(() => waveMaskStrength);
            }
            else
                errors.Add("LiquidRenderer_InternalPrepareDraw");

            c.Index = 0;
            if (c.TryGotoNext(i => i.MatchLdsfld<LiquidRenderer>("VISCOSITY_MASK")))
            {
                c.RemoveRange(4);
                c.EmitDelegate<Func<byte>>(() => viscosityMask);
            }
            else
			    errors.Add("LiquidRenderer_InternalPrepareDraw");

			if (c.TryGotoNext(i => i.MatchLdsfld<LiquidRenderer>("VISCOSITY_MASK")))
            {
                c.RemoveRange(3);
                c.EmitDelegate<Func<byte>>(() => viscosityMask);
            }
            else
                errors.Add("LiquidRenderer_InternalPrepareDraw");

            c.Index = 0;
            if (c.TryGotoNext(i => i.MatchLdloc(0), i => i.MatchLdfld<Microsoft.Xna.Framework.Rectangle>("Y"), i => i.MatchStloc(8), i => i.MatchBr(out _)))
            {
                c.Emit(OpCodes.Ldloc, 7);
                c.Emit(OpCodes.Ldfld, typeof(LiquidRenderer)
                    .GetNestedType("LiquidCache", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetField("VisibleType", BindingFlags.Public | BindingFlags.Instance));
                c.EmitDelegate<Action<byte>>((type) =>
                {
                    waveMaskStrength = LiquidLoader.GetWaveMaskStrength(type);
                    viscosityMask = LiquidLoader.GetViscosityMask(type);
                });
            }
            else
                errors.Add("LiquidRenderer_InternalPrepareDraw");

            c.Index = 0;
            if (c.TryGotoNext(i => i.MatchLdsfld<LiquidRenderer>("WATERFALL_LENGTH")))
            {
                c.Index--;
                c.Emit(OpCodes.Ldloc_S, (byte)7);
                c.Emit(OpCodes.Ldfld, typeof(LiquidRenderer)
                    .GetNestedType("LiquidCache", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetField("Type", BindingFlags.Public | BindingFlags.Instance));
                c.EmitDelegate<Action<int>>(type => LiquidLoader.GetWaterfallLength(type, ref waterfallLength));
            }
            else
                errors.Add("LiquidRenderer_InternalPrepareDraw");

            c.Index = 0;
            if (c.TryGotoNext(i => i.MatchLdsfld<LiquidRenderer>("WATERFALL_LENGTH")))
            {
                c.RemoveRange(4);
                c.EmitDelegate<Func<int>>(() => waterfallLength);
            }
            else
                errors.Add("LiquidRenderer_InternalPrepareDraw");

            c.Index = 0;
            if (c.TryGotoNext(i => i.MatchLdsfld<LiquidRenderer>("WATERFALL_LENGTH")))
            {
                c.RemoveRange(4);
                c.EmitDelegate<Func<int>>(() => waterfallLength);
            }
            else
                errors.Add("LiquidRenderer_InternalPrepareDraw");
        }

        static void LiquidRenderer_InternalDraw(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(i => i.OpCode == OpCodes.Callvirt && i.Operand.ToString().Contains("get_Value")))
            {
                c.Index -= 4;
                c.RemoveRange(5);
                c.Emit(OpCodes.Ldloc_3);
                c.Emit(OpCodes.Ldfld, typeof(LiquidRenderer)
                    .GetNestedType("LiquidDrawCache", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetField("Type", BindingFlags.Public | BindingFlags.Instance));
                c.Emit(OpCodes.Ldarg_3);
                c.EmitDelegate<Func<int, int, Texture2D>>((type, waterStyle) => LiquidLoader.GetLiquidTexture(type, waterStyle).Value);
            }
            else
                errors.Add("LiquidRenderer_InternalDraw");

            c.Index = 0;
            if (c.TryGotoNext(i => i.MatchLdsfld<LiquidRenderer>("DEFAULT_OPACITY")))
            {
                c.Remove();
                c.Index += 2;
                c.Remove();
                c.EmitDelegate<Func<int, float>>(type => LiquidLoader.GetOpacity(type));
            }
            else
                errors.Add("LiquidRenderer_InternalDraw");
        }

        static bool isWaterStyle;
        static void TileDrawing_DrawTile_LiquidBehindTile(ILContext il)
        {
            var c = new ILCursor(il);

            bool error = true;
            while (c.TryGotoNext(i => i.MatchLdloc(42)))
            {
                if (c.Index != 424)
                    continue;
                error = false;

                c.Emit(OpCodes.Ldloc_S, (byte)11);
                c.EmitDelegate<Action<bool>>(flag6 => isWaterStyle = flag6);

                var l = c.DefineLabel();
                c.Emit(OpCodes.Ldloc_S, (byte)24);
                c.Emit(OpCodes.Brfalse, l);
                c.Emit(OpCodes.Ldarg_S, (byte)7);
                c.Emit(OpCodes.Ldfld, typeof(TileDrawInfo).GetField(nameof(TileDrawInfo.tileCache), BindingFlags.Public | BindingFlags.Instance));
                c.Emit(OpCodes.Callvirt, typeof(Tile).GetMethod("liquidType", BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null));
                c.Emit(OpCodes.Stloc_S, (byte)10);
                c.MarkLabel(l);

                l = c.DefineLabel();
                c.Emit(OpCodes.Ldloc_S, (byte)27);
                c.Emit(OpCodes.Brfalse, l);
                c.Emit(OpCodes.Ldarg_S, (byte)7);
                c.Emit(OpCodes.Ldfld, typeof(TileDrawInfo).GetField(nameof(TileDrawInfo.tileCache), BindingFlags.Public | BindingFlags.Instance));
                c.Emit(OpCodes.Callvirt, typeof(Tile).GetMethod("liquidType", BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null));
                c.Emit(OpCodes.Stloc_S, (byte)10);
                c.MarkLabel(l);

                l = c.DefineLabel();
                c.Emit(OpCodes.Ldloc_S, (byte)31);
                c.Emit(OpCodes.Brfalse, l);
                c.Emit(OpCodes.Ldloc_1);
                c.Emit(OpCodes.Callvirt, typeof(Tile).GetMethod("liquidType", BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null));
                c.Emit(OpCodes.Stloc_S, (byte)10);
                c.MarkLabel(l);

                l = c.DefineLabel();
                c.Emit(OpCodes.Ldloc_S, (byte)35);
                c.Emit(OpCodes.Brfalse, l);
                c.Emit(OpCodes.Ldloc_0);
                c.Emit(OpCodes.Callvirt, typeof(Tile).GetMethod("liquidType", BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null));
                c.Emit(OpCodes.Stloc_S, (byte)10);
                c.MarkLabel(l);

                l = c.DefineLabel();
                c.Emit(OpCodes.Ldloc_S, (byte)39);
                c.Emit(OpCodes.Brfalse, l);
                c.Emit(OpCodes.Ldloc_2);
                c.Emit(OpCodes.Callvirt, typeof(Tile).GetMethod("liquidType", BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null));
                c.Emit(OpCodes.Stloc_S, (byte)10);
                c.MarkLabel(l);

                l = c.DefineLabel();
                c.Emit(OpCodes.Ldloc_S, (byte)42);
                c.Emit(OpCodes.Brfalse, l);
                c.Emit(OpCodes.Ldloc_3);
                c.Emit(OpCodes.Callvirt, typeof(Tile).GetMethod("liquidType", BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null));
                c.Emit(OpCodes.Stloc_S, (byte)10);
                c.MarkLabel(l);
                break;
            }
            if (error)
                errors.Add("TileDrawing_DrawTile_LiquidBehindTile");

            c.Index = c.Instrs.Count - 1;
            if (c.TryGotoPrev(i => i.MatchLdarg(0)))
            {
                var l = c.DefineLabel();
                c.Emit(OpCodes.Ldloc_S, (byte)18);
                c.Emit(OpCodes.Brfalse, l);
                c.Emit(OpCodes.Ret);
                c.MarkLabel(l);
            }
            else
                errors.Add("TileDrawing_DrawTile_LiquidBehindTile");

            //c.Index = 0;
            //if (c.TryGotoNext(i => i.MatchLdsfld<Main>("liquidAlpha")) && c.TryGotoNext(i => i.MatchLdsfld<Main>("liquidAlpha")))
            //{
            //    c.Index += 2;
            //    c.Emit(OpCodes.Pop);
            //    c.Emit(OpCodes.Ldc_I4_0);
            //}
            //else
            //    errors.Add("TileDrawing_DrawTile_LiquidBehindTile");
        }

        static void TileDrawing_DrawPartialLiquid(ILContext il)
        {
            var c = new ILCursor(il);

            c.Emit(OpCodes.Ldarg_1);
            c.Emit(OpCodes.Ldarg_2);
            c.Emit(OpCodes.Ldarg_3);
            c.Emit(OpCodes.Ldarg_S, (byte)4);
            c.Emit(OpCodes.Ldarg_S, (byte)5);
            c.EmitDelegate<Action<Tile, Vector2, Rectangle, int, Color>>((tileCache, position, liquidSize, liquidType, aColor) =>
            {
                int num = (int)tileCache.Slope;
                if (!TileID.Sets.BlocksWaterDrawingBehindSelf[tileCache.TileType] || num == 0)
                {
                    Main.spriteBatch.Draw(
                        isWaterStyle ? TextureAssets.Liquid[liquidType].Value : LiquidLoader.GetFlowTexture(liquidType).Value,
                        position, new Rectangle?(liquidSize), aColor, 0f, default, 1f, 0, 0f);
                }
                else
                {
                    liquidSize.X += 18 * (num - 1);
                    Main.spriteBatch.Draw(
                        isWaterStyle ? TextureAssets.LiquidSlope[liquidType].Value : LiquidLoader.GetSlopeTexture(liquidType).Value, 
                        position, new Rectangle?(liquidSize), aColor, 0f, Vector2.Zero, 1f, 0, 0f);
                }
            });
            c.Emit(OpCodes.Ret);
        }

        static void WaterfallManager_FindWaterfalls(ILContext il)
        {
            var c = new ILCursor(il);

            bool error = true;
            while (c.TryGotoNext(i => i.MatchLdarg(0)))
            {
                if (c.Index != 365)
                    continue;
                error = false;

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldfld, typeof(WaterfallManager).GetField("waterfalls", BindingFlags.NonPublic | BindingFlags.Instance));
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldfld, typeof(WaterfallManager).GetField("currentMax", BindingFlags.NonPublic | BindingFlags.Instance));
                c.Emit(OpCodes.Ldelema, typeof(WaterfallManager.WaterfallData));
                c.Emit(OpCodes.Ldloc_S, (byte)15);
                c.Emit(OpCodes.Ldloc_S, (byte)19);
                c.Emit(OpCodes.Ldloc_S, (byte)18);
                c.EmitDelegate<Func<Tile, Tile, Tile, int>>((tile2, tile4, tile3) => 
                    Math.Max(tile2.LiquidType, Math.Max(tile4.LiquidType, tile3.LiquidType)) + 100);
                c.Emit<WaterfallManager.WaterfallData>(OpCodes.Stfld, "type");
                break;
            }
            if (error)
                errors.Add("WaterfallManager_FindWaterfalls");
        }

        static Texture2D waterfallTexture;
        static bool isModLiquid;
        static void WaterfallManager_DrawWaterfall(ILContext il)
        {
            var c = new ILCursor(il);

            bool error = true;
            c.Index = 0;
            while (c.TryGotoNext(i => i.MatchLdloc(144)))
            {
                if (c.Index != 1434)
                    continue;
                error = false;

                c.Index++;
                c.Emit(OpCodes.Pop);
                c.EmitDelegate<Func<bool>>(() => !isModLiquid);
                break;
            }
            if (error)
                errors.Add("WaterfallManager_DrawWaterfall");

            error = true;
            c.Index = 0;
            while (c.TryGotoNext(i => i.MatchLdloc(113)))
            {
                if (c.Index != 804)
                    continue;
                error = false;

                c.Index++;
                c.Emit(OpCodes.Pop);
                c.EmitDelegate<Func<bool>>(() => !isModLiquid);
                break;
            }
            if (error)
                errors.Add("WaterfallManager_DrawWaterfall");

            error = true;
            c.Index = 0;
            while (c.TryGotoNext(i => i.MatchStloc(12)))
            {
                if (c.Index != 36)
                    continue;
                error = false;

                c.Emit(OpCodes.Ldarg_2);
                c.EmitDelegate<Func<int, int, int>>((num12, Style) =>
                {
                    isModLiquid = num12 >= 100;
                    waterfallTexture = LiquidLoader.GetWaterfallTexture(isModLiquid ? num12 - 100 : num12, Style).Value;
                    return num12 == 2 ? 14 : num12;
                });
                break;
            }
            if (error)
                errors.Add("WaterfallManager_DrawWaterfall");

            error = true;
            c.Index = 0;
            int i = 0;
            while (c.TryGotoNext(i => i.OpCode == OpCodes.Callvirt && i.Operand.ToString().Contains("get_Value")))
            {
                if (i++ < 3)
                    continue;
                error = false;

                c.Index -= 4;
                c.RemoveRange(5);
                c.EmitDelegate<Func<Texture2D>>(() => waterfallTexture);
            }
            if (error)
                errors.Add("WaterfallManager_DrawWaterfall");

            //error = true;
            //c.Index = 0;
            //int skip = 0;
            //while (c.TryGotoNext(i => i.MatchLdcI4(11)))
            //{
            //    if (skip == 1)
            //        continue;
            //    error = false;

            //    c.Remove();
            //    c.Emit(OpCodes.Ldc_I4, 911);
            //    skip++;
            //}
            //if (error)
            //    errors.Add("WaterfallManager_DrawWaterfall");

            //error = true;
            //c.Index = 0;
            //skip = 0;
            //while (c.TryGotoNext(i => i.MatchLdcI4(22)))
            //{
            //    if (skip == 5)
            //        continue;
            //    error = false;

            //    c.Remove();
            //    c.Emit(OpCodes.Ldc_I4, 922);
            //    skip++;
            //}
            //if (error)
            //    errors.Add("WaterfallManager_DrawWaterfall");
        }

        static void Player_ItemCheck_UseBuckets(ILContext il)
        {
            var c = new ILCursor(il);

            bool error = true;
            while (c.TryGotoNext(i => i.MatchLdarg(0)))
            {
                if (c.Index != 332)
                    continue;
                error = false;

                c.RemoveRange(6);
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc_S, (byte)4);
                c.EmitDelegate<Action<Player, int>>((player, num) =>
                {
                    if (num == 0)
                        player.PutItemInInventoryFromItemUsage(206, player.selectedItem);
                    else
                        player.PutItemInInventoryFromItemUsage(LiquidLoader.GetLiquid(num).BucketType, player.selectedItem);
                });
                break;
            }
            if (error)
                errors.Add("Player_ItemCheck_UseBuckets");

            error = true;
            c.Index = 0;
            while (c.TryGotoNext(i => i.MatchLdloc(3)))
            {
                if (c.Index != 170)
                    continue;
                error = false;

                var l = c.DefineLabel();
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate<Func<Item, bool>>(item => item.type switch
                    {
                        ItemID.EmptyBucket => LiquidLoader.OnBucket(-1, item),
                        ItemID.WaterBucket => LiquidLoader.OnBucket(0, item),
                        ItemID.LavaBucket => LiquidLoader.OnBucket(1, item),
                        ItemID.HoneyBucket => LiquidLoader.OnBucket(2, item),
                        _ => true,
                    });
                c.Emit(OpCodes.Brtrue, l);
                c.Emit(OpCodes.Ret);
                c.MarkLabel(l);
                break;
            }
            if (error)
                errors.Add("Player_ItemCheck_UseBuckets");
        }

        static void Player_Update(ILContext il)
        {
            var c = new ILCursor(il);
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<Player>>(player => entity = player);
        }

        static void NPC_Collision_WaterCollision(ILContext il)
        {
            var c = new ILCursor(il);
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<NPC>>(npc => entity = npc);
        }

        static void Projectile_Update(ILContext il)
        {
            var c = new ILCursor(il);
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<Projectile>>(projectile => entity = projectile);
        }

        static void Item_MoveInWorld(ILContext il)
        {
            var c = new ILCursor(il);
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<Item>>(item => entity = item);
        }

        static Entity entity;
        static Entity localEntity;
        static void Collision_WetCollision(ILContext il)
        {
            var c = new ILCursor(il);

            bool error = true;
            c.Index = 0;
            while (c.TryGotoNext(i => i.MatchStloc(19)))
            {
                if (c.Index != 353)
                    continue;
                error = false;

                c.Index++;
                c.Emit(OpCodes.Ldloc_S, (byte)11);
                c.Emit(OpCodes.Ldloc_S, (byte)12);
                c.Emit(OpCodes.Ldc_I4_1);
                c.Emit(OpCodes.Sub);
                c.EmitDelegate(Func);
                c.Emit(OpCodes.Ret);
                break;
            }
            if (error)
                errors.Add("Collision_WetCollision");

            error = true;
            c.Index = 0;
            while (c.TryGotoNext(i => i.MatchStloc(19)))
            {
                if (c.Index != 245)
                    continue;
                error = false;

                c.Index++;
                c.Emit(OpCodes.Ldloc_S, (byte)11);
                c.Emit(OpCodes.Ldloc_S, (byte)12);
                c.EmitDelegate(Func);
                c.Emit(OpCodes.Ret);
                break;
            }
            if (error)
                errors.Add("Collision_WetCollision");

            c.Index = 0;
            c.EmitDelegate<Action>(() =>
            {
                localEntity = entity;
                entity = null;
            });
        }
        static readonly Func<int, int, bool> Func = (i, j) =>
        {
            if (localEntity != null)
            {
                localEntity.wetCount = 10;
                LiquidLoader.OnLiquidCollision(i, j, localEntity);
                var liquidType = Main.tile[i, j].LiquidType;
                LiquidLoader.lastWetEntities.Add(localEntity);
                var a = (localEntity, liquidType);
                if (!LiquidLoader.wetEntities.Contains(a))
                {
                    LiquidLoader.wetEntities.Add(a);
                    LiquidLoader.OnInLiquid(liquidType, localEntity);
                }
                return true;
            }
            return true;
        };

        static void Liquid_Update(ILContext il)
        {
            var c = new ILCursor(il);

            bool error = true;
            while (c.TryGotoNext(i => i.MatchLdloc(13)))
            {
                if (c.Index != 146)
                    continue;
                error = false;

                c.Remove();
                c.Emit(OpCodes.Ldc_I4_0);
                break;
            }
            if (error)
                errors.Add("Liquid_Update");

            error = true;
            c.Index = 0;
            while (c.TryGotoNext(i => i.MatchLdloc(12)))
            {
                if (c.Index != 133)
                    continue;
                error = false;

                var l = c.DefineLabel();
                c.Remove();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<Liquid, bool>>((Func<Liquid, bool>)((liquid) =>
                {
                    if (!WorldGen.SolidTile(liquid.x, liquid.y, false))
                    {
                        var tile = Main.tile[liquid.x, liquid.y];

                        bool ProcessLiquid(int i, int j)
                        {
                            var t = Main.tile[i, j];
                            bool flag = true;
                            if (t.LiquidAmount > 0 && t.LiquidType != tile.LiquidType)
                            {
                                if (t.LiquidAmount > 23)
                                {
                                    var collision = GetTile(t);
                                    tile.LiquidAmount = 0;
                                    if (j < liquid.y)
                                        j++;
                                    WorldGen.PlaceTile(i, j, collision.tileType, true, true);
                                    LiquidLoader.OnTilePlaceByLiquid(i, j, t.LiquidType, tile.LiquidType);
                                    WorldGen.SquareTileFrame(i, j, true);
                                    if (!WorldGen.gen)
                                        SoundEngine.PlaySound(collision.sound, new Vector2(i * 16 + 8, j * 16 + 8));
                                    if (Main.netMode == NetmodeID.Server)
                                        NetMessage.SendTileSquare(-1, i - 1, j - 1, 3, TileChangeType.None);
                                    flag = false;
                                }
                                t.LiquidAmount = 0;
                            }
                            return flag;
                        };
                        LiquidCollision GetTile(Tile t)
                        {
                            if (LiquidLoader.CollisionContains(tile.LiquidType, t.LiquidType))
                                return LiquidLoader.CollisionGet(tile.LiquidType, t.LiquidType);
                            return new LiquidCollision(0, 0).SetTileType(TileID.Dirt).SetSound(SoundID.LiquidsWaterLava);
                        }

                        if (ProcessLiquid(liquid.x - 1, liquid.y))
                            if (ProcessLiquid(liquid.x + 1, liquid.y))
                                if (ProcessLiquid(liquid.x, liquid.y - 1))
                                    ProcessLiquid(liquid.x, liquid.y + 1);
                    }

                    if (!Liquid.quickFall)
                    {
                        if (liquid.delay < LiquidLoader.GetDelay(Main.tile[liquid.x, liquid.y].LiquidType))
                        {
                            liquid.delay++;
                            return true;
                        }
                        liquid.delay = 0;
                    }
                    return false;
                }));
                c.Emit(OpCodes.Brfalse, l);
                c.Emit(OpCodes.Ret);
                c.MarkLabel(l);
                c.Emit(OpCodes.Ldc_I4_1);
                break;
            }
            if (error)
                errors.Add("Liquid_Update");
        }

        static void TELogicSensor_GetState(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(i => i.MatchLdloc(15)))
            {
                c.Remove();
                c.Emit(OpCodes.Ldloc_S, (byte)9);
                c.Emit(OpCodes.Ldarg_2);
                c.EmitDelegate<Func<Tile, int, bool>>((tile, type) => tile.LiquidType != 0 && type == 4);
            }
            else
                errors.Add("TELogicSensor_GetState");
        }

        static void Collision_DrownCollision(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(i => i.MatchCallvirt<Tile>("lava")))
            {
                c.Index--;
                c.RemoveRange(3);
            }
            else
                errors.Add("Collision_DrownCollision");

            if (c.TryGotoNext(i => i.MatchLdloc(19)))
            {
                c.Index++;
                c.Emit(OpCodes.Ldloc_S, (byte)15);
                c.EmitDelegate<Func<bool, Tile, bool>>((result, tile) => result && LiquidLoader.OnLiquidDrown(tile.LiquidType));
            }
            else
                errors.Add("Collision_DrownCollision");
        }

        static byte fishingLiquidType;
        static void Projectile_GetFishingPondState(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(i => i.MatchLdloc(5)))
            {
                c.Emit(OpCodes.Ldloc_3);
                c.Emit(OpCodes.Ldloc_S, (byte)4);
                c.EmitDelegate<Action<int, int>>((i, j) =>
                    fishingLiquidType = Math.Max(fishingLiquidType, (byte)Main.tile[i, j].LiquidType));
            }
            else
                errors.Add("Projectile_GetFishingPondState");
        }
        delegate void RefAction<T>(ref T obj);
        static void Projectile_FishingCheck(ILContext il)
        {
            var c = new ILCursor(il);

            if (c.TryGotoNext(i => i.MatchStloc(3)))
            {
                c.Index++;
                c.Emit(OpCodes.Ldloc_S, (byte)0);
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<FishingAttempt, Projectile, FishingAttempt>>((fishingAttempt, projectile) =>
                {
                    LiquidLoader.OnCatchFish(fishingLiquidType, projectile, ref fishingAttempt);
                    fishingLiquidType = 0;
                    return fishingAttempt;
                });
                c.Emit(OpCodes.Stloc_S, (byte)0);
            }
            else
                errors.Add("Projectile_FishingCheck");
        }
    }
}
