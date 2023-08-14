namespace Sauna;

[Prefab, Title( "Radio Component" )]
public partial class RadioComponent : ItemComponent
{
	public const float INTERACT_COOLDOWN = 0.5f;

	public RadioChannelAsset Channel { get; private set; }
	public float Time { get; private set; }
	public TimeSince? ElapsedTime { get; private set; }

	private MusicPlayer player;
	private RadioDisplay display;

	public string Title => (player != null
		? player.Title == string.Empty ? Channel?.Name : player.Title
		: Channel?.Name) ?? string.Empty;

	public float? Duration => player != null 
		? player.Duration 
		: null;

	[Net] 
	public TimeSince LastInteracted { get; set; }

	public override void Initialize()
	{
		if ( Game.IsServer )
			Item.Transmit = TransmitType.Always;
		
		// Toggle the radio.
		Interactable.AddInteraction( "use", new()
		{
			Predicate = ( Player pawn ) => LastInteracted >= INTERACT_COOLDOWN,
			Function = ( Player pawn ) =>
			{
				if ( Game.IsClient )
					return;

				LastInteracted = 0f;

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
			Predicate = ( Player pawn ) => Channel != null && LastInteracted >= INTERACT_COOLDOWN,
			Function = ( Player pawn ) =>
			{
				if ( Game.IsClient )
					return;

				LastInteracted = 0f;

				var index = ((List<RadioChannelAsset>)RadioChannelAsset.All).IndexOf( Channel ) + 1;
				var next = RadioChannelAsset.All[index % RadioChannelAsset.All.Count];
				Play( channel: next );
			},
			Text = "Next"
		} );

		Interactable.AddInteraction( "menu", new()
		{
			Predicate = ( Player pawn ) => Channel != null && LastInteracted >= INTERACT_COOLDOWN,
			Function = ( Player pawn ) =>
			{
				if ( Game.IsClient )
					return;

				LastInteracted = 0f;

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
	public void Play( To? target = null, RadioChannelAsset channel = null )
	{
		Game.AssertServer();

		Channel = channel;

		// Tell our client to play the music!
		playOnClient( target ?? To.Everyone, Channel?.Name ?? string.Empty );
	}

	[ClientRpc]
	private void playOnClient( string name )
	{
		player?.Stop();
		player?.Dispose();
		player = null;

		ElapsedTime = null;

		Channel = name == string.Empty
			? null 
			: RadioChannelAsset.All
				.FirstOrDefault( ch => ch.Name == name );

		if ( Channel == null )
			return;

		player = MusicPlayer.PlayUrl( Channel.URL );
		player.Entity = Entity;
		player.Volume = 0.8f;
		player.DistanceMax = 500f;
		player.DistanceMin = 200f;
		player.Repeat = true;

		ElapsedTime = 0f;
	}

	public override void OnDestroy()
	{
		player?.Dispose();
		player = null;

		display?.Delete( true );
	}

	[SaunaEvent.OnSpawn]
	static void onSpawn( Player player )
	{
		foreach ( var radio in GetAllOfType<RadioComponent>() )
			if ( radio.Channel != null )
				radio.Play( To.Single( player.Client ) );
	}
}
