using Sauna;
using Sauna.Event;

[Icon( "highlight_alt" )]
[Category( "Events" )]
public sealed class EventSellAreaTrigger : EventTrigger
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
		var items = Scene.FindInPhysics( WorldBBox )
			?.Select( x => x.Components.Get<ItemComponent>() )
			.Where( item => item != null && item.IsSellable && item.LastParent != null )
			.ToList();

		foreach ( var item in items )
		{
			CallTrigger( item.GameObject );
			item.LastParent.Money += item.SellPrice;
			item.GameObject.Destroy();
		}
	}

	protected override void DrawGizmos()
	{
		if ( !Gizmo.IsSelected ) return;

		Gizmo.Draw.Color = Color.Cyan.WithAlpha( 0.4f );
		Gizmo.Draw.SolidBox( BBox );
		Gizmo.Draw.Color = Color.White.WithAlpha( 0.3f );
		Gizmo.Draw.LineBBox( BBox );
	}
}
