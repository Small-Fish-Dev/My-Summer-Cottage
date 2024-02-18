namespace Sauna;

public class SoundSystem009 : GameObjectSystem
{
	public class SubtitlePopup
	{
		public SubtitlePopup(SoundWithSubtitlesResource resource, GameObject source, SoundHandle soundHandle)
		{
			Resource = resource;
			Source = source;
			SoundHandle = soundHandle;
		}

		public SoundWithSubtitlesResource Resource { get; init; }
		public GameObject Source { get; init; }
		public SoundHandle SoundHandle { get; init; }
	}

	public static SoundSystem009 The => _the.Target as SoundSystem009;

	private static WeakReference _the;

	[Property] public uint MaxSubtitlesOnScreen = 3;

	/// <summary>
	/// Time in seconds before the subtitle disappears after the sound is done playing
	/// </summary>
	[Property] public float SubtitleDelay;

	public List<SubtitlePopup> SubtitleSounds { get; private set; } = new();

	public event Action<SubtitlePopup> OnSoundPlayed;

	public SoundSystem009( Scene scene ) : base( scene )
	{
		_the = new WeakReference( this );

		Listen( Stage.FinishUpdate, 0, UpdateSounds, "UpdateSounds" );
	}

	private void UpdateSounds()
	{
		// Log.Info( $"{SubtitleSounds.Count}" );
		foreach ( var s in SubtitleSounds )
		{
			Log.Info( $"test {s}" );
		}
		// var validSounds = Sounds.Where( sound => sound.SoundHandle.IsValid() ).Where( sound => sound.Source != null );
		// foreach (var sound in validSounds)
		// {
		// 	Log.Info( $"{sound}" );
		// 	if ( !sound.Source.IsValid() )
		// 		// Stop the sound if the speaker no longer exists
		// 		sound.SoundHandle.Stop();
		// 	else
		// 		sound.SoundHandle.Position = sound.Source.Transform.Position;
		// }

		// Sounds.RemoveAll( s => !s.SoundHandle.IsValid() || s.SoundHandle.IsStopped );
	}

	// TODO: Networking!
	public void Play( SoundWithSubtitlesResource sound, GameObject source )
	{
		SoundHandle soundHandle;
		if ( !source.IsValid() || source == Player.Local.GameObject )
			// TODO: figure out a proper way to force the sound to be played in 2D mode
			soundHandle = Sound.Play( sound.SoundEvent );
		else
			soundHandle = Sound.Play( sound.SoundEvent, source.Transform.Position );
		
		var subtitlePopup = new SubtitlePopup( sound, source, soundHandle);
		SubtitleSounds.Add( subtitlePopup );
		OnSoundPlayed?.Invoke( subtitlePopup );
		Log.Info( $"{SubtitleSounds.Count} {source?.Transform.Position}" );
	}

	[ConCmd( "test_play_sound" )]
	public static void TestPlaySound( string path )
	{
		var soundResource = ResourceLibrary.Get<SoundWithSubtitlesResource>( path );
		if ( soundResource is null )
			throw new Exception( $"Cannot find a sound with subtitles @ {path}" );

		The.Play( soundResource, Player.Local.GameObject );
	}
}
