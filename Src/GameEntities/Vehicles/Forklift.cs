using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Drawing.Design;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.PhysicsSystem;
using Engine.Renderer;
using Engine.SoundSystem;
using Engine.Utils;
using GameCommon;

namespace GameEntities
{
	//!!!!!!this class not implemented
	//!!!!! this is a fake

	/// <summary>
	/// Defines the <see cref="Forklift"/> entity type.
	/// </summary>
	public class ForkliftType : UnitType
	{
		//Animation fcharacter
		[DefaultValue( "right" )]
		string rightAnimationName = "right";
		[DefaultValue( "left" )]
		string leftAnimationName = "left";
		//End animation fcharacter
		
		[FieldSerialize]
		float fmaximumspeedgear1 = 8.1f;
		
		[FieldSerialize]
		float bmaximumspeedgear1 = 8.1f;
		
		[FieldSerialize]
		float brakeforcegear1 = 9.9f;
		
		[FieldSerialize]
		float forwardgear1force = 1.1f;
		
		[FieldSerialize]
		float backwardgear1force = 1.1f;
		
		[FieldSerialize]
		float rotationspeed = 10.1f;
		
		[FieldSerialize]
		float rotation = 5.1f;
		
		[FieldSerialize]
		float rotationradius = 30.1f;
		
		[FieldSerialize]
		float forkupmax = 55.1f;
		
		[FieldSerialize]
		float forkupspeed = 0.05f;
		
		//Animation fcharacter
		[DefaultValue( "right" )]
		public string RightAnimationName
		{
			get { return rightAnimationName; }
			set { rightAnimationName = value; }
		}
		[DefaultValue( "left" )]
		public string LeftAnimationName
		{
			get { return leftAnimationName; }
			set { leftAnimationName = value; }
		}
		//End animation fcharacter
		
		[DefaultValue( 8.1f )]
		public float FMaximumspeedgear1
		{
			get { return fmaximumspeedgear1; }
			set { fmaximumspeedgear1 = value; }
		}
		
		[DefaultValue( 8.1f )]
		public float BMaximumspeedgear1
		{
			get { return bmaximumspeedgear1; }
			set { bmaximumspeedgear1 = value; }
		}
		
		[DefaultValue( 9.9f )]
		public float Brakeforcegear1
		{
			get { return brakeforcegear1; }
			set { brakeforcegear1 = value; }
		}
		
		[DefaultValue( 1.1f )]
		public float Forwardgear1force
		{
			get { return forwardgear1force; }
			set { forwardgear1force = value; }
		}
		
		[DefaultValue( 1.1f )]
		public float Backwardgear1force
		{
			get { return backwardgear1force; }
			set { backwardgear1force = value; }
		}
			
		[DefaultValue( 10.1f )]
		public float Rotationspeed
		{
			get { return rotationspeed; }
			set { rotationspeed = value; }
		}
		
		[DefaultValue( 5.1f )]
		public float Rotation
		{
			get { return rotation; }
			set { rotation = value; }
		}
		
		[DefaultValue( 30.1f )]
		public float Rotationradius
		{
			get { return rotationradius; }
			set { rotationradius = value; }
		}
		
		[DefaultValue( 55.1f )]
		public float Forkupmax
		{
			get { return forkupmax; }
			set { forkupmax = value; }
		}
		
		[DefaultValue( 0.05f )]
		public float Forkupspeed
		{
			get { return forkupspeed; }
			set { forkupspeed = value; }
		}
		
		[FieldSerialize]
		List<Gear> gears = new List<Gear>();
		
		[FieldSerialize]
		string soundOn;

		[FieldSerialize]
		string soundOff;

		[FieldSerialize]
		string soundGearUp;

		[FieldSerialize]
		string soundGearDown;
		
		public class Gear
		{
			[FieldSerialize]
			int number;

			[FieldSerialize]
			Range speedRange;

			[FieldSerialize]
			string soundMotor;

			[FieldSerialize]
			[DefaultValue( typeof( Range ), "1 1.2" )]
			Range soundMotorPitchRange = new Range( 1, 1.2f );

			//

			[DefaultValue( 0 )]
			public int Number
			{
				get { return number; }
				set { number = value; }
			}

			[DefaultValue( typeof( Range ), "0 0" )]
			public Range SpeedRange
			{
				get { return speedRange; }
				set { speedRange = value; }
			}

			[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
			public string SoundMotor
			{
				get { return soundMotor; }
				set { soundMotor = value; }
			}

			[DefaultValue( typeof( Range ), "1 1.2" )]
			public Range SoundMotorPitchRange
			{
				get { return soundMotorPitchRange; }
				set { soundMotorPitchRange = value; }
			}

			public override string ToString()
			{
				return string.Format( "Gear {0}", number );
			}
		}
		
		public List<Gear> Gears
		{
			get { return gears; }
		}

		[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
		public string SoundOn
		{
			get { return soundOn; }
			set { soundOn = value; }
		}

		[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
		public string SoundOff
		{
			get { return soundOff; }
			set { soundOff = value; }
		}

		[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
		public string SoundGearUp
		{
			get { return soundGearUp; }
			set { soundGearUp = value; }
		}

		[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
		public string SoundGearDown
		{
			get { return soundGearDown; }
			set { soundGearDown = value; }
		}

		protected override void OnPreloadResources()
		{
			base.OnPreloadResources();

			if( !string.IsNullOrEmpty( SoundOn ) )
				SoundWorld.Instance.SoundCreate( SoundOn, SoundMode.Mode3D );
			if( !string.IsNullOrEmpty( SoundOff ) )
				SoundWorld.Instance.SoundCreate( SoundOff, SoundMode.Mode3D );
			if( !string.IsNullOrEmpty( SoundGearUp ) )
				SoundWorld.Instance.SoundCreate( SoundGearUp, SoundMode.Mode3D );
			if( !string.IsNullOrEmpty( SoundGearDown ) )
				SoundWorld.Instance.SoundCreate( SoundGearDown, SoundMode.Mode3D );

			foreach( Gear gear in gears )
			{
				if( !string.IsNullOrEmpty( gear.SoundMotor ) )
				{
					SoundWorld.Instance.SoundCreate( gear.SoundMotor, SoundMode.Mode3D |
						SoundMode.Loop );
				}
			}
		}
	}
	
	

	public class Forklift : Unit
	{
		Wheel leftWheel = new Wheel();
		Wheel rightWheel = new Wheel();
		float wheelsPositionYOffset;
		Body forkBody;
		Body up1Body;
		Body up2Body;
		Body b2Body;
		Body b1Body;
		Body wheelRearRightBody;
		Body wheelRearLeftBody;
		Body forkliftBody;
		Body leftwheelBody;
		Body rightwheelBody;
		
		//currently gears used only for sounds
		ForkliftType.Gear currentGear;
		bool firstTick = true;
		bool motorOn;
		string currentMotorSoundName;
		VirtualChannel motorSoundChannel;
		
		class Wheel
		{
			public List<MapObjectAttachedHelper> wheelHelpers = new List<MapObjectAttachedHelper>();
			public bool onGround = true;
		}
		
		ForkliftType _type = null; public new ForkliftType Type { get { return _type; } }
			

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );
			AddTimer();
			if( !EngineApp.Instance.IsResourceEditor )
			{
				if( PhysicsModel == null )
				{
					Log.Error( "Forklift: Physics model not exists." );
					return;
				}

				forkliftBody = PhysicsModel.GetBody( "forklift" );
				if( forkliftBody == null )
				{
					Log.Error( "Forklift: \"forklift\" body not exists." );
					return;
				}
				
				leftwheelBody = PhysicsModel.GetBody( "leftwheel" );
				rightwheelBody = PhysicsModel.GetBody( "rightwheel" );
				forkBody = PhysicsModel.GetBody( "fork" );
				up1Body = PhysicsModel.GetBody( "up1" );
		        up2Body = PhysicsModel.GetBody( "up2" );
		        b2Body = PhysicsModel.GetBody( "b2" );
		        b1Body = PhysicsModel.GetBody( "b1" );
		        wheelRearRightBody = PhysicsModel.GetBody( "wheelRearRight" );
		        wheelRearLeftBody = PhysicsModel.GetBody( "wheelRearLeft" );
		        foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
				{
					if( attachedObject.Alias == "leftWheel" )
						leftWheel.wheelHelpers.Add( (MapObjectAttachedHelper)attachedObject );
					if( attachedObject.Alias == "rightWheel" )
						rightWheel.wheelHelpers.Add( (MapObjectAttachedHelper)attachedObject );
				}

				if( leftWheel.wheelHelpers.Count != 0 )
					wheelsPositionYOffset = Math.Abs( leftWheel.wheelHelpers[ 0 ].PositionOffset.Y );
		        
			}
			//initialize currentGear
			currentGear = Type.Gears.Find( delegate( ForkliftType.Gear gear )
			{
				return gear.Number == 0;
			} );
		}
		
		protected override void OnDestroy()
		{
			if( motorSoundChannel != null )
			{
				motorSoundChannel.Stop();
				motorSoundChannel = null;
			}
			base.OnDestroy();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();			
			if( Intellect != null )
			TickRearWheels();
			TickFrontWheels();
			TickFrontWheelsGround();
			TickCurrentGear();
			TickMotorSound();
			TickTurnOver();
			firstTick = false;
		}
		
		void TickMotorSound()
		{
			bool lastMotorOn = motorOn;
			motorOn = Intellect != null && Intellect.IsActive();

			//sound on, off
			if( motorOn != lastMotorOn )
			{
				if( !firstTick && Life != 0 )
				{
					if( motorOn )
						SoundPlay3D( Type.SoundOn, .7f, true );
					else
						SoundPlay3D( Type.SoundOff, .7f, true );
				}
			}

			string needSoundName = null;
			if( motorOn && currentGear != null )
				needSoundName = currentGear.SoundMotor;

			if( needSoundName != currentMotorSoundName )
			{
				//change motor sound

				if( motorSoundChannel != null )
				{
					motorSoundChannel.Stop();
					motorSoundChannel = null;
				}

				currentMotorSoundName = needSoundName;

				if( !string.IsNullOrEmpty( needSoundName ) )
				{
					Sound sound = SoundWorld.Instance.SoundCreate( needSoundName,
						SoundMode.Mode3D | SoundMode.Loop );

					if( sound != null )
					{
						motorSoundChannel = SoundWorld.Instance.SoundPlay(
							sound, EngineApp.Instance.DefaultSoundChannelGroup, .3f, true );
						motorSoundChannel.Position = Position;
						motorSoundChannel.Pause = false;
					}
				}
			}
			//update motor channel position and pitch
			if( motorSoundChannel != null )
			{
				Range speedRangeAbs = currentGear.SpeedRange;
				if( speedRangeAbs.Minimum < 0 && speedRangeAbs.Maximum < 0 )
					speedRangeAbs = new Range( -speedRangeAbs.Maximum, -speedRangeAbs.Minimum );
				Range pitchRange = currentGear.SoundMotorPitchRange;
				
				Vec3 Velocity = forkliftBody.LinearVelocity * forkliftBody.Rotation.GetInverse();
				float speedAbs = Velocity.X * 1;

				float speedCoef = 0;
				if( speedRangeAbs.Size() != 0 )
					speedCoef = ( speedAbs - speedRangeAbs.Minimum ) / speedRangeAbs.Size();
				MathFunctions.Clamp( ref speedCoef, 0, 1 );

				//update channel
				motorSoundChannel.Pitch = pitchRange.Minimum + speedCoef * pitchRange.Size();
				motorSoundChannel.Position = Position;
			}
		}
		
		//Animation fcharacter
		protected override void OnUpdateBaseAnimation()
		{
			if (Intellect != null)
			{
				if (Intellect.IsControlKeyPressed(GameControlKeys.Right))
				{
					UpdateBaseAnimation( Type.RightAnimationName, false, false, 1);
				}
				if (Intellect.IsControlKeyPressed(GameControlKeys.Left))
				{
					UpdateBaseAnimation( Type.LeftAnimationName, false, false, 1);
				}
			}
		}
		
		void TickRearWheels()
		{
			float leftwheelForce = 0;
            float rightwheelForce = 0;

            if (Intellect != null)
            {
                    ServoMotor rightwheel = PhysicsModel.GetMotor("rightwheelMotor") as ServoMotor;
                    ServoMotor leftwheel = PhysicsModel.GetMotor("leftwheelMotor") as ServoMotor;
                    

                    if (rightwheel != null)
                    {
                        if (leftwheel != null)
                        {

                            float speed = Type.Rotationspeed;
                            MathFunctions.Clamp(ref speed, Type.Rotationspeed, Type.Rotationspeed);
                            Radian needAngle = leftwheel.DesiredAngle;
                            if (Intellect.IsControlKeyPressed(GameControlKeys.Left))
                            {
                                if (3 > 2)
                                {
                                  	leftwheelForce += speed;
                                    rightwheelForce -= speed;                                    
                                }
                                needAngle += Type.Rotation;
                            }

                            else if (Intellect.IsControlKeyPressed(GameControlKeys.Right))
                            {
                                if (3 > 2)
                                {
                                    
                                    leftwheelForce -= speed;
                                    rightwheelForce += speed;
                                }
                                
                                needAngle -= Type.Rotation;
                                
                            }

                            else
                            {
                                needAngle = 0f;
                                
                            }

                            MathFunctions.Clamp(ref needAngle,new Degree(-Type.Rotationradius).InRadians(),
                            new Degree(Type.Rotationradius).InRadians());
                            rightwheel.DesiredAngle = needAngle;
                            leftwheel.DesiredAngle = needAngle;
                        }
                    }
                }
            ServoMotor motor = PhysicsModel.GetMotor( "upMotor" ) as ServoMotor;
				if( motor != null )
				{
					Radian needAngle = motor.DesiredAngle;

					needAngle += Intellect.GetControlKeyStrength( GameControlKeys.Fire1 ) * Type.Forkupspeed;
					needAngle -= Intellect.GetControlKeyStrength( GameControlKeys.Fire2 ) * Type.Forkupspeed;

					MathFunctions.Clamp( ref needAngle,
						new Degree( -0.0f ).InRadians(), new Degree( Type.Forkupmax ).InRadians() );

					motor.DesiredAngle = needAngle;
				}		
            }
	     
	     void TickFrontWheelsGround()
		{
			if( forkliftBody == null )
				return;

			if( forkliftBody.Sleeping )
				return;

			float rayLength = .7f;

			leftWheel.onGround = false;
			rightWheel.onGround = false;

			float mass = 0;
			foreach( Body body in PhysicsModel.Bodies )
				mass += body.Mass;

			int helperCount = leftWheel.wheelHelpers.Count + rightWheel.wheelHelpers.Count;

			float verticalVelocity =
				( forkliftBody.Rotation.GetInverse() * forkliftBody.LinearVelocity ).Z;

			for( int side = 0; side < 2; side++ )
			{
				Wheel wheel = side == 0 ? leftWheel : rightWheel;

				foreach( MapObjectAttachedHelper wheelHelper in wheel.wheelHelpers )
				{
					Vec3 pos;
					Quat rot;
					Vec3 scl;
					wheelHelper.GetGlobalTransform( out pos, out rot, out scl );

					Vec3 downDirection = forkliftBody.Rotation * new Vec3( 0, 0, -rayLength );

					Vec3 start = pos - downDirection;

					Ray ray = new Ray( start, downDirection );
					RayCastResult[] piercingResult = PhysicsWorld.Instance.RayCastPiercing(
						ray, (int)ContactGroup.CastOnlyContact );

					bool collision = false;
					Vec3 collisionPos = Vec3.Zero;

					foreach( RayCastResult result in piercingResult )
					{
						if( Array.IndexOf( PhysicsModel.Bodies, result.Shape.Body ) != -1 )
							continue;
						collision = true;
						collisionPos = result.Position;
						break;
					}

					if( collision )
					{
						wheel.onGround = true;

						float distance = ( collisionPos - start ).Length();

						if( distance < rayLength )
						{
							float needCoef = ( rayLength - distance ) / rayLength;

							float force = 0;

							forkliftBody.AddForce( ForceType.GlobalAtGlobalPos,
								TickDelta, new Vec3( 0, 0, force ), pos );
						}
					}
				}
			}
		}
				
		void TickFrontWheels()
		{
			Vec3 Velocity = forkliftBody.LinearVelocity * forkliftBody.Rotation.GetInverse();
		    bool onGround = leftWheel.onGround || rightWheel.onGround;
			float leftwheel = 0;
			if( Intellect != null )
			{
				float forward = Intellect.GetControlKeyStrength( GameControlKeys.Forward );
				leftwheel += forward;

				float backward = Intellect.GetControlKeyStrength( GameControlKeys.Backward );
				leftwheel -= backward;
				
				MathFunctions.Clamp( ref leftwheel, -1, 1 );
				
			}
			//return if no throttle and sleeping
			if( forkliftBody.Sleeping && leftwheelBody.Sleeping && rightwheelBody.Sleeping
			   && wheelRearRightBody.Sleeping && wheelRearLeftBody.Sleeping && leftwheel == 0 )
				return;
			
			if( leftWheel.onGround && rightWheel.onGround)
			{
			if( leftwheel > 0 && Velocity.X < Type.FMaximumspeedgear1){
				float force = Velocity.X > 0 ? Type.Forwardgear1force : Type.Brakeforcegear1;
				force *= leftwheel;
				forkliftBody.AddForce( ForceType.LocalAtLocalPos, TickDelta,
				new Vec3( force, 0, 0 ), new Vec3( 0, 0, 0 ) );}
				
				
				
				
			if( leftwheel < 0 && ( -Velocity.X ) < Type.BMaximumspeedgear1 ){
				float force = -Velocity.X < 0 ? Type.Brakeforcegear1 : Type.Backwardgear1force;
			    force *= leftwheel;
			    forkliftBody.AddForce( ForceType.LocalAtLocalPos, TickDelta,
			    new Vec3( force, 0, 0 ), new Vec3( 0, 0, 0 ) );}
			}
		}

		
		protected override void OnRender( Camera camera )
		{
			//not very true update in the OnRender.
			//it is here because need update after all Ticks and before update attached objects.
			

			base.OnRender( camera );
		}
		
		void TickTurnOver()
		{
			if( Rotation.GetUp().Z < 0.3f )
				Die();
		}

		void TickCurrentGear()
		{
			//currently gears used only for sounds

			if( currentGear == null )
				return;

			if( motorOn )
			{
				Vec3 Velocity = forkliftBody.LinearVelocity * forkliftBody.Rotation.GetInverse();
				float speed = Velocity.X * 1;

				ForkliftType.Gear newGear = null;

				if( speed < currentGear.SpeedRange.Minimum || speed > currentGear.SpeedRange.Maximum )
				{
					//find new gear
					newGear = Type.Gears.Find( delegate( ForkliftType.Gear gear )
					{
						return speed >= gear.SpeedRange.Minimum && speed <= gear.SpeedRange.Maximum;
					} );
				}

				if( newGear != null && currentGear != newGear )
				{
					//change gear
					ForkliftType.Gear oldGear = currentGear;
					OnGearChange( oldGear, newGear );
					currentGear = newGear;
				}
			}
			else
			{
				if( currentGear.Number != 0 )
				{
					currentGear = Type.Gears.Find( delegate( ForkliftType.Gear gear )
					{
						return gear.Number == 0;
					} );
				}
			}
		}

		void OnGearChange( ForkliftType.Gear oldGear, ForkliftType.Gear newGear )
		{
			if( !firstTick && Life != 0 )
			{
				bool up = Math.Abs( newGear.Number ) > Math.Abs( oldGear.Number );
				string soundName = up ? Type.SoundGearUp : Type.SoundGearDown;
				SoundPlay3D( soundName, .7f, true );
			}
		}
	}
}