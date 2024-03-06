﻿namespace Sauna.Fishing;

public class Bobber : Component
{
	public FishingCell CurrentCell { get; set; }
	public FishingRod Rod { get; set; }
	public Rigidbody Rigidbody { get; private set; }

	private SphereCollider _collider;

	protected override void OnAwake()
	{
		Rigidbody = Components.Get<Rigidbody>();
		_collider = Components.Get<SphereCollider>();
	}

	protected override void OnFixedUpdate()
	{
		if ( !CurrentCell.IsValid() || !CurrentCell.Collider.Touching.Contains( _collider ) )
		{
			CurrentCell = null;
		}
		else
		{
			using ( Gizmo.Scope() )
			{
				Gizmo.Draw.IgnoreDepth = true;
				Gizmo.Draw.Color = Color.Blue;
				Gizmo.Draw.LineBBox( BBox.FromPositionAndSize( CurrentCell.Collider.Center + CurrentCell.Transform.Position, CurrentCell.Collider.Scale ) );
			}
		}
	}

	public void PullOut()
	{
		if ( CurrentCell.IsValid() )
			CurrentCell.PullOutFish( GameObject );
	}
}