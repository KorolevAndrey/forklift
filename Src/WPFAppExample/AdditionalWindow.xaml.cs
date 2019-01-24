// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Engine.MathEx;
using Engine.Renderer;
using Engine.EntitySystem;
using Engine.MapSystem;

namespace WPFAppExample
{
	/// <summary>
	/// Interaction logic for AdditionalWindow.xaml
	/// </summary>
	public partial class AdditionalWindow : Window
	{
		public AdditionalWindow()
		{
			InitializeComponent();
		}

		private void button1_Click( object sender, RoutedEventArgs e )
		{
			Close();
		}

		private void Grid_Loaded( object sender, RoutedEventArgs e )
		{
			renderTargetUserControl1.AutomaticUpdateFPS = 60;
			renderTargetUserControl1.Render += renderTargetUserControl1_Render;
		}

		void renderTargetUserControl1_Render( Camera camera )
		{
			//update camera
			if( Map.Instance != null )
			{
				Vec3 position;
				Vec3 forward;
				Degree fov;

				MapCamera mapCamera = Entities.Instance.GetByName( "MapCamera_1" ) as MapCamera;
				if( mapCamera != null )
				{
					position = mapCamera.Position;
					forward = mapCamera.Rotation * new Vec3( 1, 0, 0 );
					fov = mapCamera.Fov;
				}
				else
				{
					position = Map.Instance.EditorCameraPosition;
					forward = Map.Instance.EditorCameraDirection.GetVector();
					fov = Map.Instance.Fov;
				}

				if( fov == 0 )
					fov = Map.Instance.Fov;

				renderTargetUserControl1.CameraNearFarClipDistance = Map.Instance.NearFarClipDistance;
				renderTargetUserControl1.CameraFixedUp = Vec3.ZAxis;
				renderTargetUserControl1.CameraFov = fov;
				renderTargetUserControl1.CameraPosition = position;
				renderTargetUserControl1.CameraDirection = forward;
			}
		}
	}
}
