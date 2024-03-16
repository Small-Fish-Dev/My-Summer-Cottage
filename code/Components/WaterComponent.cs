namespace Sauna;

public sealed class WaterComponent : Component
{
	private const string MATERIAL_PATH = "materials/water/water.vmat";
	private const int MIN_SUBDIVISIONS = 32;

	[Property][Range( 0, 1 )] public float BobberDrag { get; set; } = 0.8f;
	[Property] public Vector3 Mins { get; set; } = -25;
	[Property] public Vector3 Maxs { get; set; } = 25;

	[Property, Sync, Category( "Appearance" )]
	public Color Color { get; set; } = Color.Blue;

	[Property, Sync, Category( "Appearance" ), Range( 0.01f, 5f )]
	public float TextureScale { get; set; } = 0.4f;

	public BBox Bounds => new BBox( Mins, Maxs );

	public Model Model
	{
		get
		{
			if ( _model == null )
				_model = GenerateModel();

			if ( _sceneObject != null )
				_sceneObject.Model = _model;

			return _model;
		}
	}

	private Model _model;
	private SceneObject _sceneObject;
	private BoxCollider _collider;

	public bool Contains( Vector3 vec )
		=> Bounds.Transform( Transform.World ).Contains( vec );

	public bool Contains( BBox bbox )
		=> Bounds.Transform( Transform.World ).Contains( bbox );

	protected override void OnStart()
	{
		_model = null;

		_sceneObject?.Delete();
		_sceneObject = new SceneObject( Scene.SceneWorld, Model );
		_sceneObject.Transform = Transform.World;
	}

	protected override void OnAwake()
	{
		_collider = Components.GetOrCreate<BoxCollider>();
		_collider.Center = Bounds.Center;
		_collider.Scale = Bounds.Size;
		_collider.IsTrigger = true;
	}

	protected override void OnFixedUpdate()
	{
		foreach ( var other in _collider.Touching )
		{
			if ( other.Tags.Has( "bobber" ) )
			{
				var bounds = other.Rigidbody.PhysicsBody.GetBounds();
				var depth = Maxs.z + Transform.Position - bounds.Mins.z;
				var bobberHeight = bounds.Size.z;
				var percentInWater = depth.Clamp( 0, bobberHeight ) / bobberHeight;

				other.Rigidbody.Velocity = other.Rigidbody.Velocity * BobberDrag
										   + Vector3.Up * other.Rigidbody.PhysicsBody.Mass * 25 * percentInWater
										   // Simulating the waves
										   * ((float)Math.Sin( Time.Now * 5 )).Remap( -1, 1, 0.5f, 1 );
			}
		}

		foreach ( var player in Scene.GetAllComponents<Player>() )
		{
			var difference = Bounds.Transform( Transform.World ).Maxs.z - player.Transform.Position.z;

			if ( difference >= 48f )
			{
				player.MoveHelper.Velocity = player.MoveHelper.Velocity.WithZ( 20f );
				player.MoveHelper.IsOnGround = false;
			}
		}

		foreach ( var rigidbody in Scene.GetAllComponents<Rigidbody>() )
		{
			if ( rigidbody.Enabled && rigidbody.MotionEnabled )
			{
				var difference = Bounds.Transform( Transform.World ).Maxs.z - rigidbody.PhysicsBody.GetBounds().Center.z;

				if ( difference > 0f )
				{
					rigidbody.Velocity = rigidbody.Velocity.WithZ( 20f );
					rigidbody.LinearDamping = 1f;
				}
				else
				{
					rigidbody.LinearDamping = 0.01f;
				}
			}
		}
	}

	protected override void DrawGizmos()
	{
		if ( Game.IsPlaying )
			return;

		Gizmo.Draw.Color = Color.Blue;
		Gizmo.Draw.LineBBox( Bounds );

		Gizmo.Draw.Color = Color.Blue.WithAlpha( 0.15f );
		Gizmo.Draw.SolidBox( Bounds );
	}

	protected override void OnPreRender()
	{
		if ( _sceneObject == null )
			return;

		_sceneObject.Transform = Transform.World;
		_sceneObject.Attributes.Set( "g_flTextureScale", TextureScale );
		_sceneObject.Attributes.Set( "g_flOpacity", Color.a );
		_sceneObject.Attributes.Set( "g_flColorTint", Color );
	}

	private Model GenerateModel()
	{
		var mesh = new Mesh( Material.Load( MATERIAL_PATH ) );
		var positions = new Vector3[]
		{
			new( Mins.x, Mins.y, 0f ),
			new( Mins.x, Maxs.y, 0f ),
			new( Maxs.x, Maxs.y, 0f ),
			new( Maxs.x, Mins.y, 0f )
		};

		var fraction = 0.02f;
		var sy = (Bounds.Size.y * fraction).FloorToInt();
		var subdivisions = (Bounds.Size.x * fraction).FloorToInt();
		subdivisions = Math.Min( MIN_SUBDIVISIONS, sy > subdivisions ? sy : subdivisions );

		var verts = new List<SimpleVertex>();
		var indices = new List<int>();

		var start = new Vector2( positions[0] );
		var stepSize = new Vector2( positions[0].Abs() + positions[3].Abs() ) / subdivisions;

		for ( var x = 0; x < subdivisions; x++ )
			for ( var y = 0; y < subdivisions; y++ )
			{
				verts.Add( new SimpleVertex()
				{
					position = new Vector3( start + stepSize * new Vector2( x, y ) ),
					normal = Vector3.Up,
					tangent = Vector3.Cross( Vector3.Up, Vector3.Forward ),
					texcoord = new Vector2( x, y ) / subdivisions
				} );
			}

		for ( int y = 0; y < (subdivisions - 1); y++ )
			for ( int x = 0; x < (subdivisions - 1); x++ )
			{
				var quad = y * subdivisions + x;

				indices.Add( quad );
				indices.Add( quad + subdivisions );
				indices.Add( quad + subdivisions + 1 );

				indices.Add( quad );
				indices.Add( quad + subdivisions + 1 );
				indices.Add( quad + 1 );
			}

		mesh.CreateVertexBuffer<SimpleVertex>( verts.Count, SimpleVertex.Layout, verts.ToArray() );
		mesh.CreateIndexBuffer( indices.Count, indices.ToArray() );

		return Model.Builder
			.AddMesh( mesh )
			.Create();
	}
}
