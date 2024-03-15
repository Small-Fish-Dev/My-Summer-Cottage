namespace Sauna;

public static class GameObjectExtensions
{
	public static void SetupNetworking(
		this GameObject obj,
		OwnerTransfer transfer = OwnerTransfer.Takeover,
		NetworkOrphaned orphaned = NetworkOrphaned.ClearOwner )
	{
		obj.NetworkMode = NetworkMode.Object;

		if ( !obj.Network.Active )
			obj.NetworkSpawn();

		obj.Network.SetOwnerTransfer( transfer );
		obj.Network.SetOrphanedMode( orphaned );
	}

	public static IEnumerable<Interaction> GetInteractions( this GameObject obj )
	{
		return obj.Components.Get<Interactions>( FindMode.EverythingInSelf )?.AllInteractions;
	}

	[Title( "Speak with Subtitle" ), Group( "Audio" ), Icon( "volume_up" )]
	public static Speech SpeakWithSubtitle( this GameObject obj, string name, string subtitle, SpeechSettings? settings = null )
	{
		var soundHandler = obj.Components.GetOrCreate<SoundHandler>();
		return soundHandler.SpeakWithSubtitle( name, subtitle, settings );
	}

	[Title( "Play Sound (Custom)" ), Group( "Audio" ), Icon( "volume_up" )]
	public static SoundHandle PlaySound( this GameObject obj, string sound )
	{
		var soundHandler = obj.Components.GetOrCreate<SoundHandler>();
		return soundHandler.PlaySound( sound );
	}

	[Title( "Play Sound With Subtitles (Custom)" ), Group( "Audio" ), Icon( "volume_up" )]
	public static SoundHandle PlaySoundWithSubtitle( this GameObject obj, string sound = "", string subtitle = "" )
	{
		var soundHandler = obj.Components.GetOrCreate<SoundHandler>();
		return soundHandler.PlaySoundWithSubtitle( sound, string.Empty, subtitle );
	}

	[Title( "Play Sound With Name and Subtitle (Custom)" ), Group( "Audio" ), Icon( "volume_up" )]
	public static SoundHandle PlaySoundWithNameAndSubtitle( this GameObject obj, string sound = "", string name = "", string subtitle = "" )
	{
		var soundHandler = obj.Components.GetOrCreate<SoundHandler>();
		return soundHandler.PlaySoundWithSubtitle( sound, name, subtitle );
	}

	[Title( "Play Sound (Custom)" ), Group( "Audio" ), Icon( "volume_up" )]
	public static SoundHandle PlaySound( this GameObject obj, SoundEvent sound )
	{
		var soundHandler = obj.Components.GetOrCreate<SoundHandler>();
		return soundHandler.PlaySound( sound );
	}

	[Title( "Play Sound With Subtitles (Custom)" ), Group( "Audio" ), Icon( "volume_up" )]
	public static SoundHandle PlaySoundWithSubtitle( this GameObject obj, SoundEvent sound = null, string subtitle = "" )
	{
		var soundHandler = obj.Components.GetOrCreate<SoundHandler>();
		return soundHandler.PlaySoundWithSubtitle( sound, string.Empty, subtitle );
	}

	[Title( "Play Sound With Name and Subtitle (Custom)" ), Group( "Audio" ), Icon( "volume_up" )]
	public static SoundHandle PlaySoundWithNameAndSubtitle( this GameObject obj, SoundEvent sound = null, string name = "", string subtitle = "" )
	{
		var soundHandler = obj.Components.GetOrCreate<SoundHandler>();
		return soundHandler.PlaySoundWithSubtitle( sound, name, subtitle );
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
