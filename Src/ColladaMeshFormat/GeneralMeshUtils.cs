// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using Engine;
using Engine.Renderer;
using Engine.MathEx;
using Engine.Utils;

namespace GeneralMeshUtils
{
	////////////////////////////////////////////////////////////////////////////////////////////////

	[StructLayout( LayoutKind.Sequential )]
	public struct SubMeshVertex
	{
		public Vec3 position;
		public Vec3 normal;
		public ColorValue color;
		public Vec2 texCoord0;
		public Vec2 texCoord1;
		public Vec2 texCoord2;
		public Vec2 texCoord3;
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	public interface ISceneMaterial
	{
		string Name { get; }
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	public struct SceneBoneAssignmentItem
	{
		int boneIndex;
		float weight;

		//

		public int BoneIndex
		{
			get { return boneIndex; }
			set { boneIndex = value; }
		}

		public float Weight
		{
			get { return weight; }
			set { weight = value; }
		}

		public override string ToString()
		{
			return string.Format( "{0} {1}", boneIndex, weight );
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	public struct PoseOffset
	{
		public int index;
		public Vec3 offset;
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	public struct PoseInfo
	{
		public String name;
		public PoseOffset[] offsets;
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	public struct PoseReference
	{
		public int index;
		public float weight;
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	public enum UVUnwrapChannels
	{
		None,
		TexCoord0,
		TexCoord1,
		TexCoord2,
		TexCoord3,
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	public interface ISceneSubMesh
	{
		ISceneMaterial Material { get; }

		void GetGeometry( out SubMeshVertex[] vertices, out int[] indices );

		SceneBoneAssignmentItem[] GetVertexBoneAssignment( int vertexIndex );

		void GetVerticesByTime( float timeFrame, out Vec3[] vertices, bool skeletonOn );

		void GetPoseReferenceByTime( float timeFrame, out PoseReference[] poseReferences );
		void GetPoses( out PoseInfo[] poses );

		int GetTextureCoordCount();
		bool HasVertexColors();
		bool HasPoses();

		bool AllowCollision { get; }
		UVUnwrapChannels UVUnwrapChannel { get; }
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	public interface ISceneBone
	{
		string Name { get; }
		ISceneBone Parent { get; }

		void GetInitialTransform( out Vec3 position, out Quat rotation, out Vec3 scale );
		void GetTransform( float timeFrame, out Vec3 position, out Quat rotation, out Vec3 scale );
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	public interface IMeshSceneObject
	{
		ISceneSubMesh[] SubMeshes { get; }

		IList<ISceneBone> SkinBones { get; }
		int AnimationFrameRate { get; }

		void GetBoundsByTime( float timeFrame, out Bounds bounds, out float radius );
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	public class MeshConstructor
	{
		//data
		bool tangents;
		bool edgeList;
		bool allowMergeSubMeshes;

		//not realized
		////lods
		//const int lodsCountDefault = 0;
		//const float lodsDistanceDefault = 10;
		//const float lodsReductionDefault = .3f;
		//int lodsCount = lodsCountDefault;
		//float lodsDistance = lodsDistanceDefault;
		//float lodsReduction = lodsReductionDefault;

		//!!!!not realized
		//animation
		//bool exportSkeleton = false;
		List<AnimationItem> animations = new List<AnimationItem>();
		float frameStep = 1;
		float morphInterpolationError = .005f;

		///////////////////////////////////////////

		public enum AnimationTypes
		{
			Skeleton,
			Morph,
			Pose,
			SkeletonAndMorph,
			SkeletonAndPose
		}

		///////////////////////////////////////////

		//!!!!!
		public class AnimationItem
		{
			string name = "";
			int frameBegin;
			int frameEnd;
			AnimationTypes format = AnimationTypes.Skeleton;

			//

			public string Name
			{
				get { return name; }
				set { name = value; }
			}

			public int FrameBegin
			{
				get { return frameBegin; }
				set { frameBegin = value; }
			}

			public int FrameEnd
			{
				get { return frameEnd; }
				set { frameEnd = value; }
			}

			public AnimationTypes Format
			{
				get { return format; }
				set { format = value; }
			}

			public override string ToString()
			{
				return Name;
			}

			public bool IsSkeletonAnimation()
			{
				return format == AnimationTypes.Skeleton ||
						  format == AnimationTypes.SkeletonAndPose ||
						  format == AnimationTypes.SkeletonAndMorph;
			}

			public bool IsPoseAnimation()
			{
				return format == AnimationTypes.Pose ||
					 format == AnimationTypes.SkeletonAndPose;
			}

			public bool IsMorphAnimation()
			{
				return format == AnimationTypes.Morph ||
					 format == AnimationTypes.SkeletonAndMorph;
			}

			public bool IsVertexAnimation()
			{
				return format == AnimationTypes.Morph ||
					 format == AnimationTypes.Pose ||
						  format == AnimationTypes.SkeletonAndPose ||
						  format == AnimationTypes.SkeletonAndMorph;
			}
		}

		///////////////////////////////////////////

		public MeshConstructor( bool tangents, bool edgeList, bool allowMergeSubMeshes )
		{
			this.tangents = tangents;
			this.edgeList = edgeList;
			this.allowMergeSubMeshes = allowMergeSubMeshes;
		}

		static bool IsPossibleGenerateTangents( Mesh mesh, out string errorText )
		{
			foreach( SubMesh subMesh in mesh.SubMeshes )
			{
				VertexData vertexData = subMesh.UseSharedVertices ?
					mesh.SharedVertexData : subMesh.VertexData;

				VertexDeclaration declaration = vertexData.VertexDeclaration;

				if( declaration.FindElementBySemantic( VertexElementSemantic.TextureCoordinates ) == -1 )
				{
					errorText = "No \"TextureCoordinates\" vertex element found";
					return false;
				}

				if( declaration.FindElementBySemantic( VertexElementSemantic.Normal ) == -1 )
				{
					errorText = "No \"Normal\" vertex element found";
					return false;
				}
			}

			errorText = null;
			return true;
		}

		string GetBoneUniqueName( string boneName, Dictionary<ISceneBone, string> bonesUniqueNames )
		{
			int number = 1;
			string nameWithNumber;

			do
			{
				nameWithNumber = String.Format( "{0}{1}", boneName, number );
				number++;
			}
			while( bonesUniqueNames.ContainsValue( nameWithNumber ) );

			return nameWithNumber;
		}

		bool TestBonesNamesUniqueness( IList<ISceneBone> skinBones, string meshName,
			 out Dictionary<ISceneBone, String> bonesUniqueNames )
		{
			bonesUniqueNames = new Dictionary<ISceneBone, String>();
			string bonesWithoutUniqueNameNames = "";
			int bonesWithoutUniqueNameCount = 0;

			foreach( ISceneBone sceneBone in skinBones )
			{
				if( bonesUniqueNames.ContainsValue( sceneBone.Name ) )
				{
					bonesUniqueNames.Add( sceneBone, GetBoneUniqueName( sceneBone.Name,
						bonesUniqueNames ) );

					if( bonesWithoutUniqueNameCount < 20 )
					{
						if( bonesWithoutUniqueNameNames != "" )
							bonesWithoutUniqueNameNames += ",";
						bonesWithoutUniqueNameNames = "\n\"" + sceneBone.Name + "\"";
					}
					else
					{
						if( bonesWithoutUniqueNameCount == 20 )
							bonesWithoutUniqueNameNames += ",\n...";
					}

					bonesWithoutUniqueNameCount++;
				}
				else
				{
					bonesUniqueNames.Add( sceneBone, sceneBone.Name );
				}
			}

			if( bonesWithoutUniqueNameCount == 0 )
				return true;

			return true;
		}

		//!!!!!!
		//bool DoExportSkeleton( IMeshSceneObject meshSceneObject, string skeletonName )
		//{
		//   if( meshSceneObject.SkinBones == null )
		//   {
		//      Log.Error( "ERROR: meshSceneObject.SkinBones == null." );
		//      return false;
		//   }

		//   string skeletonTempName = MeshManager.Instance.GetUniqueName( "_TempSkeletonName" );
		//   Skeleton skeleton = SkeletonManager.Instance.Create( skeletonTempName );

		//   Dictionary<ISceneBone, String> bonesNames;
		//   if( !TestBonesNamesUniqueness( meshSceneObject.SkinBones, meshSceneObject.Name,
		//      out bonesNames ) )
		//   {
		//      Log.Error( "ERROR: Skeleton has bones with equal names." );
		//      return false;
		//   }

		//   //create bones
		//   foreach( ISceneBone sceneBone in meshSceneObject.SkinBones )
		//   {
		//      EVec3 pos;
		//      EQuat rot;
		//      EVec3 scl;
		//      sceneBone.GetInitialTransform( out pos, out rot, out scl );

		//      Bone parentBone = null;
		//      if( sceneBone.Parent != null )
		//      {
		//         parentBone = skeleton.GetBone( bonesNames[ sceneBone.Parent ] );

		//         if( parentBone == null )
		//         {
		//            Log.Error(
		//               "ERROR: Internal error: DoExportSkeleton: parentBone == null." );
		//            skeleton.Dispose();
		//            return false;
		//         }
		//      }

		//      Bone bone = skeleton.CreateBone( parentBone, bonesNames[ sceneBone ] );
		//      bone.Position = ToVec3( pos ) * scale;
		//      bone.Rotation = ToQuat( rot );
		//      bone.Scale = ToVec3( scl );
		//   }

		//   skeleton.SetBindingPose();

		//   //create animations
		//   {
		//      float fps = 1.0f / (float)meshSceneObject.AnimationFrameRate;

		//      foreach( AnimationItem animationItem in animations )
		//      {
		//         if( !animationItem.IsSkeletonAnimation() )
		//            continue;

		//         float length = ( animationItem.FrameEnd - animationItem.FrameBegin ) * fps;

		//         Animation animation = skeleton.CreateAnimation( animationItem.Name, length );

		//         float frameBegin = (float)animationItem.FrameBegin;
		//         float frameEnd = (float)animationItem.FrameEnd;

		//         IList<NodeAnimationTrack> animationTrackList = new List<NodeAnimationTrack>();
		//         foreach( Bone bone in skeleton.Bones )
		//         {
		//            NodeAnimationTrack animationTrack = animation.CreateNodeTrack( bone.Handle, bone );
		//            animationTrackList.Add( animationTrack );
		//         }

		//         for( float frame2 = frameBegin; frame2 < frameEnd + frameStep * .99999f;
		//            frame2 += frameStep )
		//         {
		//            float frame = frame2;
		//            if( frame > frameEnd )
		//               frame = frameEnd;

		//            float timePosition = (float)( frame - frameBegin ) * fps;

		//            for( int i = 0; i < skeleton.Bones.Count; i++ )
		//            {
		//               ISceneBone sceneBone = null;
		//               foreach( ISceneBone b in meshSceneObject.SkinBones )
		//               {
		//                  if( bonesNames[ b ] == skeleton.Bones[ i ].Name )
		//                  {
		//                     sceneBone = b;
		//                     break;
		//                  }
		//               }

		//               EVec3 pos;
		//               EQuat rot;
		//               EVec3 scl;
		//               sceneBone.GetTransform( frame, out pos, out rot, out scl );

		//               TransformKeyFrame keyFrame = animationTrackList[ i ].
		//                  CreateNodeKeyFrame( timePosition );
		//               keyFrame.Position = ToVec3( pos ) * scale;
		//               keyFrame.Rotation = ToQuat( rot );
		//               keyFrame.Scale = ToVec3( scl );

		//            }
		//         }

		//         foreach( NodeAnimationTrack animationTrack in animationTrackList )
		//            animationTrack.Optimize();
		//      }
		//   }

		//   //!!!!!
		//   //string skeletonFullPath = Path.Combine( GlobalSettings.PathToDataDirectory,
		//   //   skeletonName );
		//   //if( !skeleton.Save( skeletonFullPath ) )
		//   //{
		//   //   Log.Error( "ERROR: Skeleton save failed \"{0}\".", skeletonFullPath );
		//   //   skeleton.Dispose();
		//   //   return false;
		//   //}

		//   //skeleton.Dispose();

		//   return true;
		//}

		//bool IsEqual( Vec3 vec1, Vec3 vec2, float epsilon )
		//{
		//   if( Math.Abs( vec1.x - vec2.x ) > epsilon )
		//      return false;
		//   if( Math.Abs( vec1.y - vec2.y ) > epsilon )
		//      return false;
		//   if( Math.Abs( vec1.z - vec2.z ) > epsilon )
		//      return false;

		//   return true;
		//}

		bool IsMorphFramesLinear( float keyFrameBeginTime, Vec3[] keyFrameBeginVertices,
			float frameTime, Vec3[] frameVertices, float keyFrameEndTime, Vec3[] keyFrameEndVertices )
		{
			for( int vertexIndex = 0; vertexIndex < frameVertices.Length; vertexIndex++ )
			{
				Vec3 vertex = frameVertices[ vertexIndex ];

				Vec3 beginVertex = keyFrameBeginVertices[ vertexIndex ];
				Vec3 endVertex = keyFrameEndVertices[ vertexIndex ];

				float timeScale = ( frameTime - keyFrameBeginTime ) /
					( keyFrameEndTime - keyFrameBeginTime );

				Vec3 resultVertex = beginVertex + ( endVertex - beginVertex ) * timeScale;

				if( !resultVertex.Equals( vertex, morphInterpolationError ) )
					return false;
			}

			return true;
		}

		Vec3[] GetSubMeshGroupAllVerticesByTime( List<ISceneSubMesh> subMeshGroup, float frame,
				bool isSkeletonAndMorphAnimation )
		{
			List<Vec3> allVerticesList = new List<Vec3>();

			foreach( ISceneSubMesh subMesh in subMeshGroup )
			{
				Vec3[] vertices;
				subMesh.GetVerticesByTime( frame, out vertices, !isSkeletonAndMorphAnimation );

				for( int vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++ )
					allVerticesList.Add( vertices[ vertexIndex ] );
			}

			Vec3[] allSubMeshVertices = allVerticesList.ToArray();

			return allSubMeshVertices;
		}

		List<float> GetKeyFramesForMorphAnimation( List<ISceneSubMesh> subMeshGroup, float frameBegin,
				 float frameEnd, bool isSkeletonAndMorphAnimation )
		{
			List<float> resultKeyFrames = new List<float>();

			if( morphInterpolationError > 0.0f )
			{

				resultKeyFrames.Add( frameBegin );

				Vec3[] keyFrameBeginVertices = GetSubMeshGroupAllVerticesByTime( subMeshGroup, frameBegin,
					 isSkeletonAndMorphAnimation );
				Vec3[] keyFrameEndVertices = null;

				for( float frame2 = frameBegin + frameStep; frame2 < frameEnd + frameStep * .99999f;
					frame2 += frameStep )
				{
					float frame = frame2;
					if( frame > frameEnd )
						frame = frameEnd;

					Vec3[] frameVertices = keyFrameEndVertices;
					keyFrameEndVertices = GetSubMeshGroupAllVerticesByTime( subMeshGroup, frame,
						 isSkeletonAndMorphAnimation );

					float lastKeyFrameTime = resultKeyFrames[ resultKeyFrames.Count - 1 ];
					if( frame - lastKeyFrameTime > frameStep )
					{
						float frameCandidate = frame - frameStep;

						if( morphInterpolationError == 0.0f || !IsMorphFramesLinear( lastKeyFrameTime,
							keyFrameBeginVertices, frameCandidate, frameVertices, frame, keyFrameEndVertices ) )
						{
							resultKeyFrames.Add( frameCandidate );
							keyFrameBeginVertices = frameVertices;
						}
					}
				}

				resultKeyFrames.Add( frameEnd );
			}
			else
			{
				for( float frame2 = frameBegin; frame2 < frameEnd + frameStep * .99999f;
					 frame2 += frameStep )
				{
					float frame = frame2;
					if( frame > frameEnd )
						frame = frameEnd;

					resultKeyFrames.Add( frame );
				}
			}

			return resultKeyFrames;
		}

		bool DoExportMorph( IMeshSceneObject meshSceneObject, Mesh mesh,
			List<List<ISceneSubMesh>> sceneSubMeshes )
		{
			//create animations

			float fps = 1.0f / (float)meshSceneObject.AnimationFrameRate;

			foreach( AnimationItem animationItem in animations )
			{
				if( !animationItem.IsMorphAnimation() )
					continue;

				float length = ( animationItem.FrameEnd - animationItem.FrameBegin ) * fps;

				Animation animation = mesh.CreateAnimation( animationItem.Name, length );

				float frameBegin = (float)animationItem.FrameBegin;
				float frameEnd = (float)animationItem.FrameEnd;

				bool isSkeletonAndMorphAnimation = animationItem.Format == AnimationTypes.SkeletonAndMorph;

				List<VertexAnimationTrack> animationTrackList =
					 new List<VertexAnimationTrack>( mesh.SubMeshes.Length );
				List<List<float>> keyFramesList = new List<List<float>>( mesh.SubMeshes.Length );
				for( int subMeshIndex = 0; subMeshIndex < mesh.SubMeshes.Length; subMeshIndex++ )
				{
					VertexAnimationTrack animationTrack = animation.CreateVertexTrack( subMeshIndex + 1,
						  mesh.SubMeshes[ subMeshIndex ].VertexData, VertexAnimationType.Morph );
					animationTrackList.Add( animationTrack );

					List<float> keyFrames = GetKeyFramesForMorphAnimation(
						sceneSubMeshes[ subMeshIndex ], frameBegin, frameEnd,
								isSkeletonAndMorphAnimation );
					keyFramesList.Add( keyFrames );
				}

				for( int subMeshIndex = 0; subMeshIndex < mesh.SubMeshes.Length; subMeshIndex++ )
				{
					foreach( float frame in keyFramesList[ subMeshIndex ] )
					{
						Vec3[] allSubMeshVertices = GetSubMeshGroupAllVerticesByTime(
							 sceneSubMeshes[ subMeshIndex ], frame, isSkeletonAndMorphAnimation );

						HardwareVertexBuffer buffer = HardwareBufferManager.Instance.CreateVertexBuffer(
							VertexElement.GetTypeSize( VertexElementType.Float3 ),
							allSubMeshVertices.Length, mesh.VertexBufferUsage, true );

						unsafe
						{
							IntPtr bufferPointer = buffer.Lock( HardwareBuffer.LockOptions.Discard );
							fixed( Vec3* pVertices = allSubMeshVertices )
							{
								NativeUtils.CopyMemory( bufferPointer, (IntPtr)pVertices,
									allSubMeshVertices.Length * sizeof( Vec3 ) );
							}
							buffer.Unlock();
						}

						float timePosition = (float)( frame - frameBegin ) * fps;

						VertexMorphKeyFrame keyFrame = animationTrackList[ subMeshIndex ].
							CreateVertexMorphKeyFrame( timePosition );
						keyFrame.SetVertexBuffer( buffer );

						buffer.Dispose();
					}
				}

				foreach( VertexAnimationTrack animationTrack in animationTrackList )
					animationTrack.Optimize();
			}

			return true;
		}

		bool DoExportPose( IMeshSceneObject meshSceneObject, Mesh mesh,
			List<List<ISceneSubMesh>> sceneSubMeshes )
		{
			List<int> trackTargetSubMeshIndices = new List<int>();
			List<int> startPoseIndexList = new List<int>();
			int startPoseIndex = 0;
			for( ushort i = 0; i < mesh.SubMeshes.Length; i++ )
			{
				ISceneSubMesh sceneSubMesh = sceneSubMeshes[ i ][ 0 ];//subMeshes with poses aren't merging

				PoseInfo[] posesInfo;
				sceneSubMesh.GetPoses( out posesInfo );
				if( posesInfo != null )
				{
					foreach( PoseInfo poseInfo in posesInfo )
					{
						Pose pose = mesh.CreatePose( (ushort)( i + 1 ),
							poseInfo.name + "_" + i.ToString() );
						for( ushort j = 0; j < poseInfo.offsets.Length; j++ )
						{
							Vec3 tempVec = poseInfo.offsets[ j ].offset;
							pose.AddVertex( poseInfo.offsets[ j ].index, tempVec );
						}
					}
					trackTargetSubMeshIndices.Add( i );
					startPoseIndexList.Add( startPoseIndex );
					startPoseIndex += posesInfo.Length;
				}
			}

			//create animations

			float fps = 1.0f / (float)meshSceneObject.AnimationFrameRate;

			foreach( AnimationItem animationItem in animations )
			{
				if( !animationItem.IsPoseAnimation() )
					continue;
				float length = ( animationItem.FrameEnd - animationItem.FrameBegin ) * fps;

				Animation animation = mesh.CreateAnimation( animationItem.Name, length );

				float frameBegin = (float)animationItem.FrameBegin;
				float frameEnd = (float)animationItem.FrameEnd;

				List<VertexAnimationTrack> animationTrackList =
					 new List<VertexAnimationTrack>();
				foreach( int trackTargetSubMeshIndex in trackTargetSubMeshIndices )
				{
					VertexAnimationTrack animationTrack = animation.CreateVertexTrack( trackTargetSubMeshIndex + 1,
						 VertexAnimationType.Pose );
					animationTrackList.Add( animationTrack );
				}

				for( float frame2 = frameBegin; frame2 < frameEnd + frameStep * .99999f;
					 frame2 += frameStep )
				{
					float frame = frame2;
					if( frame > frameEnd )
						frame = frameEnd;

					float timePosition = (float)( frame - frameBegin ) * fps;

					for( int i = 0; i < animationTrackList.Count; i++ )
					{
						PoseReference[] poseReferences;
						ISceneSubMesh sceneSubMesh = sceneSubMeshes[ trackTargetSubMeshIndices[ i ] ][ 0 ];
						sceneSubMesh.GetPoseReferenceByTime( frame, out poseReferences );

						if( poseReferences != null )
						{
							VertexPoseKeyFrame keyFrame = animationTrackList[ i ].
								CreateVertexPoseKeyFrame( timePosition );

							foreach( PoseReference poseReference in poseReferences )
							{
								keyFrame.AddPoseReference( poseReference.index + startPoseIndexList[ i ],
									 poseReference.weight );
							}
						}
					}
				}

				foreach( VertexAnimationTrack animationTrack in animationTrackList )
					animationTrack.Optimize();
			}

			return true;
		}

		bool IsMorphAnimationExportEnabled()
		{
			foreach( AnimationItem animationItem in animations )
				if( animationItem.IsMorphAnimation() )
					return true;

			return false;
		}

		bool IsPoseAnimationExportEnabled()
		{
			foreach( AnimationItem animationItem in animations )
				if( animationItem.IsPoseAnimation() )
					return true;

			return false;
		}

		bool IsVertexAnimationExportEnabled()
		{
			foreach( AnimationItem animationItem in animations )
				if( animationItem.IsVertexAnimation() )
					return true;

			return false;
		}

		bool IsSkeletonAnimationExportEnabled()
		{
			foreach( AnimationItem animationItem in animations )
				if( animationItem.IsSkeletonAnimation() )
					return true;

			return false;
		}

		bool IsEqualSceneSubMeshesGeometryType( ISceneSubMesh sceneSubMesh1,
			ISceneSubMesh sceneSubMesh2 )
		{
			if( sceneSubMesh1.GetTextureCoordCount() != sceneSubMesh2.GetTextureCoordCount() )
				return false;
			if( sceneSubMesh1.HasPoses() || sceneSubMesh2.HasPoses() )
				return false;

			return true;
		}

		bool IsEqualSceneSubMeshesOptions( ISceneSubMesh sceneSubMesh1, ISceneSubMesh sceneSubMesh2 )
		{
			if( sceneSubMesh1.AllowCollision != sceneSubMesh2.AllowCollision )
				return false;
			if( sceneSubMesh1.UVUnwrapChannel != sceneSubMesh2.UVUnwrapChannel )
				return false;

			return true;
		}

		bool IsEqualSubMeshesMaterials( ISceneSubMesh sceneSubMesh1, ISceneSubMesh sceneSubMesh2 )
		{
			if( sceneSubMesh1.Material == null && sceneSubMesh2.Material == null )
				return true;

			if( sceneSubMesh1.Material == null || sceneSubMesh2.Material == null )
				return false;

			return ( sceneSubMesh1.Material.Name == sceneSubMesh2.Material.Name );
		}

		bool IsSubMeshVetricesEqual( SubMeshVertex subMeshVertex1, SubMeshVertex subMeshVertex2,
			 float epsilon )
		{
			if( !subMeshVertex1.position.Equals( subMeshVertex2.position, epsilon ) )
				return false;
			if( !subMeshVertex1.normal.Equals( subMeshVertex2.normal, epsilon ) )
				return false;
			if( !subMeshVertex1.color.Equals( subMeshVertex2.color, epsilon ) )
				return false;
			if( !subMeshVertex1.texCoord0.Equals( subMeshVertex2.texCoord0, epsilon ) )
				return false;
			if( !subMeshVertex1.texCoord1.Equals( subMeshVertex2.texCoord1, epsilon ) )
				return false;
			if( !subMeshVertex1.texCoord2.Equals( subMeshVertex2.texCoord2, epsilon ) )
				return false;
			if( !subMeshVertex1.texCoord3.Equals( subMeshVertex2.texCoord3, epsilon ) )
				return false;

			return true;
		}

		bool IsSubMeshSceneBoneAssignmentsEqual( SceneBoneAssignmentItem[] sceneBoneAssignments1,
			 SceneBoneAssignmentItem[] sceneBoneAssignments2, float epsilon )
		{
			if( sceneBoneAssignments1.Length != sceneBoneAssignments2.Length )
				return false;

			for( int i = 0; i < sceneBoneAssignments1.Length; i++ )
			{
				SceneBoneAssignmentItem item1 = sceneBoneAssignments1[ i ];
				SceneBoneAssignmentItem item2 = sceneBoneAssignments2[ i ];

				if( item1.BoneIndex != item2.BoneIndex )
					return false;

				if( Math.Abs( item1.Weight - item2.Weight ) > epsilon )
					return false;
			}

			return true;
		}

		int GetResultIndex( List<SubMeshVertex> resultVerticesList, SubMeshVertex subMeshVertex,
			 List<SceneBoneAssignmentItem[]> resultSceneBoneAssignmentsList,
			 SceneBoneAssignmentItem[] vertexSceneBoneAssignments )
		{
			for( int vertexIndex = 0; vertexIndex < resultVerticesList.Count; vertexIndex++ )
			{
				if( IsSubMeshVetricesEqual( resultVerticesList[ vertexIndex ], subMeshVertex, .001f ) )
				{
					if( IsSubMeshSceneBoneAssignmentsEqual( resultSceneBoneAssignmentsList[ vertexIndex ],
						 vertexSceneBoneAssignments, .001f ) )
					{
						return vertexIndex;
					}
				}
			}

			return -1;
		}

		int GetResultIndex( List<SubMeshVertex> resultVerticesList, SubMeshVertex subMeshVertex )
		{
			for( int vertexIndex = 0; vertexIndex < resultVerticesList.Count; vertexIndex++ )
			{
				if( IsSubMeshVetricesEqual( resultVerticesList[ vertexIndex ], subMeshVertex, .001f ) )
					return vertexIndex;
			}

			return -1;
		}

		void MergeEqualVertices( bool allowEqualVerticesMerging, SubMeshVertex[][] verticesList,
			int[][] indicesList, SceneBoneAssignmentItem[][][] sceneBoneAssignmentsList,
			out SubMeshVertex[] mergedVertices, out int[] mergedIndices,
			out SceneBoneAssignmentItem[][] mergedSceneBoneAssignments )
		{
			List<SubMeshVertex> resultVerticesList = new List<SubMeshVertex>();
			List<int> resultIndicesList = new List<int>();
			List<SceneBoneAssignmentItem[]> resultSceneBoneAssignmentsList =
				new List<SceneBoneAssignmentItem[]>();

			int sceneSubMeshesCount = verticesList.Length;

			bool hasSkeleton = sceneBoneAssignmentsList != null;

			for( int sceneSubMeshIndex = 0; sceneSubMeshIndex < sceneSubMeshesCount;
				sceneSubMeshIndex++ )
			{
				SubMeshVertex[] vertices = verticesList[ sceneSubMeshIndex ];
				int[] indices = indicesList[ sceneSubMeshIndex ];

				int[] oldVertexIndexToMergedVertexIndexMap = new int[ vertices.Length ];

				for( int oldVertexIndex = 0; oldVertexIndex < vertices.Length; oldVertexIndex++ )
				{
					SubMeshVertex subMeshVertex = vertices[ oldVertexIndex ];
					SceneBoneAssignmentItem[] vertexSceneBoneAssignments = null;
					if( hasSkeleton )
					{
						vertexSceneBoneAssignments =
							sceneBoneAssignmentsList[ sceneSubMeshIndex ][ oldVertexIndex ];
					}

					int resultIndex = -1;

					if( allowEqualVerticesMerging )
					{
						if( hasSkeleton )
						{
							resultIndex = GetResultIndex( resultVerticesList, subMeshVertex,
								 resultSceneBoneAssignmentsList, vertexSceneBoneAssignments );
						}
						else
						{
							resultIndex = GetResultIndex( resultVerticesList, subMeshVertex );
						}
					}

					if( resultIndex == -1 )
					{
						resultIndex = resultVerticesList.Count;
						resultVerticesList.Add( subMeshVertex );
						if( hasSkeleton )
							resultSceneBoneAssignmentsList.Add( vertexSceneBoneAssignments );
					}

					oldVertexIndexToMergedVertexIndexMap[ oldVertexIndex ] = resultIndex;
				}

				for( int oldIndex = 0; oldIndex < indices.Length; oldIndex++ )
				{
					int mergedIndex = oldVertexIndexToMergedVertexIndexMap[ indices[ oldIndex ] ];
					resultIndicesList.Add( mergedIndex );
				}
			}

			mergedVertices = resultVerticesList.ToArray();
			mergedIndices = resultIndicesList.ToArray();
			mergedSceneBoneAssignments = resultSceneBoneAssignmentsList.ToArray();
		}

		List<List<ISceneSubMesh>> GetSubMeshMergingGroups( IMeshSceneObject meshSceneObject )
		{
			//lists of subMeshes with equal geometry types and options generation
			List<List<ISceneSubMesh>> sceneSubMeshes = new List<List<ISceneSubMesh>>();

			if( allowMergeSubMeshes )
			{
				List<ISceneSubMesh> notUsedSceneSubMeshesList = new List<ISceneSubMesh>();
				for( int i = 1; i < meshSceneObject.SubMeshes.Length; i++ )
					notUsedSceneSubMeshesList.Add( meshSceneObject.SubMeshes[ i ] );

				if( meshSceneObject.SubMeshes.Length > 0 )
				{
					List<ISceneSubMesh> sceneSubMehesGroup = new List<ISceneSubMesh>();
					sceneSubMehesGroup.Add( meshSceneObject.SubMeshes[ 0 ] );
					sceneSubMeshes.Add( sceneSubMehesGroup );

					while( notUsedSceneSubMeshesList.Count > 0 )
					{
						ISceneSubMesh sceneSubMesh = notUsedSceneSubMeshesList[ 0 ];
						notUsedSceneSubMeshesList.RemoveAt( 0 );

						int subMeshIndexInSceneSubMehes = 0;
						bool foundListWithEqualGeometryType = false;
						while( subMeshIndexInSceneSubMehes < sceneSubMeshes.Count &&
							 !foundListWithEqualGeometryType )
						{
							ISceneSubMesh usedSceneSubMesh = sceneSubMeshes[
								subMeshIndexInSceneSubMehes ][ 0 ];

							if( IsEqualSubMeshesMaterials( usedSceneSubMesh, sceneSubMesh ) &&
								IsEqualSceneSubMeshesGeometryType( usedSceneSubMesh, sceneSubMesh ) &&
								IsEqualSceneSubMeshesOptions( usedSceneSubMesh, sceneSubMesh ) )
							{
								foundListWithEqualGeometryType = true;
								sceneSubMeshes[ subMeshIndexInSceneSubMehes ].Add( sceneSubMesh );
							}
							else
								subMeshIndexInSceneSubMehes++;
						}

						if( !foundListWithEqualGeometryType )
						{
							List<ISceneSubMesh> newSceneSubMehesGroup = new List<ISceneSubMesh>();
							newSceneSubMehesGroup.Add( sceneSubMesh );
							sceneSubMeshes.Add( newSceneSubMehesGroup );
						}
					}
				}
			}
			else
			{
				foreach( ISceneSubMesh sceneSubMesh in meshSceneObject.SubMeshes )
				{
					List<ISceneSubMesh> sceneSubMeshGroup = new List<ISceneSubMesh>();
					sceneSubMeshGroup.Add( sceneSubMesh );
					sceneSubMeshes.Add( sceneSubMeshGroup );
				}
			}

			return sceneSubMeshes;
		}

		SceneBoneAssignmentItem[][][] GetMeshSceneBoneAssignments(
			List<ISceneSubMesh> subMeshMergingList, SubMeshVertex[][] verticesList )
		{
			SceneBoneAssignmentItem[][][] sceneBoneAssignmentsList =
				 new SceneBoneAssignmentItem[ subMeshMergingList.Count ][][];

			for( int subMeshIndexInGroup = 0; subMeshIndexInGroup < subMeshMergingList.Count;
				 subMeshIndexInGroup++ )
			{
				int vertexCount = verticesList[ subMeshIndexInGroup ].Length;

				sceneBoneAssignmentsList[ subMeshIndexInGroup ] =
					new SceneBoneAssignmentItem[ vertexCount ][];

				for( int vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++ )
				{
					sceneBoneAssignmentsList[ subMeshIndexInGroup ][ vertexIndex ] =
						subMeshMergingList[ subMeshIndexInGroup ].GetVertexBoneAssignment( vertexIndex );
				}
			}

			return sceneBoneAssignmentsList;
		}

		void GetBoundsFromAnimations( IMeshSceneObject meshSceneObject, AnimationItem animationItem,
			 ref Bounds bounds, ref float radius )
		{
			float frameBegin = animationItem.FrameBegin;
			float frameEnd = animationItem.FrameEnd;

			for( float frame2 = frameBegin; frame2 < frameEnd + frameStep * .99999f;
				 frame2 += frameStep )
			{
				float frame = frame2;
				if( frame > frameEnd )
					frame = frameEnd;

				Bounds b;
				float r;
				meshSceneObject.GetBoundsByTime( frame, out b, out r );

				bounds.Add( b.Minimum );
				bounds.Add( b.Maximum );
				if( r > radius )
					radius = r;
			}
		}

		bool DoExportMesh( IMeshSceneObject meshSceneObject, string skeletonName, Mesh mesh )
		{
			//string fullPath = Path.Combine( Path.Combine( GlobalSettings.PathToDataDirectory,
			//   Manager.MeshesDirectory ), Name + ".mesh" );

			Bounds bounds = Bounds.Cleared;
			float radius = -1;

			//string meshName = MeshManager.Instance.GetUniqueName( "_TempMeshName" );
			//Mesh mesh = MeshManager.Instance.CreateManual( meshName );

			if( skeletonName != null )
				mesh.SkeletonName = skeletonName;

			//Dictionary<ISceneSubMesh, ExportSubMesh> exportSubMeshes =
			//    new Dictionary<ISceneSubMesh, ExportSubMesh>();
			//for( int i = 0; i < meshSceneObject.SubMeshes.Length; i++ )
			//{
			//   ISceneSubMesh subMesh = meshSceneObject.SubMeshes[ i ];
			//   exportSubMeshes.Add( subMesh, subMeshes[ i ] );
			//}

			List<List<ISceneSubMesh>> sceneSubMeshes = GetSubMeshMergingGroups( meshSceneObject );//, exportSubMeshes );

			int subMeshGroupIndex = 0;
			while( subMeshGroupIndex < sceneSubMeshes.Count )
			{
				List<ISceneSubMesh> subMeshMergingList = sceneSubMeshes[ subMeshGroupIndex ];

				int texCoordCount = subMeshMergingList[ 0 ].GetTextureCoordCount();
				SubMeshVertex[][] verticesList = new SubMeshVertex[ subMeshMergingList.Count ][];
				int[][] indicesList = new int[ subMeshMergingList.Count ][];
				for( int i = 0; i < subMeshMergingList.Count; i++ )
					subMeshMergingList[ i ].GetGeometry( out verticesList[ i ], out indicesList[ i ] );

				//no triangles
				int indicesCount = 0;
				foreach( int[] indices in indicesList )
					indicesCount += indices.Length;
				if( indicesCount == 0 )
				{
					sceneSubMeshes.RemoveAt( subMeshGroupIndex );
					continue;
				}

				SceneBoneAssignmentItem[][][] sceneBoneAssignmentsList = null;
				if( skeletonName != null )
				{
					sceneBoneAssignmentsList = GetMeshSceneBoneAssignments( subMeshMergingList,
						verticesList );
				}

				SubMeshVertex[] mergedVertices;
				int[] mergedIndices;
				SceneBoneAssignmentItem[][] mergedSceneBoneAssignments;

				bool allowEqualVerticesMerging = !IsVertexAnimationExportEnabled() ||
					 ( IsPoseAnimationExportEnabled() && !subMeshMergingList[ 0 ].HasPoses() );

				MergeEqualVertices( allowEqualVerticesMerging, verticesList, indicesList,
					 sceneBoneAssignmentsList, out mergedVertices, out mergedIndices,
					 out mergedSceneBoneAssignments );

				//create subMesh
				SubMesh subMesh = mesh.CreateSubMesh();
				subMesh.UseSharedVertices = false;

				subMesh.AllowCollision = subMeshMergingList[ 0 ].AllowCollision;

				ISceneMaterial material = subMeshMergingList[ 0 ].Material;
				if( material != null )
					subMesh.MaterialName = material.Name;

				if( subMeshMergingList[ 0 ].UVUnwrapChannel != UVUnwrapChannels.None )
					subMesh.UnwrappedTexCoordIndex = (int)subMeshMergingList[ 0 ].UVUnwrapChannel - 1;

				//create vertex data
				{
					int positionOffset = 0;
					int normalOffset = 0;
					int colorOffset = 0;
					int[] texCoordOffsets = new int[ texCoordCount ];

					VertexDeclaration declaration = HardwareBufferManager.Instance.
						CreateVertexDeclaration();

					positionOffset = declaration.GetVertexSizeInBytes( 0 );
					declaration.AddElement( 0, declaration.GetVertexSizeInBytes( 0 ),
						VertexElementType.Float3, VertexElementSemantic.Position );

					normalOffset = declaration.GetVertexSizeInBytes( 0 );
					declaration.AddElement( 0, declaration.GetVertexSizeInBytes( 0 ),
						VertexElementType.Float3, VertexElementSemantic.Normal );

					if( subMeshMergingList[ 0 ].HasVertexColors() )
					{
						colorOffset = declaration.GetVertexSizeInBytes( 0 );
						declaration.AddElement( 0, declaration.GetVertexSizeInBytes( 0 ),
							VertexElementType.ColorARGB, VertexElementSemantic.Diffuse );
					}

					for( int texCoordIndex = 0; texCoordIndex < texCoordCount; texCoordIndex++ )
					{
						texCoordOffsets[ texCoordIndex ] = declaration.GetVertexSizeInBytes( 0 );
						declaration.AddElement( 0, declaration.GetVertexSizeInBytes( 0 ),
							VertexElementType.Float2, VertexElementSemantic.TextureCoordinates,
							texCoordIndex );
					}

					int vertexSize = declaration.GetVertexSizeInBytes( 0 );

					IntPtr pVertices = NativeUtils.Alloc( mergedVertices.Length * vertexSize );

					unsafe
					{
						byte* pVertex = (byte*)pVertices;
						for( int vertexIndex = 0; vertexIndex < mergedVertices.Length; vertexIndex++ )
						{
							SubMeshVertex vertex = mergedVertices[ vertexIndex ];

							*(Vec3*)( pVertex + positionOffset ) = vertex.position;
							*(Vec3*)( pVertex + normalOffset ) = vertex.normal;
							if( subMeshMergingList[ 0 ].HasVertexColors() )
							{
								byte* p = pVertex + colorOffset;

								p[ 3 ] = (byte)( vertex.color.Alpha * 255.0f );
								p[ 2 ] = (byte)( vertex.color.Red * 255.0f );
								p[ 1 ] = (byte)( vertex.color.Green * 255.0f );
								p[ 0 ] = (byte)( vertex.color.Blue * 255.0f );
							}

							for( int texCoordIndex = 0; texCoordIndex < texCoordCount; texCoordIndex++ )
							{
								Vec2 texCoord = new Vec2();
								switch( texCoordIndex )
								{
								case 0: texCoord = vertex.texCoord0; break;
								case 1: texCoord = vertex.texCoord1; break;
								case 2: texCoord = vertex.texCoord2; break;
								case 3: texCoord = vertex.texCoord3; break;
								}
								*(Vec2*)( pVertex + texCoordOffsets[ texCoordIndex ] ) = texCoord;
							}

							pVertex += vertexSize;
						}
					}

					subMesh.VertexData = VertexData.CreateFromArray( declaration, pVertices,
						 mergedVertices.Length * vertexSize );

					NativeUtils.Free( pVertices );

					HardwareBufferManager.Instance.DestroyVertexDeclaration( declaration );
				}

				//create index data
				subMesh.IndexData = IndexData.CreateFromArray( mergedIndices, 0, indicesCount, true );

				//bounds and radius
				for( int vertexIndex = 0; vertexIndex < mergedVertices.Length; vertexIndex++ )
				{
					Vec3 p = mergedVertices[ vertexIndex ].position;
					bounds.Add( p );
					float r = p.Length();
					if( r > radius )
						radius = r;
				}

				//bone assignments
				if( skeletonName != null )
				{
					for( int vertexIndex = 0; vertexIndex < mergedVertices.Length; vertexIndex++ )
					{
						SceneBoneAssignmentItem[] items = mergedSceneBoneAssignments[ vertexIndex ];

						const int maxBlendWeights = 4;

						if( items.Length > maxBlendWeights )
						{
							ArrayUtils.SelectionSort<SceneBoneAssignmentItem>( items, delegate(
								SceneBoneAssignmentItem item1, SceneBoneAssignmentItem item2 )
							{
								if( item1.Weight < item2.Weight )
									return 1;
								if( item1.Weight > item2.Weight )
									return -1;
								return 0;
							} );

							SceneBoneAssignmentItem[] newItems = new SceneBoneAssignmentItem[
								maxBlendWeights ];
							for( int n = 0; n < maxBlendWeights; n++ )
								newItems[ n ] = items[ n ];
							items = newItems;
						}

						//if no bones, assign to root bone
						if( items.Length == 0 )
						{
							items = new SceneBoneAssignmentItem[ 1 ];
							SceneBoneAssignmentItem item = new SceneBoneAssignmentItem();
							item.BoneIndex = 0;
							item.Weight = 1;
							items[ 0 ] = item;
						}

						float totalWeight = 0;
						{
							foreach( SceneBoneAssignmentItem i in items )
								totalWeight += i.Weight;
						}

						foreach( SceneBoneAssignmentItem item in items )
						{
							float normalizedWeight = item.Weight / totalWeight;
							subMesh.AddBoneAssignment( vertexIndex, item.BoneIndex, normalizedWeight );
						}
					}
				}

				subMeshGroupIndex++;
			}

			//bounds and radius

			{
				foreach( AnimationItem animationItem in animations )
					GetBoundsFromAnimations( meshSceneObject, animationItem, ref bounds, ref radius );
			}

			//reorganize vertex buffers
			foreach( SubMesh subMesh in mesh.SubMeshes )
			{
				subMesh.VertexData._ReorganizeBuffers( skeletonName != null,
					IsVertexAnimationExportEnabled() );
			}

			//morph
			if( IsMorphAnimationExportEnabled() )
			{
				if( !DoExportMorph( meshSceneObject, mesh, sceneSubMeshes ) )
					return false;
			}

			//pose
			if( IsPoseAnimationExportEnabled() )
			{
				if( !DoExportPose( meshSceneObject, mesh, sceneSubMeshes ) )
					return false;
			}

			//if( lodsCount > 0 )
			//{
			//   float[] lodDistances = new float[ lodsCount ];
			//   for( int n = 0; n < lodsCount; n++ )
			//      lodDistances[ n ] = lodsDistance * ( n + 1 );
			//   mesh.GenerateLodLevels( lodDistances,
			//      ProgressiveMeshVertexReductionQuota.Proportional, lodsReduction );
			//}

			if( tangents )
			{
				string error;
				if( IsPossibleGenerateTangents( mesh, out error ) )
					mesh.BuildTangentVectors( VertexElementSemantic.Tangent, 0, 0, true );
			}

			mesh.EdgeList = edgeList;

			//calculate bounds and radius
			if( bounds.IsCleared() )
				bounds = new Bounds( new Vec3( -.1f, -.1f, -.1f ), new Vec3( .1f, .1f, .1f ) );
			if( radius < 0 )
				radius = .1f;
			mesh.SetBoundsAndRadius( bounds, radius );

			return true;
		}

		bool IsAnimationsNamesUnique()
		{
			List<string> animationsNames = new List<string>();

			foreach( AnimationItem animationItem in animations )
			{
				string animationName = animationItem.Name.ToUpper();
				if( animationsNames.Contains( animationName ) )
					return false;

				animationsNames.Add( animationName );
			}

			return true;
		}

		public bool DoExport( IMeshSceneObject meshSceneObject, Mesh mesh )
		{
			//!!!!!

			//if( !IsAnimationsNamesUnique() )
			//{
			//   Log.Error( "ERROR: Animations names must be unique." );
			//   return false;
			//}

			string skeletonName = null;
			//if( exportSkeleton )
			//{
			//   skeletonName = Path.Combine( Manager.MeshesDirectory, Name ) + ".skeleton";

			//   if( !DoExportSkeleton( meshSceneObject, skeletonName ) )
			//      return false;
			//}
			//else
			//{
			//   if( IsSkeletonAnimationExportEnabled() )
			//   {
			//      Log.Error( "ERROR: For skeleton animation export \"ExportSkeleton\" must be true." );
			//      return false;
			//   }
			//}

			//mesh
			if( !DoExportMesh( meshSceneObject, skeletonName, mesh ) )
				return false;

			return true;
		}

	}
}
