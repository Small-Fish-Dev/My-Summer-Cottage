namespace Sauna;

[HammerEntity]
[Solid]
[Text( "Temperature Volume" )]
[AutoApplyMaterial( "materials/tools/toolstrigger.vmat" )]
public partial class TemperatureVolume : ModelEntity
{
	/// <summary>
	/// List of all the players that are inside of this temperature volume.
	/// </summary>
	public List<Player> Players { get; private set; } = new();

	public override void Spawn()
	{
		Tags.Add( "trigger" );

		SetupPhysicsFromModel( PhysicsMotionType.Static );
		EnableAllCollisions = false;
		EnableTouch = true;
	}

	public override void StartTouch( Entity other )
	{
		base.StartTouch( other );

		if ( other is not Player player )
			return;

		if ( !Players.Contains( player ) )
			Players.Add( player );
	}

	public override void EndTouch( Entity other )
	{
		base.StartTouch( other );

		if ( other is not Player player )
			return;

		if ( Players.Contains( player ) )
			Players.Remove( player );
	}

	[Event.Tick]
	private void tick()
	{
		// TODO: Update
	}
}
