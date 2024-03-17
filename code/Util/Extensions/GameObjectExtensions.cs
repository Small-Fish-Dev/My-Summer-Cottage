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

	[Icon( "camera" )]
	public static SweetMemory CaptureMemory( this GameObject obj, string caption, string identifier = null, float distance = 100f, int maxTries = 20 )
	{
		var center = obj.GetBounds().Center + obj.Transform.Position + Vector3.Up * 10f;
		var position = center + obj.Transform.Rotation.Backward * distance; // Default
		var rotation = Rotation.FromRoll( Game.Random.Int( -15, 15 ) );

		for ( int i = 0; i < maxTries; i++ )
		{
			var dir = Vector3.Random.Normal;
			dir = dir.WithZ( dir.z.Clamp( 0.3f, 0.9f ) ); // Clamped on z-axis so it doesn't go too low.

			var from = obj.Transform.Position + dir * distance;
			var ray = new Ray( from, (center - from).Normal );

			var tr = obj.Scene.Trace.Ray( ray, distance )
				.WithoutTags( "trigger" )
				.Run();

			if ( tr.GameObject == obj || !tr.Hit )
			{
				position = ray.Position;
				break;
			}
		}

		return SweetMemories.Capture( caption, position, rotation, center, identifier );
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
