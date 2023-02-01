namespace Sauna;

[HammerEntity]
[Solid]
public partial class SaunaWater : ModelEntity
{
	public override void Spawn()
	{
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
		Tags.Add( "trigger" );
	}

	public override void StartTouch( Entity other )
	{
		base.StartTouch( other );

		if ( other is not Player player ) return;
		player.Water = this;
	}

	public override void EndTouch( Entity other )
	{
		base.StartTouch( other );

		if ( other is not Player player ) return;
		player.Water = null;
	}
}
