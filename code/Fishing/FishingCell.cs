namespace Sauna.Fishing;

public class FishingCell : Component
{
	[Property] public List<PrefabFile> AvailableFish { get; set; }

	private BoxCollider _collider;

	protected override void OnAwake()
	{
		_collider = Components.Get<BoxCollider>();
		if ( _collider is null )
			throw new Exception( "Cannot find a collider" );
	}

	protected override void OnUpdate()
	{
		if ( !Game.IsPlaying )
			return;
		
		using ( Gizmo.Scope() )
		{
			Gizmo.Draw.Color = Color.Green;
			Gizmo.Draw.LineBBox( BBox.FromPositionAndSize( _collider.Center + Transform.Position, _collider.Scale ) );
		}
	}
}
