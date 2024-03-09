using LiquidLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;

namespace liquidlibtest
{
	public class Oil : ModLiquid
	{
		public override string BucketTexture => "liquidlibtest/Oil_Bucket";
		public override string LiquidTexture => "liquidlibtest/Oil";
		public override string SlopeTexture => "liquidlibtest/Oil_Slope";
		public override string WaterfallTexture => "liquidlibtest/Oil_Waterfall";
		public override string FlowTexture => "liquidlibtest/Oil_Block";
		public override bool AddOnlyBucket => false;

		public override void SetStaticDefaults()
		{
			Opacity = 1.0f;
			WaterfallLength = 3;
			WaveMaskStrength = 0;
			ViscosityMask = 0;
			DustCount = 20;
			DustType = DustID.Asphalt;
			Sound = SoundID.Splash;
			Delay = 5;
			Drown = true;
			LiquidLoader.CollisionGet(Type, LiquidID.Water)
				.SetTileType(TileID.Diamond)
				.SetSound(SoundID.LiquidsWaterLava);
			LiquidLoader.CollisionGet(Type, LiquidID.Lava)
				.SetTileType(TileID.Stone)
				.SetSound(SoundID.LiquidsWaterLava);
			LiquidLoader.CollisionGet(Type, LiquidID.Honey)
				.SetTileType(TileID.GrayBrick)
				.SetSound(SoundID.LiquidsWaterLava);
		}
	}
}
