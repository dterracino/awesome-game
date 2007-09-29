using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace AwesomeGame.Terrain
{
	public class SimpleTerrain : GameObject
	{
		private VertexDeclaration _vertexDeclaration;
		private VertexBuffer _vertexBuffer;
		private IndexBuffer _indexBuffer;
		private int _size;
		private string _textureAssetName;
		private string _heightMapName;
		private Texture2D _texture;
		private float[] _heightMap;

		private Effect _effect;

		private int _numVertices;
		private int _numIndices;

		public SimpleTerrain(Game game, int size, string textureAssetName)
			: base(game)
		{
			_size = size;

			_textureAssetName = textureAssetName;
		}

		public SimpleTerrain(Game game, string heightMapAssetName , string textureAssetName)
			: base(game)
		{
			_textureAssetName = textureAssetName;
			_heightMapName = heightMapAssetName;
		}

		protected override void LoadGraphicsContent(bool loadAllContent)
		{
			base.LoadGraphicsContent(loadAllContent);

			if (loadAllContent)
			{
				//grab a handle on the content manager
				ContentManager content = (ContentManager)this.Game.Services.GetService(typeof(ContentManager));

				_effect = this.GetService<ContentManager>().Load<Effect>(@"Terrain\SimpleTerrain");

				// if we have a height map, use this for the dimensions
				if (_heightMapName != null)
				{
					//read the height data from the height map
					Texture2D _heightmapTexture = content.Load<Texture2D>(_heightMapName);
					//take the size from the height map (we're assuming it is square)
					_size = _heightmapTexture.Width;

					//get the heights from the height map
					Color[] heights = new Color[_size * _size];
					_heightmapTexture.GetData<Color>(heights);

					_heightMap = new float[_size * _size];
					//take the red values for height data
					for (int i = 0; i < _size * _size; i++)
					{
						_heightMap[i] = heights[i].R * 0.02f;
					}

				}
				_numVertices = _size * _size;
				int numInternalRows = _size - 2;
				_numIndices = (2 * _size * (1 + numInternalRows)) + (2 * numInternalRows);

				//generate texture vertices
				VertexPositionNormalTexture[] vertices = new VertexPositionNormalTexture[_numVertices];
				for (int y = 0; y < _size; y++)
				{
					for (int x = 0; x < _size; x++)
					{
						float height = GetHeight(x, y);

						vertices[GetIndex(x, y)] = new VertexPositionNormalTexture(
							new Vector3(x , y , height),
							new Vector3(0, 0, 1),
							new Vector2(x / (float) (_size - 1), y / (float) (_size - 1)));
					}
				}

				_vertexBuffer = new VertexBuffer(
					this.GraphicsDevice,
					typeof(VertexPositionNormalTexture),
					vertices.Length,
					ResourceUsage.WriteOnly,
					ResourceManagementMode.Automatic);
				_vertexBuffer.SetData<VertexPositionNormalTexture>(vertices);

				short[] indices = new short[_numIndices]; int indexCounter = 0;

				for (int y = 0; y < _size - 1; y++)
				{
					// insert index for degenerate triangle
					if (y > 0)
						indices[indexCounter++] = GetIndex(0, y);

					for (int x = 0; x < _size; x++)
					{
						indices[indexCounter++] = GetIndex(x, y);
						indices[indexCounter++] = GetIndex(x, y + 1);
					}

					// insert index for degenerate triangle
					if (y < _size - 2)
						indices[indexCounter++] = GetIndex(_size - 1, y);
				}

				_indexBuffer = new IndexBuffer(
					this.GraphicsDevice,
					typeof(short),
					indices.Length,
					ResourceUsage.WriteOnly,
					ResourceManagementMode.Automatic);
				_indexBuffer.SetData<short>(indices);

				_vertexDeclaration = new VertexDeclaration(
					this.GraphicsDevice, VertexPositionNormalTexture.VertexElements);

				if (_textureAssetName != null)
				{
					_texture = content.Load<Texture2D>(_textureAssetName);
				}

				_effect.Parameters["GrassTexture"].SetValue(_texture);
				_effect.Parameters["TerrainSize"].SetValue(_size);
			}
		}

		private short GetIndex(int x, int y)
		{
			return (short) ((y * _size) + x);
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			//Matrix viewMatrix = Matrix.CreateLookAt(
			//    new Vector3(0.0f, 250.0f, 250),
			//    Vector3.Zero,
			//    new Vector3(0.0f, 1.0f, 0.0f));
			//Matrix projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
			//    MathHelper.ToRadians(45),
			//    (float) this.GraphicsDevice.Viewport.Width / (float) this.GraphicsDevice.Viewport.Height,
			//    1.0f, 1000.0f);

			Matrix viewMatrix = this.GetService<Camera>().ViewMatrix;
			Matrix projectionMatrix = this.GetService<Camera>().ProjectionMatrix;
			
			_effect.Parameters["WorldViewProjection"].SetValue(viewMatrix * projectionMatrix);
		}

		public override void Draw(GameTime gameTime)
		{
			//this.GraphicsDevice.RenderState.FillMode = FillMode.WireFrame;
			this.GraphicsDevice.VertexDeclaration = _vertexDeclaration;
			this.GraphicsDevice.Vertices[0].SetSource(_vertexBuffer, 0, VertexPositionNormalTexture.SizeInBytes);
			this.GraphicsDevice.Indices = _indexBuffer;

			_effect.Begin();
			foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
			{
				pass.Begin();

				this.GraphicsDevice.DrawIndexedPrimitives(
					PrimitiveType.TriangleStrip,
					0,
					0,
					_numVertices,
					0,
					_numIndices - 2);

				pass.End();
			}
			_effect.End();
		}

		/*public override float GetHeight(float x, float z)
		{
			int integerX = MathsHelper.FloorToInt(x);
			int integerZ = MathsHelper.FloorToInt(z);
			float fractionalX = x - integerX;
			float fractionalZ = z - integerZ;

			float v1 = GetHeight(integerX, integerZ);
			float v2 = GetHeight(integerX + 1, integerZ);
			float v3 = GetHeight(integerX, integerZ + 1);
			float v4 = GetHeight(integerX + 1, integerZ + 1);

			float i1 = PerlinNoise.Interpolate(v1, v2, fractionalX);
			float i2 = PerlinNoise.Interpolate(v3, v4, fractionalX);

			return PerlinNoise.Interpolate(i1, i2, fractionalZ);
		}
*/
		public float GetHeight(int x, int y)
		{
			return _heightMap[GetIndex(x, y)];
			
		}
	}
}
