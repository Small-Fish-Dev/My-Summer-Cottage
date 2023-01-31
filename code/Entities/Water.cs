namespace Sauna;

[HammerEntity]
public partial class Water : BaseTrigger
{

	public Water() { }

	public override void Spawn()
	{
		Log.Info( "hiii" );
	}

	public override void StartTouch( Entity other )
	{
		base.StartTouch( other );
		Log.Info( other );
		if ( other is not Player player ) return;
		player.Swimming = true;
	}

	public override void OnTouchEnd( Entity toucher )
	{
		base.OnTouchEnd( toucher );

		if ( toucher is not Player player ) return;
		player.Swimming = false;
	}
}
