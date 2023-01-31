namespace Sauna;

[HammerEntity]
[Solid]
public partial class SaunaWater : ModelEntity
{
	/// <summary>
	/// Gets the wave offset, same math as shader.
	/// </summary>
	/// <param name="position"></param>
	/// <returns></returns>
	public float WaveOffset( Vector3 position )
		=> 4f * MathF.Sin( (Position.x - position.x) * 10f + Time.Now );

	public override void Spawn()
	{
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
		Tags.Add( "trigger" );
	}

	public override void StartTouch( Entity other )
	{
		base.StartTouch( other );
		Log.Info( other );
		if ( other is not Player player ) return;
		//player.Water = this;
	}
}
