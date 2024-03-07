namespace Sauna;

public static class GameObjectExtensions
{
	public static IEnumerable<Interaction> GetInteractions( this GameObject obj )
	{
		return obj.Components.Get<Interactions>( FindMode.EverythingInSelf )?.AllInteractions;
	}

	[Title( "Play Sound (Custom)" ), Group( "Audio" ), Icon( "volume_up" )]
	public static SoundHandle PlaySound( this GameObject obj, string sound )
	{
		var soundHandler = obj.Components.GetOrCreate<SoundHandler>();
		return soundHandler.PlaySound( sound );
	}

	[Title( "Play Sound (Custom)" ), Group( "Audio" ), Icon( "volume_up" )]
	public static SoundHandle PlaySound( this GameObject obj, SoundEvent sound )
	{
		var soundHandler = obj.Components.GetOrCreate<SoundHandler>();
		return soundHandler.PlaySound( sound );
	}

	[Title( "Stop Sound (Custom)" ), Group( "Audio" ), Icon( "volume_up" )]
	public static void StopSound( this GameObject obj, string sound )
	{
		var soundHandler = obj.Components.GetOrCreate<SoundHandler>();
		soundHandler.StopSound( sound );
	}

	[Title( "Stop Sound (Custom)" ), Group( "Audio" ), Icon( "volume_up" )]
	public static void StopSound( this GameObject obj, SoundEvent sound )
	{
		var soundHandler = obj.Components.GetOrCreate<SoundHandler>();
		soundHandler.StopSound( sound );
	}
}
