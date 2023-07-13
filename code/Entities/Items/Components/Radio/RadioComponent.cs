namespace Sauna;

[Prefab, Title( "Radio Component" )]
public partial class RadioComponent : ItemComponent
{
	public RadioChannelAsset Channel { get; private set; }
	public float Time { get; private set; }
	public TimeSince? ElapsedTime { get; private set; }

	private MusicPlayer player;
	private RadioDisplay display;

	public string Title => player != null
		? player.Title
		: Channel?.Name ?? string.Empty;

	public float? Duration => player != null 
		? player.Duration 
		: null;

	public override void Initialize()
	{
		if ( Game.IsServer )
			Item.Transmit = TransmitType.Always;

		// Toggle the radio.
		Interactable.AddInteraction( "use", new()
		{
			Predicate = ( Player pawn ) => true,
			Function = ( Player pawn ) =>
			{
				if ( Game.IsClient )
					return;

				if ( Channel != null )
				{
					Stop();
					return;
				}

				var first = RadioChannelAsset.All.FirstOrDefault();
				Play( channel: first );
			},
			TextFunction = () => Channel != null
				? "Turn off"
				: "Turn on",
		} );

		// Switch Channel
		Interactable.AddInteraction( "reload", new()
		{
			Predicate = ( Player pawn ) => Channel != null,
			Function = ( Player pawn ) =>
			{
				if ( Game.IsClient )
					return;

				var index = ((List<RadioChannelAsset>)RadioChannelAsset.All).IndexOf( Channel ) + 1;
				var next = RadioChannelAsset.All[index % RadioChannelAsset.All.Count];
				Play( channel: next );
			},
			Text = "Next"
		} );

		Interactable.AddInteraction( "menu", new()
		{
			Predicate = ( Player pawn ) => Channel != null,
			Function = ( Player pawn ) =>
			{
				if ( Game.IsClient )
					return;

				var index = ((List<RadioChannelAsset>)RadioChannelAsset.All).IndexOf( Channel ) - 1;
				var previous = RadioChannelAsset.All[index < 0 ? RadioChannelAsset.All.Count - 1 : index];
				Play( channel: previous );
			},
			Text = "Previous"
		} );

		if ( Game.IsClient )
			display = new( this );
	}

	/// <summary>
	/// Stops the current song.
	/// </summary>
	public void Stop()
	{
		Game.AssertServer();

		Channel = null;
		ElapsedTime = null;

		// Stop playing song on client.
		playOnClient( To.Everyone, string.Empty );
	}

	/// <summary>
	/// Play a song.
	/// </summary>
	/// <param name="target"></param>
	/// <param name="channel"></param>
	/// <param name="skipTime"></param>
	public void Play( To? target = null, RadioChannelAsset channel = null )
	{
		Game.AssertServer();

		Channel = channel;
		/*if ( Channel != null )
			Time = Sandbox.Time.Now - skipTime;*/
		
		// Load file on client and start playing at desired time.
		playOnClient( target ?? To.Everyone, Channel?.Name ?? string.Empty );
	}

	[ClientRpc]
	private async void playOnClient( string name)
	{
		player?.Stop();
		player?.Dispose();

		ElapsedTime = null;

		Channel = name == string.Empty
			? null 
			: RadioChannelAsset.All
				.FirstOrDefault( ch => ch.Name == name );

		if ( Channel == null )
			return;

		player = MusicPlayer.PlayUrl( Channel.URL );
		player.Entity = Entity;
		player.Volume = 0.2f;
		// player.Seek( startTime );

		ElapsedTime = 0f;
	}

	public override void OnDestroy()
	{
		if ( !Game.IsClient )
			return;

		player?.Stop();
		player?.Dispose();
		display?.Delete( true );
	}

	/*[GameEvent.Tick]
	void tick()
	{
		// Pick a new random song.
		if ( Game.IsServer && ElapsedTime > CurrentSong?.Duration + 1f )
		{
			var random = sounds[Game.Random.Int( sounds.Count - 1 )];
			Play( song: random );

			return;
		}

		if ( Game.IsClient && display == null )
			display = new( this );

		if ( stream == null || !stream.IsValid() || decoder == null )
			return;

		// Handle reading the samples and writing them to the SoundStream.
		var delay = (float)stream.MaxWriteSampleCount / decoder.SampleRate / 2f; // Give room for sample writes.
		if ( lastWritten < delay || stream.QueuedSampleCount > 0 )
			return;

		var buffer = new short[QOA.Base.MaxFrameSamples];
		var read = decoder.ReadSamples( buffer );

		if ( read == -1 )
		{
			stream.Delete();
			stream = null;

			return;
		}

		stream.WriteData( buffer.AsSpan() );
		lastWritten = 0f;
	}*/

	[SaunaEvent.OnSpawn]
	static void onSpawn( Player player )
	{
		foreach ( var radio in GetAllOfType<RadioComponent>() )
			if ( radio.Channel != null )
				radio.Play( To.Single( player.Client ) );
	}
}
