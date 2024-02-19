using Sauna.Util.Extensions;

namespace Sauna.Components;

public sealed class RandomYell : Component
{
	/*[Property]*/ public List<SoundWithSubtitlesResource> VoiceLines = new()
	{
		// TODO: the inspector does not support the resources?
		ResourceLibrary.Get<SoundWithSubtitlesResource>( "sounds/speech/test_drunk.sws" ),
		ResourceLibrary.Get<SoundWithSubtitlesResource>( "sounds/door/door_creak.sws" )
	};
	/*[Property]*/ public RangedFloat Period = new(5, 7);

	private TimeUntil _speak;

	protected override void OnAwake()
	{
		base.OnAwake();

		_speak = Period.GetValue();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( _speak && VoiceLines.Count > 0 )
		{
			Scene.SoundSystem().Play( Random.Shared.FromList( VoiceLines ), GameObject );
			_speak = Period.GetValue();
		}
	}
}
