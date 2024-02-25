using Sandbox;
using Sauna;
using Sauna.Event;

[Icon( "water_drop" )]
[Category( "Events" )]
public sealed class EventPissTrigger : EventTrigger
{
	[Property]
	public Vector3 Offset { get; set; }

	[Property]
	public Vector3 Extents { get; set; }

	public BBox BBox => new BBox( Offset - Extents / 2f, Offset + Extents / 2f );
	public BBox WorldBBox => BBox.Transform( GameObject.Transform.World );

	public override bool IsPolled { get; set; } = true;

	public override void PolledMethod()
	{
		var pissingPlayers = Scene.GetAllComponents<Player>();

		foreach ( var player in pissingPlayers )
		{
			if ( player.LastPiss <= Time.Delta * MaxPollingRate )
			{
				if ( WorldBBox.Contains( player.PissingPosition ) )
				{
					CallTrigger( player.GameObject );
				}
			}
		}
	}

	public override void Clear()
	{

	}

	protected override void DrawGizmos()
	{
		Gizmo.Draw.Color = Color.Yellow.WithAlpha( 0.3f );
		Gizmo.Draw.SolidBox( BBox );
		Gizmo.Draw.Color = Color.White.WithAlpha( 0.3f );
		Gizmo.Draw.LineBBox( BBox );
	}
}
