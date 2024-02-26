namespace Sauna;

public sealed class WaterComponent : Component
{
	private const string MATERIAL_PATH = "materials/water/water.vmat";
	private const int SUBDIVISIONS = 32;

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

	protected override void DrawGizmos()
	{
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
			new Vector3( Mins.x, Mins.y, 0f ),
			new Vector3( Mins.x, Maxs.y, 0f ),
			new Vector3( Maxs.x, Maxs.y, 0f ),
			new Vector3( Maxs.x, Mins.y, 0f )
		};

		var verts = new List<SimpleVertex>();
		var indices = new List<int>();

		var start = new Vector2( positions[0] );
		var stepSize = new Vector2( positions[0].Abs() + positions[3].Abs() ) / SUBDIVISIONS;

		for ( var x = 0; x < SUBDIVISIONS; x++ )
		for ( var y = 0; y < SUBDIVISIONS; y++ )
		{
			verts.Add( new SimpleVertex()
			{
				position = new Vector3( start + stepSize * new Vector2( x, y ) ),
				normal = Vector3.Up,
				tangent = Vector3.Cross( Vector3.Up, Vector3.Forward ),
				texcoord = new Vector2( x, y ) / SUBDIVISIONS
			} );
		}

		for ( int y = 0; y < (SUBDIVISIONS - 1); y++ )
		for ( int x = 0; x < (SUBDIVISIONS - 1); x++ )
		{
			var quad = y * SUBDIVISIONS + x;

			indices.Add( quad );
			indices.Add( quad + SUBDIVISIONS );
			indices.Add( quad + SUBDIVISIONS + 1 );

			indices.Add( quad );
			indices.Add( quad + SUBDIVISIONS + 1 );
			indices.Add( quad + 1 );
		}

		mesh.CreateVertexBuffer<SimpleVertex>( verts.Count, SimpleVertex.Layout, verts.ToArray() );
		mesh.CreateIndexBuffer( indices.Count, indices.ToArray() );

		return Model.Builder
			.AddMesh( mesh )
			.Create();
	}
}
