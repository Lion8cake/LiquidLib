﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Liquid;
using Terraria.ID;
using Terraria.ModLoader.IO;

namespace LiquidLib
{
    public static class LiquidLoader
    {
        internal static readonly List<(Entity entity, int liquidType)> wetEntities = new();
        internal static readonly List<Entity> lastWetEntities = new();
        internal static readonly List<ModLiquid> liquids = new();
        internal static readonly List<GlobalLiquid> globalLiquids = new();
        internal static readonly List<LiquidCollision> liquidCollisions = new()
        {
            new LiquidCollision(0, 1).SetTileType(TileID.Obsidian).SetSound(SoundID.LiquidsWaterLava),
            new LiquidCollision(0, 2).SetTileType(TileID.HoneyBlock).SetSound(SoundID.LiquidsHoneyWater),
            new LiquidCollision(1, 2).SetTileType(TileID.CrispyHoneyBlock).SetSound(SoundID.LiquidsHoneyLava),
        };
        internal static readonly Dictionary<int, string> unloadedLiquids = new();

        public static int BucketsRecipeGroupID => LiquidLib.bucketsRecipeGroupID;

        public static LiquidCollision CollisionGet(int lqiuid, int lqiuid2)
        {
            LiquidCollision lc = null;
            if (liquidCollisions.Any(l =>
            {
                bool @is = l.Is(lqiuid, lqiuid2, out var liquidCollision);
                lc = liquidCollision;
                return @is;
            }))
                return lc;

            lc = new LiquidCollision(lqiuid, lqiuid2);
            liquidCollisions.Add(lc);
            return lc;
        }

        internal static bool CollisionContains(int lqiuid1, int lqiuid2) =>
            liquidCollisions.Any(l => l.Is(lqiuid1, lqiuid2, out _));
        internal static bool TryGetValue<T>(this IList<T> list, Func<T, bool> predict, out T value) =>
            (value = list.FirstOrDefault(predict)) != null;
        internal static bool TryGetValue(this IList<ModLiquid> list, int type, out ModLiquid modLiquid) =>
            list.TryGetValue(i => i.Type == type, out modLiquid);
        internal static bool TryGetValue(this IList<ModLiquid> list, string fullName, out ModLiquid modLiquid) =>
            list.TryGetValue(i => i.FullName == fullName, out modLiquid);

        public static int LiquidCount =>
            liquids.Count + 3;

        public static ModLiquid GetLiquid(int type) =>
            liquids.TryGetValue(type, out var modLiquid) ? modLiquid : null;

        public static ModLiquid GetLiquid(string fullName) =>
            liquids.TryGetValue(fullName, out var modLiquid) ? modLiquid : null;

        public static ModLiquid GetLiquid<T>() where T : ModLiquid =>
            liquids.TryGetValue(i => i is T, out var modLiquid) ? modLiquid : null;

        internal static void OnUpdate()
        {
            for (int i = 0; i < wetEntities.Count; i++)
            {
                if (!lastWetEntities.Contains(wetEntities[i].entity))
                {
                    var (entity, liquidType) = wetEntities[i];
                    OnOutLiquid(liquidType, entity);
                    wetEntities.RemoveAt(i);
                }
            }
            lastWetEntities.Clear();
        }

        public static void GetWaterfallLength(int type, ref int waterfallLength)
        {
            if (liquids.TryGetValue(type, out var modLiquid))
                waterfallLength = modLiquid.WaterfallLength;
            else if (type == 0)
                waterfallLength = 10;
            else if (type == 1)
                waterfallLength = 3;
            else if (type == 2)
                waterfallLength = 2;
            else
                waterfallLength = 5;

            foreach (var globalLiquid in globalLiquids)
                globalLiquid.WaterfallLength(type, ref waterfallLength);
        }

        public static float GetOpacity(int type)
        {
            float opacity = 0.6f;
            if (liquids.TryGetValue(type, out var modLiquid))
                opacity = modLiquid.Opacity;
            else if (type == 1 || type == 2)
                opacity = 0.95f;

            foreach (var globalLiquid in globalLiquids)
                globalLiquid.Opacity(type, ref opacity);

            return opacity;
        }

        public static byte GetWaveMaskStrength(int type)
        {
            byte waveMaskStrength = 0;

            if (liquids.TryGetValue(type, out var modLiquid))
                waveMaskStrength = modLiquid.WaveMaskStrength;

            foreach (var globalLiquid in globalLiquids)
                globalLiquid.WaveMaskStrength(type, ref waveMaskStrength);

            return waveMaskStrength;
        }

        public static byte GetViscosityMask(int type)
        {
            byte viscosityMask = 0;

            if (liquids.TryGetValue(type, out var modLiquid))
                viscosityMask = modLiquid.ViscosityMask;
            else if (type == 0)
                viscosityMask = 160;
            else if (type == 1)
                viscosityMask = 200;
            else if (type == 2)
                viscosityMask = 240;

            foreach (var globalLiquid in globalLiquids)
                globalLiquid.ViscosityMask(type, ref viscosityMask);

            return viscosityMask;
        }

        public static Asset<Texture2D> GetLiquidTexture(int type, int waterStyle = 0)
        {
            Asset<Texture2D> texture;

            if (liquids.TryGetValue(type, out var modLiquid))
                texture = modLiquid.Textures[0];
            else if (type == 0)
                texture = LiquidRenderer.Instance._liquidTextures[type = waterStyle];
            else if (type >= 0 && type < LiquidRenderer.Instance._liquidTextures.Length)
                texture = LiquidRenderer.Instance._liquidTextures[type == 2 ? 11 : type];
            else
                texture = LiquidRenderer.Instance._liquidTextures[0];

            foreach (var globalLiquid in globalLiquids)
                globalLiquid.LiquidTexture(type, waterStyle, ref texture);

            return texture;
        }

        public static Asset<Texture2D> GetFlowTexture(int type)
        {
            Asset<Texture2D> texture;

            if (liquids.TryGetValue(type, out var modLiquid))
                texture = modLiquid.Textures[2];
            else if (type >= 0 && type < TextureAssets.Liquid.Length)
                texture = TextureAssets.Liquid[type == 2 ? 11 : type];
            else
                texture = TextureAssets.Liquid[0];

            foreach (var globalLiquid in globalLiquids)
                globalLiquid.FlowTexture(type, ref texture);

            return texture;
        }

        public static Asset<Texture2D> GetSlopeTexture(int type)
        {
            Asset<Texture2D> texture;

            if (liquids.TryGetValue(type, out var modLiquid))
                texture = modLiquid.Textures[1];
            else if (type >= 0 && type < TextureAssets.LiquidSlope.Length)
                texture = TextureAssets.LiquidSlope[type == 2 ? 11 : type];
            else
                texture = TextureAssets.LiquidSlope[0];

            foreach (var globalLiquid in globalLiquids)
                globalLiquid.SlopeTexture(type, ref texture);

            return texture;
        }

        public static Asset<Texture2D> GetWaterfallTexture(int type, int style)
        {
            Asset<Texture2D> texture;

            if (liquids.TryGetValue(type, out var modLiquid))
                texture = modLiquid.Textures[3];
            else
            {
                if (type == 0)
                    type = style;

                var waterfallTextures = (Asset<Texture2D>[])typeof(WaterfallManager)
                    .GetField("waterfallTexture", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(Main.instance.waterfallManager);
                if (type >= 0 && type < waterfallTextures.Length)
                    texture = waterfallTextures[type == 2 ? 14 : type];
                else
                    texture = waterfallTextures[0];
            }

            foreach (var globalLiquid in globalLiquids)
                globalLiquid.WaterfallTexture(type, ref texture);

            return texture;
        }

        public static void OnLiquidCollision(int i, int j, Entity entity)
        {
            var type = Main.tile[i, j].LiquidType;

            if (liquids.TryGetValue(type, out var modLiquid))
                modLiquid.OnCollision(i, j, entity);

            foreach (var globalLiquid in globalLiquids)
                globalLiquid.OnCollision(type, i, j, entity);
        }

        public static void OnInLiquid(int type, Entity entity)
        {
            bool flag = false;
            int dustCount = -1;
            int dustType = -1;
            SoundStyle sound = SoundID.Dig;

            if (type == 0)
            {
                flag = true;
                dustCount = 50;
                dustType = Dust.dustWater();
                sound = SoundID.Splash; //19, 0
			}
            else if (type == 1)
            {
                flag = true;
                dustCount = 20;
                dustType = 35;
                sound = SoundID.Splash; //19, 1
			}
            else if (type == 2)
            {
                flag = true;
                dustCount = 20;
                dustType = 152;
                sound = SoundID.Splash; //19, 1
            }
            else if (liquids.TryGetValue(type, out var modLiquid))
                if (modLiquid.OnInLiquid(entity))
                {
                    flag = true;
                    dustCount = modLiquid.DustCount;
                    dustType = modLiquid.DustType;
                    sound = modLiquid.Sound;
                }

            foreach (var globalLiquid in globalLiquids)
                if (globalLiquid.OnInLiquid(type, entity))
                {
                    flag = true;
                    globalLiquid.ParticlesAndSound(type, entity, ref dustCount, ref dustType, ref sound);
                }

            if (flag)
                ParticlesAndSound(dustCount, dustType, sound, entity);
        }

        public static void OnOutLiquid(int type, Entity entity)
        {
            bool flag = false;
            int dustCount = -1;
            int dustType = -1;
            SoundStyle sound = SoundID.Dig;

			if (type == 0)
            {
                flag = true;
                dustCount = 50;
                dustType = Dust.dustWater();
                sound = SoundID.Splash; //19, 0
            }
            else if (type == 1)
            {
                flag = true;
                dustCount = 20;
                dustType = 35;
                sound = SoundID.Splash; //19, 1
			}
            else if (type == 2)
            {
                flag = true;
                dustCount = 20;
                dustType = 152;
                sound = SoundID.Splash; //19, 1
            }
            else if (liquids.TryGetValue(type, out var modLiquid))
                if (modLiquid.OnOutLiquid(entity))
                {
                    flag = true;
                    dustCount = modLiquid.DustCount;
                    dustType = modLiquid.DustType;
                    sound = modLiquid.Sound;
                }

            foreach (var globalLiquid in globalLiquids)
                if (globalLiquid.OnOutLiquid(type, entity))
                {
                    flag = true;
                    globalLiquid.ParticlesAndSound(type, entity, ref dustCount, ref dustType, ref sound);
                }

            if (flag)
                ParticlesAndSound(dustCount, dustType, sound, entity);
        }
        static void ParticlesAndSound(int dustCount, int dustType, SoundStyle sound, Entity entity)
        {
            if (dustType >= 0)
                for (int i = 0; i < dustCount; i++)
                {
                    int dust = Dust.NewDust(new Vector2(entity.position.X - 6f, entity.position.Y + entity.height / 2f), entity.width + 12, 24, dustType, 0f, 0f, 0, default, 1f);
                    Main.dust[dust].velocity.Y -= 2f;
                    Main.dust[dust].velocity.X *= 2.5f;
                    Main.dust[dust].alpha = 100;
                    Main.dust[dust].noGravity = true;
                }
            if (sound != null)
                SoundEngine.PlaySound(sound, entity.position);
        }

        public static bool OnUpdate(Liquid liquid)
        {
            bool flag = true;

            if (liquids.TryGetValue(Main.tile[liquid.x, liquid.y].LiquidType, out var modLiquid))
                flag = modLiquid.OnUpdate(liquid);

            foreach (var globalLiquid in globalLiquids)
                flag = globalLiquid.OnUpdate(liquid);

            return flag;
        }

        public static void OnRandomUpdate(int i, int j, int type)
        {
            if (liquids.TryGetValue(type, out var modLiquid))
                modLiquid.OnRandomUpdate(i, j);

            foreach (var globalLiquid in globalLiquids)
                globalLiquid.OnRandomUpdate(type, i, j);
        }

        public static int GetDelay(int type)
        {
            int delay = 0;

            if (liquids.TryGetValue(type, out var modLiquid))
                delay = modLiquid.Delay;
            else if (type == 1)
                delay = 5;
            else if (type == 2)
                delay = 10;

            foreach (var globalLiquid in globalLiquids)
                globalLiquid.Delay(type, ref delay);

            return delay;
        }

        public static bool OnBucket(int type, Item item)
        {
            bool flag = true;

            if (liquids.TryGetValue(type, out var modLiquid))
                flag = modLiquid.OnBucket(item);

            foreach (var globalLiquid in globalLiquids)
                flag = globalLiquid.OnBucket(type, item);

            return flag;
        }

        public static void OnTilePlaceByLiquid(int i, int j, int type, int type2)
        {
            if (liquids.TryGetValue(type, out var modLiquid))
                modLiquid.OnTilePlaceByLiquid(i, j, type2);
            if (liquids.TryGetValue(type2, out modLiquid))
                modLiquid.OnTilePlaceByLiquid(i, j, type);

            foreach (var globalLiquid in globalLiquids)
                globalLiquid.OnTilePlaceByLiquid(i, j, type, type2);
        }

        public static bool OnLiquidDrown(int type)
        {
            bool drown = true;
            if (type == LiquidID.Lava)
                drown = false;

            if (liquids.TryGetValue(type, out var modLiquid))
                drown = modLiquid.Drown;

            foreach (var globalLiquid in globalLiquids)
                globalLiquid.Drown(type, ref drown);

            return drown;
        }

        public static void OnCatchFish(int type, Projectile projectile, ref FishingAttempt fisher)
        {
            if (liquids.TryGetValue(type, out var modLiquid))
            {
                fisher.rolledItemDrop = 0;
                fisher.rolledEnemySpawn = 0;
                modLiquid.OnCatchFish(projectile, ref fisher);
            }

            foreach (var globalLiquid in globalLiquids)
                globalLiquid.OnCatchFish(type, projectile, ref fisher);
        }

        internal static void ReloadTypes(TagCompound unloadedTag)
        {
            unloadedLiquids.Clear();
            var unloaded = new Dictionary<string, object>(unloadedTag);
            foreach ((var name, var type) in unloaded)
                unloadedLiquids[(int)type] = name;

            var reserveTypes = new HashSet<int>();
            foreach ((var type, var name) in unloadedLiquids)
            {
                if (liquids.TryGetValue(name, out var modLiquid))
                {
                    modLiquid.ChangeType(type);
                    reserveTypes.Add(type);
                }
            }

            int freeType = 3;
            foreach (var modLiquid in liquids)
            {
                if (reserveTypes.Contains(modLiquid.Type))
                    continue;

                while (reserveTypes.Contains(freeType))
                    freeType++;

                modLiquid.ChangeType(freeType);
                freeType++;
            }
        }

        internal static void Unload()
        {
            wetEntities.Clear();
            lastWetEntities.Clear();
            liquids.Clear();
            globalLiquids.Clear();
            liquidCollisions.Clear();
            unloadedLiquids.Clear();
        }
    }
}
