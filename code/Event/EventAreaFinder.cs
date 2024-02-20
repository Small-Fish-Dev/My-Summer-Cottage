using Sandbox;

[Icon( "crop_din" )]
[Category( "Events" )]
public sealed class EventAreaFinder : EventTrigger, Component.ITriggerListener
{
	[Property]
	public Vector3 Offset { get; set; }

	[Property]
	public Vector3 Extents { get; set; }

	[Property]
	public TagSet TagSet { get; set; }

	/// <summary>
	/// Get all objects inside of this area
	/// </summary>
	List<GameObject> ObjectsInside { get; set; } = new();

	public BoxCollider Collider { get; private set; }

	public BBox BBox => new BBox( Offset - Extents / 2f, Offset + Extents / 2f );

	protected override void OnStart()
	{
		Collider = Components.GetOrCreate<BoxCollider>();
		Collider.Center = Offset;
		Collider.Scale = Extents;
		Collider.IsTrigger = true;
	}

	protected override void OnUpdate()
	{
	}

	public void OnTriggerEnter( Collider other )
	{
		if ( other.Tags.HasAny( TagSet ) )
			if ( !ObjectsInside.Contains( other.GameObject ) )
				ObjectsInside.Add( other.GameObject );
	}

	public void OnTriggerExit( Collider other )
	{
		if ( ObjectsInside.Contains( other.GameObject ) )
			ObjectsInside.Remove( other.GameObject );
	}

	protected override void DrawGizmos()
	{
		Gizmo.Draw.Color = Color.Yellow.WithAlpha( 0.2f );
		Gizmo.Draw.SolidBox( BBox );
		Gizmo.Draw.Color = Color.White.WithAlpha( 0.3f );
		Gizmo.Draw.LineBBox( BBox );
	}
}
