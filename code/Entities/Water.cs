namespace Sauna;

[HammerEntity]
public partial class SaunaWater : BaseTrigger
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
		SetupPhysicsFromModel( PhysicsMotionType.Static );
	}

	public override void StartTouch( Entity other )
	{
		base.StartTouch( other );

		if ( other is not Player player ) return;
		player.Water = this;
	}

	public override void OnTouchEnd( Entity other )
	{
		base.OnTouchEnd( other );

		if ( other is not Player player ) return;
		player.Water = null;
	}
}
