using System;
using System.Collections.Generic;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.UISystem;
using Engine.FileSystem;
using Engine.PhysicsSystem;
using Engine.Renderer;
using Engine.SoundSystem;
using Engine.MathEx;
using Engine.Utils;
using GameCommon;
using GameEntities;

namespace Maps_Level__LogicSystem_LogicSystemScripts
{
	public class ShowText : Engine.EntitySystem.LogicSystem.LogicEntityObject
	{
		Engine.MapSystem.Region __ownerEntity;
		
		public ShowText( Engine.MapSystem.Region ownerEntity )
			: base( ownerEntity )
		{
			this.__ownerEntity = ownerEntity;
			ownerEntity.ObjectIn += delegate( Engine.EntitySystem.Entity __entity, Engine.MapSystem.MapObject obj ) { if( Engine.EntitySystem.LogicSystemManager.Instance != null )ObjectIn( obj ); };
		}
		
		public Engine.MapSystem.Region Owner
		{
			get { return __ownerEntity; }
		}
		
		[Engine.EntitySystem.Entity.FieldSerialize]
		public System.Boolean visited;
		
		public void ObjectIn( Engine.MapSystem.MapObject obj )
		{
			Engine.EntitySystem.LogicClass __class = Engine.EntitySystem.LogicSystemManager.Instance.MapClassManager.GetByName( "ShowText" );
			Engine.EntitySystem.LogicSystem.LogicDesignerMethod __method = (Engine.EntitySystem.LogicSystem.LogicDesignerMethod)__class.GetMethodByName( "ObjectIn" );
			__method.Execute( this, new object[ 1 ]{ obj } );
		}

	}
}
