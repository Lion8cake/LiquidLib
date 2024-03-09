using LiquidLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace liquidlibtest
{
	internal class Oil_Bucket : ModItem
	{
		public override void SetStaticDefaults()
		{
			//DisplayName.SetDefault(modLiquid.Name[0].ToString().ToUpper() + modLiquid.Name.Remove(0, 1).ToLower() + " Bucket");
			//Tooltip.SetDefault($"Contains a small amount of {modLiquid.Name.ToLower()}" + (modLiquid.AddOnlyBucket ? "\nCannot be poured out" : "\nCan be poured out"));
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
		}

		public override void SetDefaults()
		{
			//modLiquid.BucketType = Type;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTurn = true;
			Item.useAnimation = 15;
			Item.useTime = 15;
			Item.width = 20;
			Item.height = 20;
			Item.maxStack = 99;
			Item.autoReuse = true;
		}

		public override void HoldItem(Player player)
		{
			if (player.InInteractionRange(Player.tileTargetX, Player.tileTargetY))
			{
				player.cursorItemIconEnabled = true;
				player.cursorItemIconID = Type;
			}
		}

		public override bool? UseItem(Player player)
		{
			if (player.whoAmI == Main.myPlayer && player.InInteractionRange(Player.tileTargetX, Player.tileTargetY))
			{
				var tile = Main.tile[Player.tileTargetX, Player.tileTargetY];
				if (tile.LiquidAmount < 200)
				{
					if (WorldGen.PlaceLiquid(Player.tileTargetX, Player.tileTargetY, (byte)LiquidLoader.GetLiquid<Oil>().Type, 255))
					{
						Item.stack--;
						player.PutItemInInventoryFromItemUsage(ItemID.EmptyBucket, player.selectedItem);
						SoundEngine.PlaySound(SoundID.SplashWeak, player.position);
						return true;
					}
				}
			}
			return false;
		}

		//public override void AddRecipes() =>
		//	RecipeGroup.recipeGroups[LiquidLib.bucketsRecipeGroupID].ValidItems.Add(Type);

		public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup) =>
			itemGroup = ContentSamples.CreativeHelper.ItemGroup.Material;
	}
}
