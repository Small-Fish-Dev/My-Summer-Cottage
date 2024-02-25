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

			if ( !_on ) // Close the player.
			{
				_player?.Stop();
				_player = null;

				return;
			}

			// Start the player.
			StartMusic();
		}
	}

	public RadioChannel Channel => RadioChannel.All[ChannelIndex];
	public string Title => _player != null && !string.IsNullOrWhiteSpace( _player.Title )
		? $"{Channel.Name} - {_player.Title}"
		: Channel.Name;

	MusicPlayer _player;
	bool _on;
	int _channelIndex;

	public ReadOnlySpan<float> GetSpectrum() => _player != null ? _player.Spectrum : ReadOnlySpan<float>.Empty;

	protected override void OnStart()
	{
		var interactions = Components.Create<Interactions>();

		// Toggle
		interactions.AddInteraction( new Interaction()
		{
			Identifier = "radio.toggle",
			Action = ( Player interactor, GameObject obj ) => On = !On,
			Keybind = "use",
			DynamicText = () => $"Toggle {(On ? "off" : "on")}",
		} );

		// Next channel
		interactions.AddInteraction( new Interaction()
		{
			Identifier = "radio.next",
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
			Identifier = "radio.previous",
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
		_player?.Stop();
		_player = MusicPlayer.PlayUrl( Channel.URL );
		_player.Repeat = true;
		_player.ListenLocal = false;
		_player.Volume = 0.05f;
	}

	protected override void OnDestroy()
	{
		_player?.Stop();
		_player = null;
	}

	protected override void OnUpdate()
	{
		if ( _player == null )
			return;

		_player.Position = Transform.Position;
	}
}
