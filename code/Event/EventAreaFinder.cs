using Sandbox;
using Sauna.Event;

[Icon( "crop_din" )]
[Category( "Events" )]
public sealed class EventAreaFinder : EventTrigger
{
	[Property]
	public Vector3 Offset { get; set; }

	[Property]
	public Vector3 Extents { get; set; }

	[Property]
	public TagSet TagSet { get; set; }

	public override bool IsPolled { get; set; } = true;

	/// <summary>
	/// Get all objects inside of this area
	/// </summary>
	List<GameObject> ObjectsInside { get; set; } = new();

	public BBox BBox => new BBox( Offset - Extents / 2f, Offset + Extents / 2f );
	public BBox WorldBBox => BBox.Transform( GameObject.Transform.World );

	protected override void OnStart()
	{
	}

	protected override void OnUpdate()
	{
	}

	public override void PolledMethod()
	{
		var find = Scene.FindInPhysics( WorldBBox );

		if ( find != null )
			ObjectsInside = find.ToList();
	}

	protected override void DrawGizmos()
	{
		Gizmo.Draw.Color = Color.Yellow.WithAlpha( 0.2f );
		Gizmo.Draw.SolidBox( BBox );
		Gizmo.Draw.Color = Color.White.WithAlpha( 0.3f );
		Gizmo.Draw.LineBBox( BBox );
	}
}
