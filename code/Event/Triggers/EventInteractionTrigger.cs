using Sandbox;
using Sauna;
using Sauna.Event;

[Icon( "touch_app" )]
[Category( "Events" )]
public sealed class EventInteractionTrigger : EventTrigger
{
	/// <summary>
	/// Which interaction identifier triggers this
	/// </summary>
	[Property]
	public string InteractionIdentifier { get; set; }

	/// <summary>
	/// Only check for interactions that happened inside of an area
	/// </summary>
	[Property]
	public bool InsideArea { get; set; } = false;

	[Property]
	[HideIf( "InsideArea", false )]
	public Vector3 Offset { get; set; }

	[Property]
	[HideIf( "InsideArea", false )]
	public Vector3 Extents { get; set; }

	public BBox BBox => new BBox( Offset - Extents / 2f, Offset + Extents / 2f );
	public BBox WorldBBox => BBox.Transform( GameObject.Transform.World );



	protected override void DrawGizmos()
	{
		if ( InsideArea )
		{
			Gizmo.Draw.Color = Color.Green.WithAlpha( 0.3f );
			Gizmo.Draw.SolidBox( BBox );
			Gizmo.Draw.Color = Color.White.WithAlpha( 0.3f );
			Gizmo.Draw.LineBBox( BBox );
		}
	}
}
