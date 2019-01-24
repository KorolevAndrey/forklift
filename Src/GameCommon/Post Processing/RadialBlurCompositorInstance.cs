// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Engine;
using Engine.Renderer;
using Engine.MathEx;

namespace GameCommon
{
	/// <summary>
	/// Represents work with the RadialBlur post effect.
	/// </summary>
	[CompositorName( "RadialBlur" )]
	public class RadialBlurCompositorInstance : CompositorInstance
	{
		static Vec2 center = new Vec2( .5f, .5f );
		static float blurFactor = .1f;

		//

		public static Vec2 Center
		{
			get { return center; }
			set { center = value; }
		}

		public static float BlurFactor
		{
			get { return blurFactor; }
			set { blurFactor = value; }
		}

		protected override void OnMaterialRender( uint passId, Material material, ref bool skipPass )
		{
			base.OnMaterialRender( passId, material, ref skipPass );

			if( passId == 123 )
			{
				GpuProgramParameters parameters = material.Techniques[ 0 ].Passes[ 0 ].FragmentProgramParameters;
				if( parameters != null )
				{
					parameters.SetNamedConstant( "center", new Vec4( center.X, center.Y, 0, 0 ) );
					parameters.SetNamedConstant( "blurFactor", blurFactor );
				}
			}
		}
	}
}
