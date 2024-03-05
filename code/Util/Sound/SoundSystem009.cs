using Sauna.Util.Extensions;

namespace Sauna;

public class SoundSystem009 : GameObjectSystem
{
	public record SubtitlePopup( SoundWithSubtitlesResource Resource, GameObject Source, SoundHandle SoundHandle );

	/// <summary>
	/// Time in seconds before the subtitle disappears after the sound is done playing
	/// </summary>
	[Property] public float SubtitleDelay;

	// TODO: Networking!
	// TODO: Do not add a sound if it's played far away from the listener
	public List<SubtitlePopup> Sounds { get; } = new();

	public event Action<SubtitlePopup> OnSoundPlayed;
	public event Action<SubtitlePopup> OnSoundStopped;

	public SoundSystem009( Scene scene ) : base( scene )
	{
		Listen( Stage.FinishUpdate, 0, UpdateSounds, "UpdateSounds" );
	}

	private void UpdateSounds()
	{
		if ( !Game.IsPlaying )
			return;
		
		var validSounds = Sounds.Where( sound => sound.SoundHandle.IsValid() ).Where( sound => sound.Source != null );
		foreach (var sound in validSounds)
		{
			if ( !sound.Source.IsValid() )
				// Stop the sound if the speaker no longer exists
				sound.SoundHandle.Stop();
			else
				sound.SoundHandle.Position = sound.Source.Transform.Position;
		}

		for ( var i = 0; i < Sounds.Count; i++ )
		{
			var s = Sounds[i];
			if ( !s.SoundHandle.IsValid() || s.SoundHandle.IsStopped )
			{
				Sounds.RemoveAt( i-- );
				
				OnSoundStopped?.Invoke( s );
			}
		}
	}

	public void Play( SoundWithSubtitlesResource sound, GameObject source )
	{
		if ( source.IsValid() && source.Transform.Position.Distance( Player.Local.GameObject.Transform.Position ) >
		    sound.MaxDistance )
			// The sound was played too far away
			return;
		
		SoundHandle soundHandle;
		if ( !source.IsValid() || source == Player.Local.GameObject )
			// TODO: figure out a proper way to force the sound to be played in 2D mode
			soundHandle = Sound.Play( sound.SoundEvent );
		else
			soundHandle = Sound.Play( sound.SoundEvent, source.Transform.Position );
		
		var subtitlePopup = new SubtitlePopup( sound, source, soundHandle);
		Sounds.Add( subtitlePopup );
		OnSoundPlayed?.Invoke( subtitlePopup );
		Log.Info( $"{Sounds.Count} {source?.Transform.Position}" );
	}

	[ConCmd( "test_play_sound" )]
	public static void TestPlaySound( string path )
	{
		var soundResource = ResourceLibrary.Get<SoundWithSubtitlesResource>( path );
		if ( soundResource is null )
			throw new Exception( $"Cannot find a sound with subtitles @ {path}" );

		Game.ActiveScene.SoundSystem().Play( soundResource, Player.Local.GameObject );
	}
}
