using Sauna.UI;

namespace Sauna;

public class SoundHandler : Component
{
	private readonly List<SoundHandle> _activeSounds = new();

	public SoundHandle PlaySound( string sound )
	{
		var soundHandle = Sound.Play( sound );
		_activeSounds.Add( soundHandle );
		return soundHandle;
	}

	public SoundHandle PlaySoundWithSubtitle( string sound, string name, string subtitle )
	{
		Subtitles.AddSubtitle( name, subtitle, null );
		return PlaySound( sound );
	}

	public Speech SpeakWithSubtitle( string name, string subtitle, SpeechSettings? settings = null )
	{
		Subtitles.AddSubtitle( name, subtitle, null );
		return Speech.Create( subtitle, (settings ?? SpeechSettings.Default) with { GameObject = GameObject } );
	}

	public SoundHandle PlaySound( SoundEvent sound )
	{
		var soundHandle = Sound.Play( sound );
		_activeSounds.Add( soundHandle );
		return soundHandle;
	}

	public SoundHandle PlaySoundWithSubtitle( SoundEvent sound, string name, string subtitle )
	{
		var soundHandle = PlaySound( sound );
		Subtitles.AddSubtitle( name, subtitle, soundHandle );
		return soundHandle;
	}

	public void StopSound( string sound )
	{
		foreach ( var activeSound in _activeSounds )
		{
			if ( activeSound.Name.ToLower().Contains( sound.ToLower() ) )
				activeSound.Stop();
		}
	}

	public void StopSound( SoundEvent soundEvent )
	{
		StopSound( soundEvent.ResourceName );
	}

	protected override void OnUpdate()
	{
		foreach ( var sound in _activeSounds.ToList() )
		{
			if ( sound is null || sound.IsStopped )
			{
				_activeSounds.Remove( sound );
				continue;
			}

			sound.Position = GameObject.Transform.Position;
			sound.Rotation = GameObject.Transform.Rotation;
		}
	}

	protected override void OnDestroy()
	{
		foreach ( var sound in _activeSounds.ToList() )
		{
			sound.Stop();
		}
	}
}
