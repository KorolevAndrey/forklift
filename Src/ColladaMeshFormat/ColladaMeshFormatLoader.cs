// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using Engine;
using Engine.FileSystem;
using Engine.Renderer;
using Engine.MathEx;
using Engine.Utils;
using System.Xml;
using GeneralMeshUtils;

namespace ColladaMeshFormat
{
	[CustomMeshFormatLoaderExtensions( new string[] { "dae" } )]
	public class ColladaMeshFormatLoader : CustomMeshFormatLoader
	{
		string currentFileName;
		Mesh currentMesh;

		float globalScale = 1;
		bool yAxisUp = true;
		Dictionary<string, MySceneMaterial> generatedMaterials;
		Dictionary<string, GeometryItem> generatedGeometries;
		List<MySceneSubMesh> generatedSubMeshes;

		/////////////////////////////////////////////////////////////////////////////////////////////

		class MySceneMaterial : ISceneMaterial
		{
			string name;

			public MySceneMaterial( string name )
			{
				this.name = name;
			}

			public string Name
			{
				get { return name; }
			}
		}

		/////////////////////////////////////////////////////////////////////////////////////////////

		class GeometryItem
		{
			string id;
			SubMesh[] subMeshes;

			//

			public class SubMesh
			{
				SubMeshVertex[] vertices;
				int[] indices;
				int textureCoordCount;
				bool vertexColors;
				MySceneMaterial material;

				//

				public SubMesh( SubMeshVertex[] vertices, int[] indices, int textureCoordCount,
					bool vertexColors, MySceneMaterial material )
				{
					this.vertices = vertices;
					this.indices = indices;
					this.textureCoordCount = textureCoordCount;
					this.vertexColors = vertexColors;
					this.material = material;
				}

				public SubMeshVertex[] Vertices
				{
					get { return vertices; }
				}

				public int[] Indices
				{
					get { return indices; }
				}

				public int TextureCoordCount
				{
					get { return textureCoordCount; }
				}

				public bool VertexColors
				{
					get { return vertexColors; }
				}

				public MySceneMaterial Material
				{
					get { return material; }
				}
			}

			//

			public GeometryItem( string id, SubMesh[] subMeshes )
			{
				this.id = id;
				this.subMeshes = subMeshes;
			}

			public string Id
			{
				get { return id; }
			}

			public SubMesh[] SubMeshes
			{
				get { return subMeshes; }
			}
		}

		/////////////////////////////////////////////////////////////////////////////////////////////

		class MySceneSubMesh : ISceneSubMesh
		{
			SubMeshVertex[] vertices;
			int[] indices;
			int textureCoordCount;
			bool vertexColors;
			MySceneMaterial material;

			//

			public MySceneSubMesh( SubMeshVertex[] vertices, int[] indices, int textureCoordCount,
				bool vertexColors, MySceneMaterial material )
			{
				this.vertices = vertices;
				this.indices = indices;
				this.textureCoordCount = textureCoordCount;
				this.vertexColors = vertexColors;
				this.material = material;
			}

			public void GetGeometry( out SubMeshVertex[] vertices, out int[] indices )
			{
				vertices = this.vertices;
				indices = this.indices;
			}

			public int GetTextureCoordCount()
			{
				return textureCoordCount;
			}

			public bool VertexColors
			{
				get { return vertexColors; }
			}

			public ISceneMaterial Material
			{
				get { return material; }
			}

			public SceneBoneAssignmentItem[] GetVertexBoneAssignment( int vertexIndex )
			{
				return null;
			}

			public void GetVerticesByTime( float timeFrame, out Vec3[] vertices, bool skeletonOn )
			{
				vertices = null;
				skeletonOn = false;
			}

			public bool HasPoses()
			{
				return false;
			}

			public void GetPoses( out PoseInfo[] poses )
			{
				poses = null;
			}

			public void GetPoseReferenceByTime( float timeFrame, out PoseReference[] poseReferences )
			{
				poseReferences = null;
			}

			public bool AllowCollision
			{
				get { return true; }
			}

			public UVUnwrapChannels UVUnwrapChannel
			{
				get { return UVUnwrapChannels.None; }
			}

			public bool HasVertexColors()
			{
				return vertexColors;
			}
		}

		/////////////////////////////////////////////////////////////////////////////////////////////

		class MyMeshSceneObject : IMeshSceneObject
		{
			MySceneSubMesh[] subMeshes;

			//

			public MyMeshSceneObject( MySceneSubMesh[] subMeshes )
			{
				this.subMeshes = subMeshes;
			}


			public ISceneSubMesh[] SubMeshes
			{
				get { return subMeshes; }
			}

			public IList<ISceneBone> SkinBones
			{
				get
				{
					return new ISceneBone[ 0 ];
				}
			}

			public int AnimationFrameRate
			{
				get
				{
					return 0;
				}
			}

			public void GetBoundsByTime( float timeFrame, out Bounds bounds, out float radius )
			{
				Log.Fatal( "GetBoundsByTime" );
				bounds = Bounds.Cleared;
				radius = -1;
			}
		}

		/////////////////////////////////////////////////////////////////////////////////////////////

		class SourceItem
		{
			string id;
			float[] data;
			int offset;
			int stride;

			//

			public SourceItem( string id, float[] data, int offset, int stride )
			{
				this.id = id;
				this.data = data;
				this.offset = offset;
				this.stride = stride;
			}

			public string Id
			{
				get { return id; }
			}

			public float[] Data
			{
				get { return data; }
			}

			public int Offset
			{
				get { return offset; }
			}

			public int Stride
			{
				get { return stride; }
			}

			public Vec2 GetItemVec2( int index )
			{
				int index2 = offset + index * stride;
				return new Vec2(
					data[ index2 + 0 ],
					data[ index2 + 1 ] );
			}

			public Vec3 GetItemVec3( int index )
			{
				int index2 = offset + index * stride;
				return new Vec3(
					data[ index2 + 0 ],
					data[ index2 + 1 ],
					data[ index2 + 2 ] );
			}
		}

		/////////////////////////////////////////////////////////////////////////////////////////////

		enum ChannelTypes
		{
			Unknown,

			POSITION,
			NORMAL,
			COLOR,
			TEXCOORD0,
			TEXCOORD1,
			TEXCOORD2,
			TEXCOORD3,
			TANGENT,
		}

		/////////////////////////////////////////////////////////////////////////////////////////////

		void Error( string format, params object[] args )
		{
			Log.Warning(
				string.Format( "ColladaMeshFormatLoader: Cannot load file \"{0}\". ", currentFileName ) +
				string.Format( format, args ) );
		}

		static float[] ConvertStringToFloatArray( string str )
		{
			if( string.IsNullOrEmpty( str ) )
				return new float[ 0 ];

			string fixedStr = str.Replace( ',', '.' );

			string[] strings = fixedStr.Split( new char[] { ' ', '\n', '\t', '\r' },
				StringSplitOptions.RemoveEmptyEntries );

			float[] array = new float[ strings.Length ];

			for( int n = 0; n < strings.Length; n++ )
			{
				float value;
				if( !float.TryParse( strings[ n ], out value ) )
					return null;

				array[ n ] = value;
			}

			return array;
		}

		static int[] ConvertStringToIntArray( string str )
		{
			if( string.IsNullOrEmpty( str ) )
				return new int[ 0 ];

			string fixedStr = str.Replace( ',', '.' );

			string[] strings = fixedStr.Split( new char[] { ' ', '\n', '\t', '\r' },
				StringSplitOptions.RemoveEmptyEntries );

			int[] array = new int[ strings.Length ];

			for( int n = 0; n < strings.Length; n++ )
			{
				int value;
				if( !int.TryParse( strings[ n ], out value ) )
					return null;

				array[ n ] = value;
			}

			return array;
		}

		MySceneMaterial GetOrCreateMaterial( string materialName )
		{
			MySceneMaterial material;
			if( !generatedMaterials.TryGetValue( materialName, out material ) )
			{
				material = new MySceneMaterial( materialName );
				generatedMaterials.Add( material.Name, material );
			}
			return material;
		}

		bool ParseSource( XmlNode sourceNode, out SourceItem source )
		{
			source = null;

			string id = XmlUtils.GetAttribute( sourceNode, "id" );

			XmlNode floatArrayNode = XmlUtils.FindChildNode( sourceNode, "float_array" );
			if( floatArrayNode == null )
			{
				Error( "\"float_array\" node is not exists. Source: \"{0}\".", id );
				return false;
			}

			int floatsCount;
			if( !int.TryParse( XmlUtils.GetAttribute( floatArrayNode, "count" ), out floatsCount ) )
			{
				Error( "Invalid \"count\" attribute of floats array. Source: \"{0}\".", id );
				return false;
			}

			float[] data = ConvertStringToFloatArray( floatArrayNode.InnerText );
			if( data == null )
			{
				Error( "Cannot read array with name \"{0}\".", XmlUtils.GetAttribute( floatArrayNode, "id" ) );
				return false;
			}

			if( data.Length != floatsCount )
			{
				Error( "Invalid amount of items in \"float_array\". Required amount: \"{0}\". " +
					"Real amount: \"{1}\". Source: \"{2}\".", floatsCount, data.Length, id );
				return false;
			}

			XmlNode techniqueCommonNode = XmlUtils.FindChildNode( sourceNode, "technique_common" );
			if( techniqueCommonNode == null )
			{
				Error( "\"technique_common\" node is not exists. Source: \"{0}\".", id );
				return false;
			}

			XmlNode accessorNode = XmlUtils.FindChildNode( techniqueCommonNode, "accessor" );
			if( accessorNode == null )
			{
				Error( "\"accessor\" node is not exists. Source: \"{0}\".", id );
				return false;
			}

			int offset = 0;
			{
				string offsetAttribute = XmlUtils.GetAttribute( accessorNode, "offset" );
				if( !string.IsNullOrEmpty( offsetAttribute ) )
				{
					if( !int.TryParse( offsetAttribute, out offset ) )
					{
						Error( "Invalid \"offset\" attribute of accessor. Source: \"{0}\".", id );
						return false;
					}
				}
			}

			int stride = 1;
			{
				string strideAttribute = XmlUtils.GetAttribute( accessorNode, "stride" );
				if( !string.IsNullOrEmpty( strideAttribute ) )
				{
					if( !int.TryParse( strideAttribute, out stride ) )
					{
						Error( "Invalid \"stride\" attribute of accessor. Source: \"{0}\".", id );
						return false;
					}
				}
			}

			int count;
			if( !int.TryParse( XmlUtils.GetAttribute( accessorNode, "count" ), out count ) )
			{
				Error( "Invalid \"count\" attribute of accessor. Source: \"{0}\".", id );
				return false;
			}

			source = new SourceItem( id, data, offset, stride );
			return true;
		}

		bool ParseMeshSources( XmlNode meshNode, out List<SourceItem> sources )
		{
			sources = new List<SourceItem>();

			foreach( XmlNode childNode in meshNode.ChildNodes )
			{
				if( childNode.Name != "source" )
					continue;

				SourceItem source;
				if( !ParseSource( childNode, out source ) )
					return false;

				sources.Add( source );
			}

			return true;
		}

		string GetIdFromURL( string url )
		{
			if( string.IsNullOrEmpty( url ) )
				return "";
			if( url[ 0 ] != '#' )
				return "";
			return url.Substring( 1 );
		}

		bool ParseInputNode( string geometryId, Dictionary<string, string> vertexSemanticDictionary,
			XmlNode inputNode, out int offset, out string sourceId, out ChannelTypes channelType )
		{
			offset = 0;
			sourceId = null;
			channelType = ChannelTypes.Unknown;

			//offset
			if( !int.TryParse( XmlUtils.GetAttribute( inputNode, "offset" ), out offset ) )
			{
				Error( "Invalid \"offset\" attribute. Geometry: \"{0}\".", geometryId );
				return false;
			}

			string semantic = XmlUtils.GetAttribute( inputNode, "semantic" );

			//channelType
			{
				int set = 0;
				{
					string setAsString = XmlUtils.GetAttribute( inputNode, "set" );
					if( !string.IsNullOrEmpty( setAsString ) )
					{
						if( !int.TryParse( setAsString, out set ) )
						{
							Error( "Invalid \"set\" attribute. Geometry: \"{0}\".", geometryId );
							return false;
						}
					}
				}

				if( semantic == "VERTEX" )
					channelType = ChannelTypes.POSITION;
				else if( semantic == "POSITION" )
					channelType = ChannelTypes.POSITION;
				else if( semantic == "NORMAL" )
					channelType = ChannelTypes.NORMAL;
				else if( semantic == "COLOR" )
					channelType = ChannelTypes.COLOR;
				else if( semantic == "TEXCOORD" )
				{
					switch( set )
					{
					case 0: channelType = ChannelTypes.TEXCOORD0; break;
					case 1: channelType = ChannelTypes.TEXCOORD1; break;
					case 2: channelType = ChannelTypes.TEXCOORD2; break;
					case 3: channelType = ChannelTypes.TEXCOORD3; break;
					default: channelType = ChannelTypes.Unknown; break;
					}
				}
				else if( semantic == "TANGENT" )
					channelType = ChannelTypes.TANGENT;
				else
				{
					channelType = ChannelTypes.Unknown;
				}
			}

			//sourceId
			{
				string sourceUrl = XmlUtils.GetAttribute( inputNode, "source" );

				sourceId = GetIdFromURL( sourceUrl );
				if( string.IsNullOrEmpty( sourceId ) )
				{
					Error( "Invalid \"source\" attribute for input node. Source url: \"{0}\". Geometry: \"{1}\".",
						sourceUrl, geometryId );
					return false;
				}

				if( semantic == "VERTEX" )
				{
					string newSourceId;
					if( !vertexSemanticDictionary.TryGetValue( sourceId, out newSourceId ) )
					{
						Error( "Cannot find vertices node with \"{0}\". Geometry: \"{1}\".",
							sourceId, geometryId );
						return false;
					}
					sourceId = newSourceId;
				}
			}

			return true;
		}

		bool ParseInputs( string geometryId, Dictionary<string, SourceItem> sourceDictionary,
			Dictionary<string, string> vertexSemanticDictionary, XmlNode primitiveElementsNode,
			out Pair<ChannelTypes, SourceItem>[] inputs )
		{
			inputs = null;

			List<Pair<ChannelTypes, SourceItem>> inputList = new List<Pair<ChannelTypes, SourceItem>>();

			foreach( XmlNode inputNode in primitiveElementsNode.ChildNodes )
			{
				if( inputNode.Name != "input" )
					continue;

				int offset;
				string sourceId;
				ChannelTypes channelType;

				if( !ParseInputNode( geometryId, vertexSemanticDictionary, inputNode, out offset, out sourceId,
					out channelType ) )
				{
					return false;
				}

				while( inputList.Count < offset + 1 )
					inputList.Add( new Pair<ChannelTypes, SourceItem>( ChannelTypes.Unknown, null ) );

				if( channelType != ChannelTypes.Unknown )
				{
					if( inputList[ offset ].Second != null )
					{
						Error( "Input with offset \"{0}\" is already defined.", offset );
						return false;
					}

					SourceItem source;
					if( !sourceDictionary.TryGetValue( sourceId, out source ) )
					{
						Error( "Source with id \"{0}\" is not exists.", sourceId );
						return false;
					}

					inputList[ offset ] = new Pair<ChannelTypes, SourceItem>( channelType, source );
				}
			}

			inputs = inputList.ToArray();

			return true;
		}

		SubMeshVertex[] GenerateSubMeshVertices( Pair<ChannelTypes, SourceItem>[] inputs, int vertexCount,
			int[] indices, int startIndex )
		{
			SubMeshVertex[] itemVertices = new SubMeshVertex[ vertexCount ];

			int currentIndex = startIndex;

			for( int nVertex = 0; nVertex < itemVertices.Length; nVertex++ )
			{
				SubMeshVertex vertex = new SubMeshVertex();

				foreach( Pair<ChannelTypes, SourceItem> input in inputs )
				{
					ChannelTypes channelType = input.First;
					SourceItem source = input.Second;

					int indexValue = indices[ currentIndex ];
					currentIndex++;

					switch( channelType )
					{
					case ChannelTypes.POSITION:
						vertex.position = source.GetItemVec3( indexValue );
						break;

					case ChannelTypes.NORMAL:
						vertex.normal = source.GetItemVec3( indexValue );
						break;

					case ChannelTypes.TEXCOORD0:
						vertex.texCoord0 = source.GetItemVec2( indexValue );
						break;

					case ChannelTypes.TEXCOORD1:
						vertex.texCoord1 = source.GetItemVec2( indexValue );
						break;

					case ChannelTypes.TEXCOORD2:
						vertex.texCoord2 = source.GetItemVec2( indexValue );
						break;

					case ChannelTypes.TEXCOORD3:
						vertex.texCoord3 = source.GetItemVec2( indexValue );
						break;

					case ChannelTypes.COLOR:
						{
							Vec3 c = source.GetItemVec3( indexValue ); ;
							vertex.color = new ColorValue( c.X, c.Y, c.Z, 1 );
						}
						break;

					//maybe need use "TEXTANGENT".
					//case ChannelTypes.TANGENT:
					//   vertex.tangent = source.GetItemVec3( indexValue );
					//   break;

					}
				}

				itemVertices[ nVertex ] = vertex;
			}

			return itemVertices;
		}

		bool ParseMeshNode( string geometryId, XmlNode meshNode )
		{
			List<SourceItem> sources;
			if( !ParseMeshSources( meshNode, out sources ) )
				return false;

			Dictionary<string, SourceItem> sourceDictionary = new Dictionary<string, SourceItem>();
			foreach( SourceItem source in sources )
				sourceDictionary.Add( source.Id, source );

			//vertexSemanticDictionary
			Dictionary<string, string> vertexSemanticDictionary = new Dictionary<string, string>();
			{
				foreach( XmlNode verticesNode in meshNode.ChildNodes )
				{
					if( verticesNode.Name != "vertices" )
						continue;

					string id = XmlUtils.GetAttribute( verticesNode, "id" );

					XmlNode inputNode = XmlUtils.FindChildNode( verticesNode, "input" );
					if( inputNode == null )
					{
						Error( "\"input\" node is not defined for vertices node \"{0}\". Geometry: \"{1}\".",
							id, geometryId );
						return false;
					}

					string sourceUrl = XmlUtils.GetAttribute( inputNode, "source" );

					string sourceId = GetIdFromURL( sourceUrl );
					if( string.IsNullOrEmpty( sourceId ) )
					{
						Error( "Invalid \"source\" attribute for vertices node \"{0}\". Source url: \"{1}\". " +
							"Geometry: \"{2}\".", id, sourceUrl, geometryId );
						return false;
					}

					vertexSemanticDictionary.Add( id, sourceId );
				}
			}

			List<GeometryItem.SubMesh> geometrySubMeshes = new List<GeometryItem.SubMesh>();

			//polygons, triangles, ...
			foreach( XmlNode primitiveElementsNode in meshNode.ChildNodes )
			{
				bool lines = primitiveElementsNode.Name == "lines";
				bool linestrips = primitiveElementsNode.Name == "linestrips";
				bool polygons = primitiveElementsNode.Name == "polygons";
				bool polylist = primitiveElementsNode.Name == "polylist";
				bool triangles = primitiveElementsNode.Name == "triangles";
				bool trifans = primitiveElementsNode.Name == "trifans";
				bool tristrips = primitiveElementsNode.Name == "tristrips";

				if( lines || linestrips || trifans || tristrips )
				{
					Error( "\"{0}\" primitive element is not supported. Geometry: \"{1}\".",
						primitiveElementsNode.Name, geometryId );
					return false;
				}

				if( polygons || triangles || polylist )
				{
					int itemCount;
					if( !int.TryParse( XmlUtils.GetAttribute( primitiveElementsNode, "count" ), out itemCount ) )
					{
						Error( "Invalid \"count\" attribute of \"{0}\". Geometry: \"{1}\".",
							primitiveElementsNode.Name, geometryId );
						return false;
					}

					//

					Pair<ChannelTypes, SourceItem>[] inputs;
					if( !ParseInputs( geometryId, sourceDictionary, vertexSemanticDictionary,
						primitiveElementsNode, out inputs ) )
					{
						return false;
					}

					int textureCoordCount = 0;
					bool vertexColors = false;
					{
						foreach( Pair<ChannelTypes, SourceItem> input in inputs )
						{
							ChannelTypes channelType = input.First;

							if( channelType >= ChannelTypes.TEXCOORD0 && channelType <= ChannelTypes.TEXCOORD3 )
							{
								int v = channelType - ChannelTypes.TEXCOORD0;
								if( ( v + 1 ) > textureCoordCount )
									textureCoordCount = v + 1;
							}

							if( channelType == ChannelTypes.COLOR )
								vertexColors = true;
						}
					}

					List<SubMeshVertex> vertices = new List<SubMeshVertex>( itemCount );

					if( polygons )
					{
						foreach( XmlNode pNode in primitiveElementsNode.ChildNodes )
						{
							if( pNode.Name != "p" )
								continue;

							int[] indexValues = ConvertStringToIntArray( pNode.InnerText );
							if( indexValues == null )
							{
								Error( "Cannot read index array of geometry \"{0}\".", geometryId );
								return false;
							}

							int vertexCount = indexValues.Length / inputs.Length;
							SubMeshVertex[] itemVertices = GenerateSubMeshVertices( inputs,
								vertexCount, indexValues, 0 );

							//generate triangles
							for( int n = 1; n < itemVertices.Length - 1; n++ )
							{
								vertices.Add( itemVertices[ 0 ] );
								vertices.Add( itemVertices[ n ] );
								vertices.Add( itemVertices[ n + 1 ] );
							}
						}
					}

					if( triangles )
					{
						XmlNode pNode = XmlUtils.FindChildNode( primitiveElementsNode, "p" );

						if( pNode != null )
						{
							int[] indexValues = ConvertStringToIntArray( pNode.InnerText );
							if( indexValues == null )
							{
								Error( "Cannot read \"p\" node of geometry \"{0}\".", geometryId );
								return false;
							}

							int vertexCount = indexValues.Length / inputs.Length;

							if( itemCount != vertexCount / 3 )
							{
								Error( "Invalid item amount of \"p\" node of geometry \"{0}\".", geometryId );
								return false;
							}

							SubMeshVertex[] itemVertices = GenerateSubMeshVertices( inputs,
								vertexCount, indexValues, 0 );

							//generate triangles
							for( int n = 0; n < vertexCount; n++ )
								vertices.Add( itemVertices[ n ] );
						}
					}

					if( polylist )
					{
						XmlNode vCountNode = XmlUtils.FindChildNode( primitiveElementsNode, "vcount" );
						XmlNode pNode = XmlUtils.FindChildNode( primitiveElementsNode, "p" );

						if( vCountNode != null && pNode != null )
						{
							int[] vCount = ConvertStringToIntArray( vCountNode.InnerText );
							if( vCount == null )
							{
								Error( "Cannot read \"vcount\" node of geometry \"{0}\".", geometryId );
								return false;
							}

							if( vCount.Length != itemCount )
							{
								Error( "Invalid item amount of \"vcount\" node of geometry \"{0}\".", geometryId );
								return false;
							}

							int[] indexValues = ConvertStringToIntArray( pNode.InnerText );
							if( indexValues == null )
							{
								Error( "Cannot read \"p\" node of geometry \"{0}\".", geometryId );
								return false;
							}

							int currentIndex = 0;

							foreach( int polyCount in vCount )
							{
								SubMeshVertex[] itemVertices = GenerateSubMeshVertices( inputs, polyCount,
									indexValues, currentIndex );
								currentIndex += polyCount * inputs.Length;

								//generate triangles
								for( int n = 1; n < itemVertices.Length - 1; n++ )
								{
									vertices.Add( itemVertices[ 0 ] );
									vertices.Add( itemVertices[ n ] );
									vertices.Add( itemVertices[ n + 1 ] );
								}

							}

							if( currentIndex != indexValues.Length )
							{
								Error( "Invalid indices of geometry \"{0}\".", geometryId );
								return false;
							}
						}
					}

					int[] indices = new int[ vertices.Count ];
					for( int n = 0; n < indices.Length; n++ )
						indices[ n ] = n;

					MySceneMaterial material;
					{
						string materialName = XmlUtils.GetAttribute( primitiveElementsNode, "material" );
						material = GetOrCreateMaterial( materialName );
					}

					//add to geometrySubMeshes
					GeometryItem.SubMesh geometrySubMesh = new GeometryItem.SubMesh(
						vertices.ToArray(), indices, textureCoordCount, vertexColors, material );
					geometrySubMeshes.Add( geometrySubMesh );

					continue;
				}
			}

			//add to generatedGeometries
			{
				GeometryItem geometry = new GeometryItem( geometryId, geometrySubMeshes.ToArray() );
				generatedGeometries.Add( geometry.Id, geometry );
			}

			return true;
		}

		bool ParseGeometry( XmlNode geometryNode )
		{
			string geometryId = XmlUtils.GetAttribute( geometryNode, "id" );

			//find mesh node
			XmlNode meshNode = XmlUtils.FindChildNode( geometryNode, "mesh" );
			if( meshNode == null )
			{
				Error( "Mesh node is not exists for geometry \"{0}\".", geometryId );
				return false;
			}

			if( !ParseMeshNode( geometryId, meshNode ) )
				return false;

			return true;
		}

		bool ParseGeometries( XmlNode colladaNode )
		{
			foreach( XmlNode libraryGeometriesNode in colladaNode.ChildNodes )
			{
				if( libraryGeometriesNode.Name != "library_geometries" )
					continue;

				foreach( XmlNode geometryNode in libraryGeometriesNode.ChildNodes )
				{
					if( geometryNode.Name != "geometry" )
						continue;

					if( !ParseGeometry( geometryNode ) )
						return false;
				}
			}

			return true;
		}

		bool ParseNodeInstanceGeometry( Mat4 nodeTransform, XmlNode instanceGeometry )
		{
			string url = XmlUtils.GetAttribute( instanceGeometry, "url" );

			bool nodeTransformIdentity = nodeTransform.Equals( Mat4.Identity, .0001f );
			Quat nodeRotation = nodeTransform.ToMat3().ToQuat().GetNormalize();

			string geometryId = GetIdFromURL( url );
			if( string.IsNullOrEmpty( geometryId ) )
			{
				Error( "Invalid \"url\" attribute specified for \"instance_geometry\". Url: \"{0}\".", url );
				return false;
			}

			GeometryItem geometry;
			if( !generatedGeometries.TryGetValue( geometryId, out geometry ) )
			{
				Error( "Geometry with id \"{0}\" is not exists.", geometryId );
				return false;
			}

			foreach( GeometryItem.SubMesh geometrySubMesh in geometry.SubMeshes )
			{
				SubMeshVertex[] newVertices = new SubMeshVertex[ geometrySubMesh.Vertices.Length ];

				for( int n = 0; n < newVertices.Length; n++ )
				{
					SubMeshVertex vertex = geometrySubMesh.Vertices[ n ];

					if( !nodeTransformIdentity )
					{
						vertex.position = nodeTransform * vertex.position;
						vertex.normal = ( nodeRotation * vertex.normal ).GetNormalize();
					}

					newVertices[ n ] = vertex;
				}

				MySceneSubMesh sceneSubMesh = new MySceneSubMesh( newVertices, geometrySubMesh.Indices,
					geometrySubMesh.TextureCoordCount, geometrySubMesh.VertexColors, geometrySubMesh.Material );
				generatedSubMeshes.Add( sceneSubMesh );
			}

			return true;
		}

		bool ParseNode( Mat4 nodeTransform, XmlNode node )
		{
			string nodeId = XmlUtils.GetAttribute( node, "id" );

			Mat4 currentTransform = nodeTransform;

			foreach( XmlNode childNode in node.ChildNodes )
			{
				if( childNode.Name == "matrix" )
				{
					float[] values = ConvertStringToFloatArray( childNode.InnerText );
					if( values == null || values.Length != 16 )
					{
						Error( "Invalid format of \"matrix\" node. Node \"{0}\".", nodeId );
						return false;
					}
					Mat4 matrix = new Mat4( values );
					currentTransform *= matrix;
					continue;
				}

				if( childNode.Name == "translate" )
				{
					float[] values = ConvertStringToFloatArray( childNode.InnerText );
					if( values == null || values.Length != 3 )
					{
						Error( "Invalid format of \"translate\" node. Node \"{0}\".", nodeId );
						return false;
					}
					Vec3 translate = new Vec3( values[ 0 ], values[ 1 ], values[ 2 ] );
					currentTransform *= Mat4.FromTranslate( translate );
					continue;
				}

				if( childNode.Name == "rotate" )
				{
					float[] values = ConvertStringToFloatArray( childNode.InnerText );
					if( values == null || values.Length != 4 )
					{
						Error( "Invalid format of \"rotate\" node. Node \"{0}\".", nodeId );
						return false;
					}

					Vec3 axis = new Vec3( values[ 0 ], values[ 1 ], values[ 2 ] );
					Radian angle = new Degree( values[ 3 ] ).InRadians();

					if( axis != Vec3.Zero )
					{
						axis.Normalize();
						float halfAngle = .5f * angle;
						float sin = MathFunctions.Sin( halfAngle );
						float cos = MathFunctions.Cos( halfAngle );
						Quat r = new Quat( axis * sin, cos );
						r.Normalize();

						currentTransform *= r.ToMat3().ToMat4();
					}

					continue;
				}

				if( childNode.Name == "scale" )
				{
					float[] values = ConvertStringToFloatArray( childNode.InnerText );
					if( values == null || values.Length != 3 )
					{
						Error( "Invalid format of \"scale\" node. Node \"{0}\".", nodeId );
						return false;
					}
					Vec3 scale = new Vec3( values[ 0 ], values[ 1 ], values[ 2 ] );
					currentTransform *= Mat3.FromScale( scale ).ToMat4();
					continue;
				}

				if( childNode.Name == "node" )
				{
					if( !ParseNode( currentTransform, childNode ) )
						return false;

					continue;
				}

				if( childNode.Name == "instance_geometry" )
				{
					if( !ParseNodeInstanceGeometry( currentTransform, childNode ) )
						return false;

					continue;
				}
			}

			return true;
		}

		bool ParseColladaNode( XmlNode colladaNode )
		{
			//globalScale, yAxisUp
			{
				XmlNode assetNode = XmlUtils.FindChildNode( colladaNode, "asset" );
				if( assetNode != null )
				{
					XmlNode upAxisNode = XmlUtils.FindChildNode( assetNode, "up_axis" );
					if( upAxisNode != null )
					{
						if( upAxisNode.InnerText == "Z_UP" )
							yAxisUp = false;
						else if( upAxisNode.InnerText == "X_UP" )
						{
							Error( "X up axis is not supported." );
							return false;
						}
					}

					XmlNode unitNode = XmlUtils.FindChildNode( assetNode, "unit" );
					if( unitNode != null )
					{
						string meterStr = XmlUtils.GetAttribute( unitNode, "meter" );
						if( !string.IsNullOrEmpty( meterStr ) )
						{
							string fixedStr = meterStr.Replace( ',', '.' );

							if( !float.TryParse( fixedStr, out globalScale ) )
							{
								Error( "Invalid \"meter\" attribute of \"unit\" node." );
								return false;
							}
						}
					}
				}
			}

			//library_geometries
			if( !ParseGeometries( colladaNode ) )
				return false;

			//library_visual_scenes
			foreach( XmlNode visualScenesNode in colladaNode.ChildNodes )
			{
				if( visualScenesNode.Name != "library_visual_scenes" )
					continue;

				foreach( XmlNode visualSceneNode in visualScenesNode.ChildNodes )
				{
					if( visualSceneNode.Name != "visual_scene" )
						continue;

					foreach( XmlNode nodeNode in visualSceneNode.ChildNodes )
					{
						if( nodeNode.Name != "node" )
							continue;

						Mat4 currentTransform = Mat4.Identity;

						if( globalScale != 1 )
						{
							Mat4 m = Mat3.FromScale( new Vec3( globalScale, globalScale, globalScale ) ).ToMat4();
							currentTransform *= m;
						}

						if( yAxisUp )
						{
							Mat4 mayaToNeoAxisRotation = new Mat4(
								new Vec4( 0, 1, 0, 0 ),
								new Vec4( 0, 0, 1, 0 ),
								new Vec4( 1, 0, 0, 0 ),
								new Vec4( 0, 0, 0, 1 ) );

							currentTransform *= mayaToNeoAxisRotation;
						}

						if( !ParseNode( currentTransform, nodeNode ) )
							return false;
					}
				}
			}

			return true;
		}

		void ClearTemporaryFields()
		{
			currentFileName = null;
			currentMesh = null;
			globalScale = 1;
			yAxisUp = true;
			generatedMaterials = null;
			generatedGeometries = null;
			generatedSubMeshes = null;
		}

		protected override bool Load( string virtualFileName, Mesh mesh )
		{
			currentFileName = virtualFileName;
			currentMesh = mesh;
			globalScale = 1;
			yAxisUp = true;
			generatedMaterials = new Dictionary<string, MySceneMaterial>();
			generatedGeometries = new Dictionary<string, GeometryItem>();
			generatedSubMeshes = new List<MySceneSubMesh>();

			//load file
			XmlNode colladaNode;
			{
				try
				{
					using( Stream stream = VirtualFile.Open( virtualFileName ) )
					{
						string fileText = new StreamReader( stream ).ReadToEnd();
						colladaNode = XmlUtils.LoadFromText( fileText );
					}
				}
				catch( Exception ex )
				{
					Error( ex.Message );
					return false;
				}
			}

			//parse
			if( !ParseColladaNode( colladaNode ) )
			{
				ClearTemporaryFields();
				return false;
			}

			//generate mesh
			const bool tangents = true;
			const bool edgeList = true;
			const bool allowMergeSubMeshes = true;
			MeshConstructor meshConstructor = new MeshConstructor( tangents, edgeList, allowMergeSubMeshes );

			MyMeshSceneObject meshSceneObject = new MyMeshSceneObject( generatedSubMeshes.ToArray() );

			if( !meshConstructor.DoExport( meshSceneObject, mesh ) )
			{
				ClearTemporaryFields();
				return false;
			}

			ClearTemporaryFields();

			return true;
		}

	}
}
