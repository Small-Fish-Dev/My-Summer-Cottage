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

	public SoundHandle PlaySound( SoundEvent sound )
	{
		var soundHandle = Sound.Play( sound );
		_activeSounds.Add( soundHandle );
		return soundHandle;
	}

	public void PlaySoundWithSubtitle( string sound, string subtitle )
	{
		// TODO:
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
			if ( sound.IsStopped )
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
