using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace AwesomeGame.Terrain
{
	public class SimpleTerrain : GameObject
	{
		private VertexBuffer _vertexBuffer;
		private IndexBuffer _indexBuffer;
		private int _size;
		private string _textureAssetName;
		private string _heightMapName;
		private string _objectMapName;

		private Texture2D _texture;
		private Texture2D _normalMap;
		private float[] _heightMap;		//height map

		private Matrix translationMatrix = Matrix.Identity;		//where to put the terrain (so it gets centered)
		private Effect _effect;
		
		private Vector3 _mapScale = Vector3.One;	//scale texture map data by this
		private Vector3 _mapOffset = Vector3.Zero;	//offset texture map data by this

		private int _numVertices;
		private int _numIndices;

		private Matrix _textureScaleAndOffsetMatrix;

		public int Size
		{
			get { return _size; }
		}

		public SimpleTerrain(Game game, int size, string textureAssetName)
			: base(game)
		{
			_size = size;

			_textureAssetName = textureAssetName;
		}

		public SimpleTerrain(Game game, string heightMapAssetName , string textureAssetName, string objectMapName)
			: base(game)
		{
			_textureAssetName = textureAssetName;
			_heightMapName = heightMapAssetName;
			_objectMapName = objectMapName;
		}

		public Vector3 GetPosition(int x, int z)
		{
			return (new Vector3(x, GetHeight(x, z), z) * _mapScale) + _mapOffset;
		}

		protected override void LoadContent()
		{
				//grab a handle on the content manager
				ContentManager content = (ContentManager)this.Game.Services.GetService(typeof(ContentManager));

				_effect = this.GetService<ContentManager>().Load<Effect>(@"Terrain\SimpleTerrain");
				/*foreach (EffectTechnique technique in _effect.Techniques.GetValidTechniques())
				{
					_effect.CurrentTechnique = technique;
					break;
				}*/

				// if we have a height map, use this for the dimensions
				if (_heightMapName != null)
				{
					//read the height data from the height map
					Texture2D _heightmapTexture = content.Load<Texture2D>(_heightMapName);
					//take the size from the height map (we're assuming it is square)
					_size = _heightmapTexture.Width;

					//translationMatrix = Matrix.CreateTranslation(-_size / 2, 0, -_size / 2);
					//translationMatrix = Matrix.CreateTranslation(0, 0, 0);

					//get the heights from the height map
					Color[] heights = new Color[_size * _size];
					_heightmapTexture.GetData<Color>(heights);

					_heightMap = new float[_size * _size];
					//take the red values for height data
					for (int i = 0; i < _size * _size; i++)
					{
						_heightMap[i] = heights[i].R;
					}
				}

				_numVertices = _size * _size;
				int numInternalRows = _size - 2;
				_numIndices = (2 * _size * (1 + numInternalRows)) + (2 * numInternalRows);

				//our map is square
				const int MAPDIMENSION = 2048;
				_mapScale = new Vector3(MAPDIMENSION / (float)_size, 1.0f, MAPDIMENSION / (float)_size);
				_mapOffset = new Vector3(-MAPDIMENSION / 2, 1, -MAPDIMENSION / 2);	//move to origin

				//get our map objects
				Texture2D objectMapTexture = content.Load<Texture2D>(_objectMapName);
				int objectMapSize = objectMapTexture.Width;
				Color[] objects = new Color[objectMapSize * objectMapSize];
				objectMapTexture.GetData<Color>(objects);
				Vector3 mapObjectScale = new Vector3(MAPDIMENSION / (float)objectMapSize, 1.0f, MAPDIMENSION / (float)objectMapSize);

				//list which model to use for each object
				InGameObject[] inGameObject = new InGameObject[256];
				inGameObject[255] = new InGameObject("Cone", 1.5f, false, false, 50);
				inGameObject[254] = new InGameObject("Building1", 4.0f, true, false, 5);
				inGameObject[253] = new InGameObject("Building2", 4.0f, true, false, 5);
				inGameObject[252] = new InGameObject("Building3", 4.0f, true, false, 5);
				inGameObject[251] = new InGameObject("Building4", 5.0f, true, false, 5);
				inGameObject[250] = new InGameObject("Building5", 8.0f, true, false, 5);
				inGameObject[249] = new InGameObject("Building6", 4.0f, true, false, 5);
				inGameObject[248] = new InGameObject("Building7", 4.0f, true, false, 5);
				inGameObject[247] = new InGameObject("Building8", 4.0f, true, false, 5);
				inGameObject[246] = new InGameObject("Checkpoint", 1.0f, false, false, 5);
				inGameObject[245] = new InGameObject("bridge", 7.0f, true, false, 5);

				inGameObject[243] = new InGameObject("sheep", 3.0f, false, false, 25);

				//take the red values for height data
				for (int i = 0; i < objectMapSize * objectMapSize; i++)
				{
					//extract object info from colours
					int objectIndex = objects[i].R;
					float objectRotation = (float)objects[i].G / 256.0f * 360.0f;
					if (inGameObject[objectIndex] != null)
					{
						if (inGameObject[objectIndex].createdInstances < inGameObject[objectIndex].maxInstances)
						{
							Vector3 newObjectPos = new Vector3(i % objectMapSize - 10, 0.0f, (int)(i / objectMapSize) - 10) * mapObjectScale + _mapOffset;
							newObjectPos.Y = GetHeight(newObjectPos.X, newObjectPos.Z);
							Matrix trans = Matrix.CreateRotationY(MathHelper.ToRadians(objectRotation)) * Matrix.CreateScale(inGameObject[objectIndex].scale) * Matrix.CreateTranslation(newObjectPos);

							GameObject newObject;
							newObject = CreateMesh(this.Game, inGameObject[objectIndex].model, trans);

							newObject.collidable = inGameObject[objectIndex].collideable;
							newObject.moveable = inGameObject[objectIndex].moveable;

							if (objectIndex == 255)
							{
								// cone
								Physics.ParticleSystem newCone = new Physics.ParticleSystem(this.Game, global::AwesomeGame.Physics.enumPhysicsObjects.Cone, newObjectPos);
								newCone.graphicObject = newObject;	//tell it to use the new graphics object for display
								this.Game.Components.Add(newCone);	//the physics need to be added to the components
							}
							if (objectIndex == 243)
							{
								// sheep
								Physics.ParticleSystem newSheep = new Physics.ParticleSystem(this.Game, global::AwesomeGame.Physics.enumPhysicsObjects.Sheep, newObjectPos);
								newSheep.graphicObject = newObject;	//tell it to use the new graphics object for display
								this.Game.Components.Add(newSheep);	//the physics need to be added to the components
							}
							if (objectIndex == 246)
							{
								//add a checkpoint to the course
								((Course)this.Game.Services.GetService(typeof(Course))).addCheckpoint(newObject);
							}

							//add the objects to the game components
							this.Game.Components.Add(newObject);

							//increment the count of this number of objects
							inGameObject[objectIndex].createdInstances++;
						}
					}
				}

				//generate texture vertices
				NormalMap normalMap = new NormalMap(this);
				Color[] normals = new Color[_size * _size];
				VertexPositionNormalTexture[] vertices = new VertexPositionNormalTexture[_numVertices];
				for (int z = 0; z < _size; z++)
				{
					for (int x = 0; x < _size; x++)
					{
						Vector3 normal = normalMap.GetNormal(x, z);
						vertices[GetIndex(x, z)] = new VertexPositionNormalTexture(
							GetPosition(x, z),
							normal,
							new Vector2(x / (float) (_size - 1), z / (float) (_size - 1)));
							//new Vector2(2.0f * x / _size , 2.0f * z / _size ));

						normal /= 2;
						normal += new Vector3(0.5f);
						normals[GetIndex(x, z)] = new Color(normal);
					}
				}

				_vertexBuffer = new VertexBuffer(
					this.GraphicsDevice,
					VertexPositionNormalTexture.VertexDeclaration,
					vertices.Length,
					BufferUsage.WriteOnly);
				_vertexBuffer.SetData<VertexPositionNormalTexture>(vertices);

				short[] indices = new short[_numIndices]; int indexCounter = 0;

				for (int z = 0; z < _size - 1; z++)
				{
					// insert index for degenerate triangle
					if (z > 0)
						indices[indexCounter++] = GetIndex(0, z);

					for (int x = 0; x < _size; x++)
					{
						indices[indexCounter++] = GetIndex(x, z);
						indices[indexCounter++] = GetIndex(x, z + 1);
					}

					// insert index for degenerate triangle
					if (z < _size - 2)
						indices[indexCounter++] = GetIndex(_size - 1, z);
				}

				_indexBuffer = new IndexBuffer(
					this.GraphicsDevice,
					IndexElementSize.SixteenBits,
					indices.Length,
					BufferUsage.WriteOnly);
				_indexBuffer.SetData(indices);

				if (_textureAssetName != null)
				{
					_texture = content.Load<Texture2D>(_textureAssetName);
				}

				_effect.Parameters["GrassTexture"].SetValue(_texture);
				_effect.Parameters["TerrainSize"].SetValue(_size);

				_normalMap = new Texture2D(this.GraphicsDevice, _size, _size, true, SurfaceFormat.Color);
				_normalMap.SetData<Color>(normals);
				_effect.Parameters["NormalMapTexture"].SetValue(_normalMap);

				ShadowMap shadowMap = GetService<ShadowMap>();
				if (shadowMap != null)
				{
					_effect.Parameters["ShadowMapSize"].SetValue(shadowMap.ShadowMapSize);
					_effect.Parameters["ShadowMapSizeInverse"].SetValue(1.0f / (float) shadowMap.ShadowMapSize);

					float offset = 0.5f + (0.5f / (float) shadowMap.ShadowMapSize);
					_textureScaleAndOffsetMatrix = new Matrix(
						0.5f, 0.0f, 0.0f, 0.0f,
						0.0f, -0.5f, 0.0f, 0.0f,
						0.0f, 0.0f, 1.0f, 0.0f,
						offset, offset, 0.0f, 1.0f);
				}
		}

		private Mesh CreateMesh(Game game, string filename, Matrix trans)
		{
			if (filename == "Cone")
				return new Models.Cone(game, trans);
			else if (filename == "Checkpoint")
				return new Models.Checkpoint(game, trans);
			else
				return new Mesh(game, @"Models\" + filename, trans);
		}

		private short GetIndex(int x, int z)
		{
			return (short) ((z * _size) + x);
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			Matrix viewMatrix = this.GetService<Camera>().ViewMatrix;
			Matrix projectionMatrix = this.GetService<Camera>().ProjectionMatrix;

			_effect.Parameters["WorldViewProjection"].SetValue(translationMatrix * viewMatrix * projectionMatrix);
		}

		public override void Draw(GameTime gameTime)
		{
			Sunlight light = GetService<Sunlight>();
			if (light != null)
				_effect.Parameters["ShadowMapProjector"].SetValue(light.ViewMatrix * light.ProjectionMatrix * _textureScaleAndOffsetMatrix);

			// render geometry with shadow
			ShadowMap shadowMap = GetService<ShadowMap>();
			if (shadowMap != null)
				_effect.Parameters["ShadowMap"].SetValue(shadowMap.ShadowMapTexture);

			//this.GraphicsDevice.RenderState.FillMode = FillMode.WireFrame;
			this.GraphicsDevice.SetVertexBuffer(_vertexBuffer);
			this.GraphicsDevice.Indices = _indexBuffer;

			foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
			{
				pass.Apply();

				this.GraphicsDevice.DrawIndexedPrimitives(
					PrimitiveType.TriangleStrip,
					0,
					0,
					_numVertices,
					0,
					_numIndices - 2);
			}
		}

		public float GetHeight(float x, float z)
		{
			//this takes coordinates in world geometry
			
			//convert to heightmap coordinate space
			x = (x - _mapOffset.X) / _mapScale.X;
			z = (z - _mapOffset.Z) / _mapScale.Z;
			
			int integerX = (int)Math.Floor(x);
			int integerZ = (int)Math.Floor(z);
			float fractionalX = x - integerX;
			float fractionalZ = z - integerZ;

			float v1 = GetHeight(integerX, integerZ);
			float v2 = GetHeight(integerX + 1, integerZ);
			float v3 = GetHeight(integerX, integerZ + 1);
			float v4 = GetHeight(integerX + 1, integerZ + 1);

			//float i1 = v1 + (v1, v2, fractionalX);
			//float i2 = PerlinNoise.Interpolate(v3, v4, fractionalX);
			//return PerlinNoise.Interpolate(i1, i2, fractionalZ);

			float i1 = v1 + ((v2 - v1) * fractionalX);
			float i2 = v3 + ((v4 - v3) * fractionalX);

			//convert back to world coordinates
			float height = i1 + ((i2 - i1) * fractionalZ);
			height = (height * _mapScale.Y) + _mapOffset.Y;

			return height;
		}

		private float GetHeight(int x, int z)
		{
			//this takes coordinates in the height map geometry
			if (x >= 0 && x < _size && z >= 0 && z < _size)
			{
				return _heightMap[GetIndex(x, z)];
			}
			else
			{
				// car is off the map
				return -1000.0f;
			}	
		}

		public Vector3 GetNormal(float x, float z)
		{
			//convert to heightmap coordinate space
			x = (x - _mapOffset.X) / _mapScale.X;
			z = (z - _mapOffset.Z) / _mapScale.Z;

			return new NormalMap(this).GetNormal((int)x, (int)z);
		}

		private class InGameObject
		{
			public string model ="";
			public float scale = 1.0f;
			public bool collideable =false;
			public bool moveable = false;
			public int maxInstances = 100;
			public int createdInstances =0;

			public InGameObject(string model, float scale, bool collideable, bool moveable, int maxInstances)
			{
				this.model = model;
				this.scale = scale;
				this.collideable = collideable;
				this.moveable = moveable;
				this.maxInstances = maxInstances;
			}
		}
	}
}
