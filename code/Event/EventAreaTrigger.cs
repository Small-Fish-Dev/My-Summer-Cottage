using Sandbox;
using Sauna.Event;

[Icon( "highlight_alt" )]
[Category( "Events" )]
public sealed class EventAreaTrigger : EventTrigger
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

	public BBox BBox => new BBox( Offset - Extents / 2f, Offset + Extents / 2f );
	public BBox WorldBBox => BBox.Transform( GameObject.Transform.World );

	public override bool IsPolled { get; set; } = true;

	public override void PolledMethod()
	{
		var find = Scene.FindInPhysics( WorldBBox )
			.Where( x => x.Tags.HasAny( TagSet ) );

		foreach ( var found in find )
			if ( !ObjectsInside.Contains( found ) ) // Has entered just now
				CallTrigger( found );

		ObjectsInside = find.ToList();
	}

	protected override void DrawGizmos()
	{
		Gizmo.Draw.Color = Color.Cyan.WithAlpha( 0.2f );
		Gizmo.Draw.SolidBox( BBox );
		Gizmo.Draw.Color = Color.White.WithAlpha( 0.3f );
		Gizmo.Draw.LineBBox( BBox );
	}
}
