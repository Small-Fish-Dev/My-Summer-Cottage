namespace Sauna;

[HammerEntity]
[Model]
public partial class Radio : ModelEntity, IInteractable
{
	public struct Song
	{
		public string Producer;
		public string Name;
		public string Path;
		public float Duration;

		public Song( string path )
		{
			Path = path;

			// Get song duration from QOA header.
			var stream = FileSystem.Mounted.OpenRead( path );
			var buffer = new byte[64];
			stream.Read( buffer, 0, buffer.Length );
			stream.Close();

			var decoder = new QOA.Decoder( buffer );
			Duration = (float)decoder.SampleCount / decoder.SampleRate;
		}
	}

	public Song? CurrentSong { get; private set; }
	public TimeSince? ElapsedTime { get; private set; }
	public float StartTime { get; private set; }

	InteractionOffset IInteractable.Offset => Vector3.Up * 10f;
	string IInteractable.DisplayTitle => "Mankka";

	private static string[] songFromPath( string path )
		=> path
			.Substring( 0, path.Length - 5 )
			.Replace( '_', ' ' )
			.Split( "-" );

	List<Song> sounds = FileSystem.Mounted.FindFile( "sounds/qoa/" )
		.Where( file => file.EndsWith( ".json" ) ) // This .qoa file is on an undercover mission to infiltrate JSON headquarters.
		.Select( path =>
		{
			var name = path.Substring( 0, path.Length - 5 );
			var separate = songFromPath( path );
			var fullPath = $"sounds/qoa/{name}.json";

			// Return all of the song data.
			return new Song( fullPath )
			{
				Producer = separate[0],
				Name = separate[1]
			};
		} )
		.ToList();

	private Sound? sound;
	private SoundStream stream;

	private TimeSince lastWritten;
	private QOA.Decoder decoder;

	private RadioDisplay display;

	public Radio()
	{
		var interactable = this as IInteractable;

		// Turn radio on.
		interactable.AddInteraction( InputButton.Use, new()
		{
			Predicate = ( Player pawn ) => true,
			Function = ( Player pawn ) =>
			{
				if ( Game.IsClient ) 
					return;
				
				if ( ElapsedTime != null )
				{
					Stop();
					return;
				}

				var random = sounds[Game.Random.Int( sounds.Count - 1 )];
				Play( song: random );
			},
			TextFunction = () => CurrentSong != null ? "Turn off" : "Turn on"
		} );

		// Play random song.
		interactable.AddInteraction( InputButton.Reload, new()
		{
			Predicate = ( Player pawn ) => CurrentSong != null,
			Function = ( Player pawn ) =>
			{
				if ( Game.IsClient ) 
					return;

				var array = sounds.Where( song => song.Path != CurrentSong?.Path )
					.ToArray();
				var random = array[Game.Random.Int( array.Length - 1 )];
				Play( song: random );
			},
			Text = "Play random"
		} );
	}

	public override void Spawn()
	{
		SetModel( "models/radio/radio.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
	}

	/// <summary>
	/// Stops the current song.
	/// </summary>
	public void Stop()
	{
		Game.AssertServer();

		ElapsedTime = null;
		CurrentSong = null;

		// Stop playing song on client.
		playOnClient( To.Everyone, -1, 0f );
	}

	/// <summary>
	/// Play a song.
	/// </summary>
	/// <param name="target"></param>
	/// <param name="song"></param>
	/// <param name="skipTime"></param>
	public void Play( To? target = null, Song? song = null, float skipTime = 0f )
	{
		Game.AssertServer();

		if ( song != null )
		{
			ElapsedTime = skipTime;
			CurrentSong = song;
			StartTime = Time.Now - skipTime;
		}

		// Load file on client and start playing at desired time.
		playOnClient( target ?? To.Everyone, sounds.IndexOf( CurrentSong.Value ), StartTime );
	}

	[ClientRpc]
	private async void playOnClient( int index, float startTime )
	{
		sound?.Stop();
		stream?.Delete();
		CurrentSong = null;
		ElapsedTime = null;

		if ( index > sounds.Count - 1 || index < 0 ) 
			return;

		var song = sounds.ElementAtOrDefault( index );
		CurrentSong = song;

		decoder = new QOA.Decoder( await FileSystem.Mounted.ReadAllBytesAsync( song.Path ) );
		if ( !decoder.Valid )
		{
			Log.Error( "QOA Decoder has an invalid haeder." );
			return;
		}
		
		sound = Sound.FromEntity( "audiostream.default", this );
		stream = sound?.CreateStream( decoder.SampleRate, decoder.Channels );

		ElapsedTime = MathF.Max( Time.Now - startTime, 0f );
		decoder.SeekToSample( (int)(decoder.SampleRate * ElapsedTime) );
	}

	protected override void OnDestroy()
	{
		if ( !Game.IsClient )
			return;

		sound?.Stop();
		stream?.Delete();
	}

	[Event.Tick]
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

		if ( stream == null || sound == null || !stream.IsValid() || decoder == null )
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
	}

	[Event( "OnSpawn" )]
	static void onSpawn( Player player )
	{
		foreach ( var radio in Entity.All.OfType<Radio>() )
			if ( radio.CurrentSong != null )
				radio.Play( To.Single( player.Client ) );
	}
}
