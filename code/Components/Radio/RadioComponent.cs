namespace Sauna;

public partial class RadioComponent : Component
{
	[Sync]
	public int ChannelIndex
	{
		get => _channelIndex;
		set
		{
			_channelIndex = value;
			StartMusic();
		}
	}

	[Sync, Property]
	public bool On
	{
		get => _on;
		set
		{
			_on = value;

			if ( !Game.IsPlaying || GameObject == Game.ActiveScene )
				return;

			if ( !_on ) // Close the player.
			{
				StopMusic();
				return;
			}

			// Start the player.
			StartMusic();
		}
	}

	public RadioChannel Channel => RadioChannel.All[ChannelIndex];
	public string Title { get; private set; }

	MusicPlayer _player;
	SoundHandle _handle;
	bool _on;
	int _channelIndex;

	// todo: try to thread spectrum shit
	public ReadOnlySpan<float> GetSpectrum() => ReadOnlySpan<float>.Empty; // _player != null ? _player.Spectrum :

	protected override void OnStart()
	{
		var interactions = Components.GetOrCreate<Interactions>();

		// Toggle
		interactions.AddInteraction( new Interaction()
		{
			//Identifier = "radio.toggle",
			Action = ( Player interactor, GameObject obj ) => On = !On,
			Keybind = "use2",
			DynamicText = () => $"Toggle {(On ? "off" : "on")}",
		} );

		// Next channel
		interactions.AddInteraction( new Interaction()
		{
			//Identifier = "radio.next",
			Action = ( Player interactor, GameObject obj ) =>
			{
				var next = ChannelIndex + 1;
				ChannelIndex = next >= RadioChannel.All.Length
					? 0
					: next;
			},
			Keybind = "next",
			Description = "Next channel",
			Disabled = () => !On
		} );

		// Previous channel
		interactions.AddInteraction( new Interaction()
		{
			//Identifier = "radio.previous",
			Action = ( Player interactor, GameObject obj ) =>
			{
				var previous = ChannelIndex - 1;
				ChannelIndex = previous < 0
					? RadioChannel.All.Length - 1
					: previous;
			},
			Keybind = "previous",
			Description = "Previous channel",
			Disabled = () => !On
		} );
	}

	private void StartMusic()
	{
		StopMusic();

		if ( !Channel.Local )
		{
			_player = MusicPlayer.PlayUrl( Channel.URL );
			_player.Repeat = true;
			_player.ListenLocal = false;
			_player.Volume = 0.05f;
		}
		else
		{
			_handle = Sound.Play( Channel.URL );
			_handle.ListenLocal = false;
			_handle.Volume = 0.05f;
			_handle.Decibels = 50;
			_handle.DistanceAttenuation = true;
			_handle.Occlusion = false;
		}

		Title = _player != null && !string.IsNullOrWhiteSpace( _player.Title )
				? $"{Channel.Name} - {_player.Title}"
				: Channel.Name;

		Title = Title.RemoveDiacritics();
	}

	private void StopMusic()
	{
		_handle?.Stop();
		_handle = null;

		_player?.Stop();
		_player = null;
	}

	protected override void OnDestroy()
	{
		StopMusic();
	}

	protected override void OnDisabled()
	{
		StopMusic();
	}

	protected override void OnUpdate()
	{
		if ( _player != null )
			_player.Position = Transform.Position;
		else if ( _handle != null )
			_handle.Position = Transform.Position;
	}
}
