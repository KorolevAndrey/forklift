// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using Engine;
using Engine.MathEx;
using Engine.Renderer;
using Engine.EntitySystem;
using Engine.FileSystem;
using Engine.MapSystem;
using Engine.UISystem;
using Engine.SoundSystem;
using Engine.PhysicsSystem;
using Engine.Utils;
using Engine.Networking;
using GameCommon;
using GameEntities;

namespace Game
{
	/// <summary>
	/// Defines a game application.
	/// </summary>
	public class GameEngineApp : EngineApp
	{
		static GameEngineApp instance;

		string needMapLoadName;
		bool needMapCreateForDynamicMapExample;
		string needWorldLoadName;

		//for network client
		bool client_AllowCheckForDisconnection = true;

		ScreenControlManager controlManager;

		int fullscreenFadingRemainingFrames;

		//

		static float gamma = 1;
		[Config( "Video", "gamma" )]
		public static float _Gamma
		{
			get { return gamma; }
			set
			{
				gamma = value;
				EngineApp.Instance.Gamma = gamma;
			}
		}

		static bool showSystemCursor;
		[Config( "Video", "showSystemCursor" )]
		public static bool _ShowSystemCursor
		{
			get { return showSystemCursor; }
			set
			{
				showSystemCursor = value;

				EngineApp.Instance.ShowSystemCursor = value;

				if( EngineApp.Instance.ShowSystemCursor )
				{
					if( Instance != null && Instance.ControlManager != null )
						Instance.ControlManager.DefaultCursor = null;
				}
				else
				{
					string cursorName = "Cursors\\Default.png";
					if( !VirtualFile.Exists( cursorName ) )
						cursorName = null;
					if( Instance != null && Instance.ControlManager != null )
						Instance.ControlManager.DefaultCursor = cursorName;
					if( cursorName == null )
						EngineApp.Instance.ShowSystemCursor = true;
				}
			}
		}

		static bool drawFPS;
		[Config( "Video", "drawFPS" )]
		public static bool _DrawFPS
		{
			get { return drawFPS; }
			set
			{
				drawFPS = value;
				EngineApp.Instance.ShowFPS = value;
			}
		}

		static MaterialSchemes materialScheme = MaterialSchemes.Default;
		[Config( "Video", "materialScheme" )]
		public static MaterialSchemes MaterialScheme
		{
			get { return materialScheme; }
			set
			{
				materialScheme = value;
				if( RendererWorld.Instance != null )
					RendererWorld.Instance.DefaultViewport.MaterialScheme = materialScheme.ToString();
			}
		}

		static ShadowTechniques shadowTechnique = ShadowTechniques.ShadowmapHigh;
		[Config( "Video", "shadowTechnique" )]
		public static ShadowTechniques ShadowTechnique
		{
			get { return shadowTechnique; }
			set { shadowTechnique = value; }
		}

		//this options affect to shadowColor and shadowFarDistance
		static bool shadowUseMapSettings = true;
		[Config( "Video", "shadowUseMapSettings" )]
		public static bool ShadowUseMapSettings
		{
			get { return shadowUseMapSettings; }
			set { shadowUseMapSettings = value; }
		}

		static ColorValue shadowColor = new ColorValue( .75f, .75f, .75f );
		[Config( "Video", "shadowColor" )]
		public static ColorValue ShadowColor
		{
			get { return shadowColor; }
			set { shadowColor = value; }
		}

		static float shadowFarDistance = 30;
		[Config( "Video", "shadowFarDistance" )]
		public static float ShadowFarDistance
		{
			get { return shadowFarDistance; }
			set { shadowFarDistance = value; }
		}

		static Vec2 shadowPSSMSplitFactors = new Vec2( .1f, .4f );
		[Config( "Video", "shadowPSSMSplitFactors" )]
		public static Vec2 ShadowPSSMSplitFactors
		{
			get { return shadowPSSMSplitFactors; }
			set { shadowPSSMSplitFactors = value; }
		}

		static int shadowDirectionalLightTextureSize = 2048;
		[Config( "Video", "shadowDirectionalLightTextureSize" )]
		public static int ShadowDirectionalLightTextureSize
		{
			get { return shadowDirectionalLightTextureSize; }
			set { shadowDirectionalLightTextureSize = value; }
		}

		static int shadowDirectionalLightMaxTextureCount = 1;
		[Config( "Video", "shadowDirectionalLightMaxTextureCount" )]
		public static int ShadowDirectionalLightMaxTextureCount
		{
			get { return shadowDirectionalLightMaxTextureCount; }
			set { shadowDirectionalLightMaxTextureCount = value; }
		}

		static int shadowSpotLightTextureSize = 2048;
		[Config( "Video", "shadowSpotLightTextureSize" )]
		public static int ShadowSpotLightTextureSize
		{
			get { return shadowSpotLightTextureSize; }
			set { shadowSpotLightTextureSize = value; }
		}

		static int shadowSpotLightMaxTextureCount = 2;
		[Config( "Video", "shadowSpotLightMaxTextureCount" )]
		public static int ShadowSpotLightMaxTextureCount
		{
			get { return shadowSpotLightMaxTextureCount; }
			set { shadowSpotLightMaxTextureCount = value; }
		}

		static int shadowPointLightTextureSize = 1024;
		[Config( "Video", "shadowPointLightTextureSize" )]
		public static int ShadowPointLightTextureSize
		{
			get { return shadowPointLightTextureSize; }
			set { shadowPointLightTextureSize = value; }
		}

		static int shadowPointLightMaxTextureCount = 2;
		[Config( "Video", "shadowPointLightMaxTextureCount" )]
		public static int ShadowPointLightMaxTextureCount
		{
			get { return shadowPointLightMaxTextureCount; }
			set { shadowPointLightMaxTextureCount = value; }
		}

		static WaterPlane.ReflectionLevels waterReflectionLevel = WaterPlane.ReflectionLevels.OnlyModels;
		[Config( "Video", "waterReflectionLevel" )]
		public static WaterPlane.ReflectionLevels WaterReflectionLevel
		{
			get { return waterReflectionLevel; }
			set { waterReflectionLevel = value; }
		}

		static bool showDecorativeObjects = true;
		[Config( "Video", "showDecorativeObjects" )]
		public static bool ShowDecorativeObjects
		{
			get { return showDecorativeObjects; }
			set { showDecorativeObjects = value; }
		}

		static float soundVolume = .8f;
		[Config( "Sound", "soundVolume" )]
		public static float SoundVolume
		{
			get { return soundVolume; }
			set
			{
				soundVolume = value;
				if( EngineApp.Instance.DefaultSoundChannelGroup != null )
					EngineApp.Instance.DefaultSoundChannelGroup.Volume = soundVolume;
			}
		}

		static float musicVolume = .8f;
		[Config( "Sound", "musicVolume" )]
		public static float MusicVolume
		{
			get { return musicVolume; }
			set
			{
				musicVolume = value;
				if( GameMusic.MusicChannelGroup != null )
					GameMusic.MusicChannelGroup.Volume = musicVolume;
			}
		}

		[Config( "Environment", "autorunMapName" )]
		public static string autorunMapName = "";

		//

		public static new GameEngineApp Instance
		{
			get { return instance; }
		}

		void ChangeToBetterDefaultSettings()
		{
			bool shadowTechniqueInitialized = false;
			bool shadowTextureSizeInitialized = false;

			if( !string.IsNullOrEmpty( EngineApp.ConfigName ) )
			{
				string error;
				TextBlock block = TextBlockUtils.LoadFromRealFile(
					VirtualFileSystem.GetRealPathByVirtual( EngineApp.ConfigName ), out error );
				if( block != null )
				{
					TextBlock blockVideo = block.FindChild( "Video" );
					if( blockVideo != null )
					{
						if( blockVideo.IsAttributeExist( "shadowTechnique" ) )
							shadowTechniqueInitialized = true;
						if( blockVideo.IsAttributeExist( "shadowDirectionalLightTextureSize" ) )
							shadowTextureSizeInitialized = true;
					}
				}
			}

			//shadowTechnique
			if( !shadowTechniqueInitialized )
			{
				if( RenderSystem.Instance.GPUIsGeForce() )
				{
					if( RenderSystem.Instance.GPUCodeName >= GPUCodeNames.GeForce_NV10 &&
						RenderSystem.Instance.GPUCodeName <= GPUCodeNames.GeForce_NV40 )
					{
						shadowTechnique = ShadowTechniques.ShadowmapLow;
					}
					if( RenderSystem.Instance.GPUCodeName == GPUCodeNames.GeForce_G70 )
						shadowTechnique = ShadowTechniques.ShadowmapMedium;
				}

				if( RenderSystem.Instance.GPUIsRadeon() )
				{
					if( RenderSystem.Instance.GPUCodeName >= GPUCodeNames.Radeon_R100 &&
						RenderSystem.Instance.GPUCodeName <= GPUCodeNames.Radeon_R400 )
					{
						shadowTechnique = ShadowTechniques.ShadowmapLow;
					}
					if( RenderSystem.Instance.GPUCodeName == GPUCodeNames.Radeon_R500 )
						shadowTechnique = ShadowTechniques.ShadowmapMedium;
				}

				if( !RenderSystem.Instance.HasShaderModel2() )
					shadowTechnique = ShadowTechniques.None;
			}

			//shadow texture size
			if( !shadowTextureSizeInitialized )
			{
				if( RenderSystem.Instance.GPUIsGeForce() )
				{
					if( RenderSystem.Instance.GPUCodeName >= GPUCodeNames.GeForce_NV10 &&
						RenderSystem.Instance.GPUCodeName <= GPUCodeNames.GeForce_G70 )
					{
						shadowDirectionalLightTextureSize = 1024;
						shadowSpotLightTextureSize = 1024;
						shadowPointLightTextureSize = 512;
					}
				}
				else if( RenderSystem.Instance.GPUIsRadeon() )
				{
					if( RenderSystem.Instance.GPUCodeName >= GPUCodeNames.Radeon_R100 &&
						RenderSystem.Instance.GPUCodeName <= GPUCodeNames.Radeon_R500 )
					{
						shadowDirectionalLightTextureSize = 1024;
						shadowSpotLightTextureSize = 1024;
						shadowPointLightTextureSize = 512;
					}
				}
				else
				{
					shadowDirectionalLightTextureSize = 1024;
					shadowSpotLightTextureSize = 1024;
					shadowPointLightTextureSize = 512;
				}
			}
		}

		float loadingCallbackLastTimeCall;
		void LongOperationCallbackManager_LoadingCallback( string callerInfo, object userData )
		{
			//How to calculate time for progress bar.
			//It is impossible to make universal solution. It's depending to concrete project.
			//By "callerInfo" data you can collect useful info for calculation total loading time.

			//Limit fps.
			const float maxFPSInv = 1.0f / 15.0f;
			float now = Time;
			if( now - loadingCallbackLastTimeCall < maxFPSInv )
				return;
			loadingCallbackLastTimeCall = now;

			//animate "Indicator".
			EControl loadingWindow = userData as EControl;
			if( loadingWindow != null )
			{
				EControl indicator = loadingWindow.Controls[ "Indicator" ];
				if( indicator != null )
				{
					int frame = (int)( ( EngineApp.Instance.Time * 10 ) % 8 );
					indicator.BackTextureCoord =
						new Rect( (float)frame / 8, 0, (float)( frame + 1 ) / 8, 1 );
				}
			}

			//Update frame (2D only).
			SceneManager.Instance.Enable3DSceneRendering = false;
			RenderScene();
			SceneManager.Instance.Enable3DSceneRendering = true;
		}

		protected override void OnBeforeRendererWorldInit()
		{
			base.OnBeforeRendererWorldInit();

			//here you can set default settings like:
			//RendererWorld.InitializationOptions.FullSceneAntialiasing.
		}

		protected override void OnAfterRendererWorldInit()
		{
			base.OnAfterRendererWorldInit();

			//We will load materials later (in OnCreate()), during showing "ProgramLoadingWindow.gui".
			HighLevelMaterialManager.Instance.NeedLoadAllMaterialsAtStartup = false;
		}

		protected override bool OnCreate()
		{
			instance = this;

			ChangeToBetterDefaultSettings();

			if( !base.OnCreate() )
				return false;

			SoundVolume = soundVolume;
			MusicVolume = musicVolume;

			controlManager = new ScreenControlManager( ScreenGuiRenderer );
			if( !ControlsWorld.Init() )
				return false;

			_ShowSystemCursor = _ShowSystemCursor;
			_DrawFPS = _DrawFPS;
			MaterialScheme = materialScheme;

			Log.Handlers.InfoHandler += Log_Handlers_InfoHandler;
			Log.Handlers.WarningHandler += Log_Handlers_WarningHandler;
			Log.Handlers.ErrorHandler += Log_Handlers_ErrorHandler;
			Log.Handlers.FatalHandler += Log_Handlers_FatalHandler;

			//Camera
			Camera camera = RendererWorld.Instance.DefaultCamera;
			camera.NearClipDistance = .1f;
			camera.FarClipDistance = 1000.0f;
			camera.FixedUp = Vec3.ZAxis;
			camera.Fov = 90;
			camera.Position = new Vec3( -10, -10, 10 );
			camera.LookAt( new Vec3( 0, 0, 0 ) );

			EControl programLoadingWindow = ControlDeclarationManager.Instance.CreateControl(
				"Gui\\ProgramLoadingWindow.gui" );
			if( programLoadingWindow != null )
				controlManager.Controls.Add( programLoadingWindow );

			//Subcribe to callbacks during engine loading. We will render scene from callback.
			LongOperationCallbackManager.Subscribe( LongOperationCallbackManager_LoadingCallback,
				programLoadingWindow );

			if( !HighLevelMaterialManager.Instance.LoadAllMaterials() )
			{
				LongOperationCallbackManager.Unsubscribe();
				return true;
			}

			RenderScene();

			//Game controls
			GameControlsManager.Init();

			//EntitySystem
			if( !EntitySystemWorld.Init( new EntitySystemWorld() ) )
			{
				LongOperationCallbackManager.Unsubscribe();
				return true;
			}

			LongOperationCallbackManager.Unsubscribe();

			//close loading window.
			if( programLoadingWindow != null )
				programLoadingWindow.SetShouldDetach();

			string mapName = "";

			if( autorunMapName != "" && autorunMapName.Length > 2 )
			{
				mapName = autorunMapName;
				if( !mapName.Contains( "\\" ) && !mapName.Contains( "/" ) )
					mapName = "Maps/" + mapName + "/Map.map";
			}

			if( !WebPlayerMode )
			{
				string[] commandLineArgs = Environment.GetCommandLineArgs();
				if( commandLineArgs.Length > 1 )
				{
					string name = commandLineArgs[ 1 ];
					if( name[ 0 ] == '\"' && name[ name.Length - 1 ] == '\"' )
						name = name.Substring( 1, name.Length - 2 );
					name = name.Replace( '/', '\\' );

					string dataDirectory = VirtualFileSystem.ResourceDirectoryPath;
					dataDirectory = dataDirectory.Replace( '/', '\\' );

					if( name.Length > dataDirectory.Length )
						if( string.Compare( name.Substring( 0, dataDirectory.Length ), dataDirectory, true ) == 0 )
							name = name.Substring( dataDirectory.Length + 1 );

					mapName = name;
				}
			}

			if( mapName != "" )
			{
				if( !ServerOrSingle_MapLoad( mapName, EntitySystemWorld.Instance.DefaultWorldType, false ) )
				{
					//Error
					foreach( EControl control in controlManager.Controls )
					{
						if( control is MessageBoxWindow && !control.IsShouldDetach() )
							return true;
					}

					GameMusic.MusicPlay( "Sounds\\Music\\MainMenu.ogg", true );
					controlManager.Controls.Add( new EngineLogoWindow() );
				}
			}
			else
			{
				GameMusic.MusicPlay( "Sounds\\Music\\MainMenu.ogg", true );
				controlManager.Controls.Add( new EngineLogoWindow() );
			}

			//showDebugInformation console command
			if( EngineConsole.Instance != null )
			{
				EngineConsole.Instance.AddCommand( "showDebugInformationWindow",
					ConsoleCommand_ShowDebugInformationWindow );
			}

			//example of custom input device
			//ExampleCustomInputDevice.InitDevice();

			return true;
		}

		protected override void OnDestroy()
		{
			MapSystemWorld.MapDestroy();
			if( EntitySystemWorld.Instance != null )
				EntitySystemWorld.Instance.WorldDestroy();

			Server_DestroyServer( "The server has been destroyed" );
			Client_DisconnectFromServer();

			EntitySystemWorld.Shutdown();

			GameControlsManager.Shutdown();

			ControlsWorld.Shutdown();
			controlManager = null;

			EngineConsole.Shutdown();

			instance = null;
			base.OnDestroy();
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			if( EngineConsole.Instance != null )
				if( EngineConsole.Instance.DoKeyDown( e ) )
					return true;
			if( controlManager != null )
				if( controlManager.DoKeyDown( e ) )
					return true;

			//Debug information window
			if( e.Key == EKeys.F11 )
			{
				bool show = DebugInformationWindow.Instance == null;
				ShowDebugInformationWindow( show );
				return true;
			}

			//make screenshot
			if( e.Key == EKeys.F12 )
			{
				if( !WebPlayerMode )
				{
					MakeScreenshot();
					return true;
				}
			}

			return base.OnKeyDown( e );
		}

		protected override bool OnKeyPress( KeyPressEvent e )
		{
			if( EngineConsole.Instance != null )
				if( EngineConsole.Instance.DoKeyPress( e ) )
					return true;
			if( controlManager != null )
				if( controlManager.DoKeyPress( e ) )
					return true;

			return base.OnKeyPress( e );
		}

		protected override bool OnKeyUp( KeyEvent e )
		{
			if( controlManager != null )
				if( controlManager.DoKeyUp( e ) )
					return true;
			return base.OnKeyUp( e );
		}

		protected override bool OnMouseDown( EMouseButtons button )
		{
			if( controlManager != null )
				if( controlManager.DoMouseDown( button ) )
					return true;
			return base.OnMouseDown( button );
		}

		protected override bool OnMouseUp( EMouseButtons button )
		{
			if( controlManager != null )
				if( controlManager.DoMouseUp( button ) )
					return true;
			return base.OnMouseUp( button );
		}

		protected override bool OnMouseDoubleClick( EMouseButtons button )
		{
			if( controlManager != null )
				if( controlManager.DoMouseDoubleClick( button ) )
					return true;
			return base.OnMouseDoubleClick( button );
		}

		protected override void OnMouseMove( Vec2 mouse )
		{
			base.OnMouseMove( mouse );
			if( controlManager != null )
				controlManager.DoMouseMove( mouse );
		}

		protected override bool OnMouseWheel( int delta )
		{
			if( controlManager != null )
				if( controlManager.DoMouseWheel( delta ) )
					return true;
			return base.OnMouseWheel( delta );
		}

		protected override bool OnJoystickEvent( JoystickInputEvent e )
		{
			if( controlManager != null )
				if( controlManager.DoJoystickEvent( e ) )
					return true;
			return base.OnJoystickEvent( e );
		}

		protected override bool OnCustomInputDeviceEvent( InputEvent e )
		{
			if( controlManager != null )
				if( controlManager.DoCustomInputDeviceEvent( e ) )
					return true;
			return base.OnCustomInputDeviceEvent( e );
		}

		protected override void OnSystemPause( bool pause )
		{
			base.OnSystemPause( pause );

			if( EntitySystemWorld.Instance != null )
				EntitySystemWorld.Instance.SystemPauseOfSimulation = pause;
		}

		protected override void OnTick( float delta )
		{
			base.OnTick( delta );

			if( needMapLoadName != null )
			{
				string name = needMapLoadName;
				needMapLoadName = null;
				ServerOrSingle_MapLoad( name, EntitySystemWorld.Instance.DefaultWorldType, false );
			}
			if( needMapCreateForDynamicMapExample )
			{
				needMapCreateForDynamicMapExample = false;
				DynamicCreatedMapExample.ServerOrSingle_MapCreate();
			}
			if( needWorldLoadName != null )
			{
				string name = needWorldLoadName;
				needWorldLoadName = null;
				WorldLoad( name );
			}

			if( EngineConsole.Instance != null )
				EngineConsole.Instance.DoTick( delta );
			controlManager.DoTick( delta );

			//update server
			GameNetworkServer server = GameNetworkServer.Instance;
			if( server != null )
				server.Update();

			//update client
			GameNetworkClient client = GameNetworkClient.Instance;
			if( client != null )
			{
				client.Update();

				//check for disconnection
				if( client_AllowCheckForDisconnection )
				{
					if( client.Status == NetworkConnectionStatuses.Disconnected )
					{
						Client_DisconnectFromServer();

						Log.Error( "Disconnected from server.\n\nReason: \"{0}\"",
							client.DisconnectionReason );
					}
				}
			}
		}

		protected override void OnRenderFrame()
		{
			base.OnRenderFrame();

			if( !WebPlayerMode )
				SystemCursorFileName = "Cursors\\DefaultSystem.cur";
			controlManager.DoRender();
		}

		protected override void OnRenderScreenUI( GuiRenderer renderer )
		{
			base.OnRenderScreenUI( renderer );

			if( Map.Instance != null )
				Map.Instance.DoDebugRenderUI( renderer );

			DoFullscreenFading( renderer );

			controlManager.DoRenderUI( renderer );

			if( EngineConsole.Instance != null )
				EngineConsole.Instance.DoRenderUI( renderer );
		}

		void DoFullscreenFading( GuiRenderer renderer )
		{
			if( fullscreenFadingRemainingFrames > 0 )
			{
				fullscreenFadingRemainingFrames--;

				float alpha = (float)fullscreenFadingRemainingFrames / 30;
				MathFunctions.Saturate( ref alpha );
				renderer.AddQuad( new Rect( 0, 0, 1, 1 ), new ColorValue( 0, 0, 0, alpha ) );
			}
		}

		public void CreateGameWindowForMap()
		{
			//close all windows
			foreach( EControl control in controlManager.Controls )
				control.SetShouldDetach();

			GameWindow gameWindow = null;

			//Create specific game window
			if( GameMap.Instance != null )
				gameWindow = CreateGameWindowByGameType( GameMap.Instance.GameType );

			if( gameWindow == null )
				gameWindow = new ActionGameWindow();

			controlManager.Controls.Add( gameWindow );
		}

		public void DeleteAllGameWindows()
		{
			ttt:
			foreach( EControl control in controlManager.Controls )
			{
				if( control is GameWindow )
				{
					controlManager.Controls.Remove( control );
					goto ttt;
				}
			}
		}

		public bool ServerOrSingle_MapLoad( string fileName, WorldType worldType,
			bool noChangeWindows )
		{
			GameNetworkServer server = GameNetworkServer.Instance;

			EControl mapLoadingWindow = null;

			//show map loading window
			if( !noChangeWindows )
			{
				mapLoadingWindow = ControlDeclarationManager.Instance.CreateControl(
					"Gui\\MapLoadingWindow.gui" );
				if( mapLoadingWindow != null )
				{
					mapLoadingWindow.Text = fileName;
					controlManager.Controls.Add( mapLoadingWindow );
				}
				RenderScene();
			}

			DeleteAllGameWindows();

			MapSystemWorld.MapDestroy();

			//create world if need
			if( World.Instance == null || World.Instance.Type != worldType )
			{
				WorldSimulationTypes worldSimulationType;
				EntitySystemWorld.NetworkingInterface networkingInterface = null;

				if( server != null )
				{
					worldSimulationType = WorldSimulationTypes.ServerAndClient;
					networkingInterface = server.EntitySystemService.NetworkingInterface;
				}
				else
					worldSimulationType = WorldSimulationTypes.Single;

				if( !EntitySystemWorld.Instance.WorldCreate( worldSimulationType, worldType,
					networkingInterface ) )
				{
					Log.Fatal( "GameEngineApp: MapLoad: EntitySystemWorld.WorldCreate failed." );
				}
			}

			//Subcribe to callbacks during map loading. We will render scene from callback.
			LongOperationCallbackManager.Subscribe( LongOperationCallbackManager_LoadingCallback,
				mapLoadingWindow );

			//load map
			if( !MapSystemWorld.MapLoad( fileName ) )
			{
				if( mapLoadingWindow != null )
					mapLoadingWindow.SetShouldDetach();

				LongOperationCallbackManager.Unsubscribe();

				return false;
			}

			//inform clients about world created
			if( server != null )
				server.EntitySystemService.InformClientsAfterWorldCreated();

			//Simulate physics for 5 seconds. That the physics has fallen asleep.
			if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
				SimulatePhysicsForLoadedMap( 5 );

			//Update shadow settings. This operation can be slow because need update all 
			//shaders if shadow technique changed.
			Map.Instance.UpdateSceneManagerShadowSettings();

			fullscreenFadingRemainingFrames = 35;

			LongOperationCallbackManager.Unsubscribe();

			//Error
			foreach( EControl control in controlManager.Controls )
			{
				if( control is MessageBoxWindow && !control.IsShouldDetach() )
					return false;
			}

			if( !noChangeWindows )
				CreateGameWindowForMap();

			//play music
			if( !noChangeWindows )
			{
				if( GameMap.Instance != null )
					GameMusic.MusicPlay( GameMap.Instance.GameMusic, true );
			}

			EntitySystemWorld.Instance.ResetExecutedTime();

			return true;
		}

		void SimulatePhysicsForLoadedMap( float seconds )
		{
			//inform entities about this simulation
			foreach( Entity entity in Map.Instance.Children )
			{
				Dynamic dynamic = entity as Dynamic;
				if( dynamic != null )
					dynamic.SuspendPhysicsDuringMapLoading( true );
			}

			PhysicsWorld.Instance.EnableCollisionEvents = false;

			for( float time = 0; time < seconds; time += Entity.TickDelta )
			{
				PhysicsWorld.Instance.Simulate( Entity.TickDelta );

				//WaterPlane specific
				foreach( WaterPlane waterPlane in WaterPlane.Instances )
					waterPlane.TickPhysicsInfluence( false );
			}

			PhysicsWorld.Instance.EnableCollisionEvents = true;

			//inform entities about this simulation
			foreach( Entity entity in Map.Instance.Children )
			{
				Dynamic dynamic = entity as Dynamic;
				if( dynamic != null )
					dynamic.SuspendPhysicsDuringMapLoading( false );
			}
		}

		public bool WorldLoad( string fileName )
		{
			EControl worldLoadingWindow = null;

			//world loading window
			{
				worldLoadingWindow = ControlDeclarationManager.Instance.CreateControl(
					"Gui\\WorldLoadingWindow.gui" );
				if( worldLoadingWindow != null )
				{
					worldLoadingWindow.Text = fileName;
					controlManager.Controls.Add( worldLoadingWindow );
				}
				RenderScene();
			}

			DeleteAllGameWindows();

			//Subcribe to callbacks during engine loading. We will render scene from callback.
			LongOperationCallbackManager.Subscribe( LongOperationCallbackManager_LoadingCallback,
				worldLoadingWindow );

			if( !MapSystemWorld.WorldLoad( WorldSimulationTypes.Single, fileName ) )
			{
				if( worldLoadingWindow != null )
					worldLoadingWindow.SetShouldDetach();

				LongOperationCallbackManager.Unsubscribe();

				return false;
			}

			//Update shadow settings. This operation can be slow because need update all 
			//shaders if shadow technique changed.
			Map.Instance.UpdateSceneManagerShadowSettings();

			fullscreenFadingRemainingFrames = 35;

			LongOperationCallbackManager.Unsubscribe();

			//Error
			foreach( EControl control in controlManager.Controls )
			{
				if( control is MessageBoxWindow && !control.IsShouldDetach() )
					return false;
			}

			//create game window
			CreateGameWindowForMap();

			//play music
			if( GameMap.Instance != null )
				GameMusic.MusicPlay( GameMap.Instance.GameMusic, true );

			return true;
		}

		public bool WorldSave( string fileName )
		{
			EControl worldSavingWindow = null;

			//world saving window
			{
				worldSavingWindow = ControlDeclarationManager.Instance.CreateControl(
					"Gui\\WorldSavingWindow.gui" );
				if( worldSavingWindow != null )
				{
					worldSavingWindow.Text = fileName;
					controlManager.Controls.Add( worldSavingWindow );
				}
				RenderScene();
			}

			GameWindow gameWindow = null;
			foreach( EControl control in controlManager.Controls )
			{
				gameWindow = control as GameWindow;
				if( gameWindow != null )
					break;
			}
			if( gameWindow != null )
				gameWindow.OnBeforeWorldSave();

			bool result = MapSystemWorld.WorldSave( fileName );

			if( worldSavingWindow != null )
				worldSavingWindow.SetShouldDetach();

			return result;
		}

		public void SetNeedMapLoad( string fileName )
		{
			needMapLoadName = fileName;
		}

		public void SetNeedMapCreateForDynamicMapExample()
		{
			needMapCreateForDynamicMapExample = true;
		}

		public void SetNeedWorldLoad( string fileName )
		{
			needWorldLoadName = fileName;
		}

		GameWindow CreateGameWindowByGameType( GameMap.GameTypes gameType )
		{
			switch( gameType )
			{
			case GameMap.GameTypes.Action:
			case GameMap.GameTypes.TPSArcade:
				return new ActionGameWindow();

			case GameMap.GameTypes.RTS:
				return new RTSGameWindow();

			case GameMap.GameTypes.TurretDemo:
				return new TurretDemoGameWindow();

			case GameMap.GameTypes.JigsawPuzzleGame:
				return new JigsawPuzzleGameWindow();

			//Here it is necessary to add a your specific game mode.
			//..

			}

			return null;
		}

		public void Client_OnConnectedToServer()
		{
			//add handlers for entity system service events
			GameNetworkClient client = GameNetworkClient.Instance;
			client.EntitySystemService.WorldCreateBegin += Client_EntitySystemService_WorldCreateBegin;
			client.EntitySystemService.WorldCreateEnd += Client_EntitySystemService_WorldCreateEnd;

			SuspendWorkingWhenApplicationIsNotActive = false;
		}

		public void Client_DisconnectFromServer()
		{
			GameNetworkClient client = GameNetworkClient.Instance;

			if( client != null )
			{
				//remove handlers for entity system service events
				client.EntitySystemService.WorldCreateBegin -= Client_EntitySystemService_WorldCreateBegin;
				client.EntitySystemService.WorldCreateEnd -= Client_EntitySystemService_WorldCreateEnd;
				client.Dispose();

				SuspendWorkingWhenApplicationIsNotActive = true;
			}
		}

		void Client_EntitySystemService_WorldCreateBegin( EntitySystemClientNetworkService sender,
			WorldType worldType, string mapVirtualFileName )
		{
			//show map loading window
			EControl mapLoadingWindow = ControlDeclarationManager.Instance.CreateControl(
				"Gui\\MapLoadingWindow.gui" );
			if( mapLoadingWindow != null )
			{
				mapLoadingWindow.Text = mapVirtualFileName;
				controlManager.Controls.Add( mapLoadingWindow );
			}
			RenderScene();

			DeleteAllGameWindows();

			MapSystemWorld.MapDestroy();

			if( !EntitySystemWorld.Instance.WorldCreate( WorldSimulationTypes.ClientOnly,
				worldType, sender.NetworkingInterface ) )
			{
				Log.Fatal( "GameEngineApp: Client_EntitySystemService_WorldCreateBegin: " +
					"EntitySystemWorld.WorldCreate failed." );
			}
		}

		void Client_EntitySystemService_WorldCreateEnd( EntitySystemClientNetworkService sender )
		{
			//dynamic created map example
			if( string.IsNullOrEmpty( Map.Instance.VirtualFileName ) )
				DynamicCreatedMapExample.Client_CreateEntities();

			//play music
			if( GameMap.Instance != null )
				GameMusic.MusicPlay( GameMap.Instance.GameMusic, true );

			CreateGameWindowForMap();
		}

		public void Server_OnCreateServer()
		{
			SuspendWorkingWhenApplicationIsNotActive = false;
		}

		public void Server_DestroyServer( string reason )
		{
			GameNetworkServer server = GameNetworkServer.Instance;
			if( server != null )
			{
				server.Dispose( reason );

				SuspendWorkingWhenApplicationIsNotActive = true;
			}
		}

		public bool Client_AllowCheckForDisconnection
		{
			get { return client_AllowCheckForDisconnection; }
			set { client_AllowCheckForDisconnection = value; }
		}

		void Log_Handlers_InfoHandler( string text )
		{
			if( EngineConsole.Instance != null )
				EngineConsole.Instance.Print( text );
		}

		void Log_Handlers_WarningHandler( string text, ref bool handled )
		{
			if( EngineConsole.Instance != null )
			{
				handled = true;
				EngineConsole.Instance.Print( "Warning: " + text, new ColorValue( 1, 0, 0 ) );
				EngineConsole.Instance.Active = true;
			}
		}

		void Log_Handlers_ErrorHandler( string text, ref bool handled )
		{
			if( controlManager != null )
			{
				handled = true;

				//find already created MessageBoxWindow
				foreach( EControl control in controlManager.Controls )
				{
					if( control is MessageBoxWindow && !control.IsShouldDetach() )
						return;
				}

				if( Map.Instance != null )
				{
					if( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() )
						EntitySystemWorld.Instance.Simulation = false;
				}

				EngineApp.Instance.MouseRelativeMode = false;

				DeleteAllGameWindows();

				MapSystemWorld.MapDestroy();
				if( EntitySystemWorld.Instance != null )
					EntitySystemWorld.Instance.WorldDestroy();

				GameEngineApp.Instance.Server_DestroyServer( "Error on the server" );
				GameEngineApp.Instance.Client_DisconnectFromServer();

				//show message box

				MessageBoxWindow messageBoxWindow = new MessageBoxWindow( text, "Error",
					delegate( EButton sender )
					{
						//close all windows
						foreach( EControl control in controlManager.Controls )
							control.SetShouldDetach();

						if( EntitySystemWorld.Instance == null )
						{
							EngineApp.Instance.SetNeedExit();
							return;
						}

						//create main menu
						controlManager.Controls.Add( new MainMenuWindow() );

					} );

				controlManager.Controls.Add( messageBoxWindow );
			}
		}

		void Log_Handlers_FatalHandler( string text, string createdLogFilePath, ref bool handled )
		{
			if( controlManager != null )
			{
				//find already created MessageBoxWindow
				foreach( EControl control in controlManager.Controls )
				{
					if( control is MessageBoxWindow && !control.IsShouldDetach() )
					{
						handled = true;
						return;
					}
				}
			}
		}

		void MakeScreenshot()
		{
			string directoryName = VirtualFileSystem.GetRealPathByVirtual( "user:Screenshots" );

			if( !Directory.Exists( directoryName ) )
				Directory.CreateDirectory( directoryName );

			string format = Path.Combine( directoryName, "Screenshot{0}.tga" );

			for( int n = 1; n < 1000; n++ )
			{
				string v = n.ToString();
				if( n < 10 )
					v = "0" + v;
				if( n < 100 )
					v = "0" + v;

				string fileName = string.Format( format, v );

				if( !File.Exists( fileName ) )
				{
					RendererWorld.Instance.RenderWindow.WriteContentsToFile( fileName );
					break;
				}
			}
		}

		static void ConsoleCommand_ShowDebugInformationWindow( string arguments )
		{
			bool show = DebugInformationWindow.Instance == null;

			try
			{
				show = (bool)SimpleTypesUtils.GetSimpleTypeValue( typeof( bool ), arguments );
			}
			catch { }

			ShowDebugInformationWindow( show );
		}

		static void ShowDebugInformationWindow( bool show )
		{
			if( show )
			{
				if( DebugInformationWindow.Instance == null )
				{
					DebugInformationWindow window = new DebugInformationWindow();
					Instance.ControlManager.Controls.Add( window );
				}
			}
			else
			{
				if( DebugInformationWindow.Instance != null )
					DebugInformationWindow.Instance.SetShouldDetach();
			}
		}

		public ScreenControlManager ControlManager
		{
			get { return controlManager; }
		}

		protected override void OnRegisterConfigParameter( Config.Parameter parameter )
		{
			base.OnRegisterConfigParameter( parameter );

			if( EngineConsole.Instance != null )
				EngineConsole.Instance.RegisterConfigParameter( parameter );
		}

		protected override void OnBeforeUpdateShadowSettings()
		{
			base.OnBeforeUpdateShadowSettings();

			//Override map's shadow settings by game options.
			if( Map.Instance != null )
			{
				Map map = Map.Instance;

				map.ShadowTechnique = GameEngineApp.ShadowTechnique;

				if( GameEngineApp.ShadowUseMapSettings )
				{
					GameEngineApp.shadowPSSMSplitFactors = map.InitialShadowPSSMSplitFactors;
					GameEngineApp.ShadowFarDistance = map.InitialShadowFarDistance;
					GameEngineApp.ShadowColor = map.InitialShadowColor;
				}
				map.ShadowColor = GameEngineApp.ShadowColor;
				map.ShadowFarDistance = GameEngineApp.ShadowFarDistance;
				map.ShadowPSSMSplitFactors = GameEngineApp.ShadowPSSMSplitFactors;

				switch( GameEngineApp.ShadowDirectionalLightTextureSize )
				{
				case 256: map.ShadowDirectionalLightTextureSize = Map.ShadowTextureSizes.Size256x256; break;
				case 512: map.ShadowDirectionalLightTextureSize = Map.ShadowTextureSizes.Size512x512; break;
				case 1024: map.ShadowDirectionalLightTextureSize = Map.ShadowTextureSizes.Size1024x1024; break;
				case 2048: map.ShadowDirectionalLightTextureSize = Map.ShadowTextureSizes.Size2048x2048; break;
				default: Log.Warning( "Invalid GameEngineApp.ShadowDirectionalLightTextureSize value." ); break;
				}
				map.ShadowDirectionalLightMaxTextureCount = GameEngineApp.ShadowDirectionalLightMaxTextureCount;

				switch( GameEngineApp.ShadowSpotLightTextureSize )
				{
				case 256: map.ShadowSpotLightTextureSize = Map.ShadowTextureSizes.Size256x256; break;
				case 512: map.ShadowSpotLightTextureSize = Map.ShadowTextureSizes.Size512x512; break;
				case 1024: map.ShadowSpotLightTextureSize = Map.ShadowTextureSizes.Size1024x1024; break;
				case 2048: map.ShadowSpotLightTextureSize = Map.ShadowTextureSizes.Size2048x2048; break;
				default: Log.Warning( "Invalid GameEngineApp.ShadowSpotLightTextureSize value." ); break;
				}
				map.ShadowSpotLightMaxTextureCount = GameEngineApp.ShadowSpotLightMaxTextureCount;

				switch( GameEngineApp.ShadowPointLightTextureSize )
				{
				case 256: map.ShadowPointLightTextureSize = Map.ShadowTextureSizes.Size256x256; break;
				case 512: map.ShadowPointLightTextureSize = Map.ShadowTextureSizes.Size512x512; break;
				case 1024: map.ShadowPointLightTextureSize = Map.ShadowTextureSizes.Size1024x1024; break;
				case 2048: map.ShadowPointLightTextureSize = Map.ShadowTextureSizes.Size2048x2048; break;
				default: Log.Warning( "Invalid GameEngineApp.ShadowPointLightTextureSize value." ); break;
				}
				map.ShadowPointLightMaxTextureCount = GameEngineApp.ShadowPointLightMaxTextureCount;
			}
		}

	}
}
