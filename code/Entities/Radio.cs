namespace Sauna;

public partial class Radio : ModelEntity, IInteractable
{
	public struct Song
	{
		public string Producer;
		public string Name;
		public string Path;
		public SoundFile File;

		public float Duration => File.Duration;
	}

	public Song? CurrentSong { get; private set; }
	public TimeSince? ElapsedTime { get; private set; }

	string IInteractable.DisplayTitle => $"Mankka";

	private static string[] songFromPath( string path )
		=> path
			.Substring( 0, path.Length - 6 )
			.Replace( '_', ' ' )
			.Split( "-" );

	List<Song> sounds = FileSystem.Mounted.FindFile( "sounds/music/" )
		.Where( file => file.EndsWith( ".sound" ) )
		.Select( path =>
		{
			var name = path.Substring( 0, path.Length - 6 );
			var separate = songFromPath( path );
			var vsnd = $"sounds/music/{name}.vsnd";

			return new Song 
			{ 
				Producer = separate[0],
				Name = separate[1],
				Path = vsnd,
				File = SoundFile.Load( vsnd )
			};
		} )
		.ToList();

	private Sound? sound;
	private SoundStream stream;

	private TimeSince lastWritten;
	private MemoryStream songStream;

	public Radio()
	{
		var interactable = this as IInteractable;

		interactable.AddInteraction( InputButton.Use, new()
		{
			Predicate = ( Player pawn ) => true,
			Function = ( Player pawn ) =>
			{
				if ( Game.IsClient ) return;
				
				if ( ElapsedTime != null )
				{
					Stop();
					return;
				}

				var random = sounds[Game.Random.Int( sounds.Count - 1 )];
				Play( random );
			},
			Text = "Toggle"
		} );
	}

	public override void Spawn()
	{
		SetModel( "models/arrow.vmdl" );
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
	/// <param name="song"></param>
	/// <param name="target"></param>
	public void Play( Song song, To? target = null )
	{
		Game.AssertServer();

		ElapsedTime = 0f;
		CurrentSong = song;

		// Load file on client and start playing at desired time.
		playOnClient( target ?? To.Everyone, sounds.IndexOf( song ), 0f );
	}

	[ClientRpc]
	private async void playOnClient( int index, float elapsedTime )
	{
		sound?.Stop();
		stream?.Delete();
		CurrentSong = null;

		if ( index > sounds.Count - 1 || index < 0 ) 
			return;

		var song = sounds.ElementAtOrDefault( index );
		CurrentSong = song;
		sound = Sound.FromEntity( "audiostream.default", this );

		var loaded = await song.File.LoadAsync();
		if( !loaded )
		{
			Log.Error( "Something went wrong with the radio." );
			return;
		}

		// Turn the samples from a short array to a byte array.
		var samples = (await song.File.GetSamplesAsync())
			.SelectMany( sample => new byte[2] { 
				(byte)(sample & 0xFF), 
				(byte)(sample >> 8)
			} )
			.ToArray();

		stream = sound?.CreateStream( 44100, 1 );
		songStream = new MemoryStream( samples );
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
		if ( Game.IsServer && CurrentSong?.File != null && ElapsedTime > CurrentSong?.Duration )
		{
			var random = sounds[Game.Random.Int( sounds.Count - 1 )];
			Play( random );

			return;
		}

		if ( stream == null || sound == null || !stream.IsValid() || songStream == null )
			return;

		// Handle getting the samples and writing them to the SoundStream.
		var delay = (float)stream.MaxWriteSampleCount / 44100 / 2; // 1 channel and 44100 sample rate, but give the room for two sample writes to avoid hiccups
		if ( lastWritten < delay ) 
			return;
		
		var buffer = new short[stream.MaxWriteSampleCount - stream.QueuedSampleCount];
		var reader = new BinaryReader( songStream ); // WARNING: `using` disposes the stream!
		try
		{
			for ( var i = 0; i < buffer.Length; i++ )
				buffer[i] = reader.ReadInt16();
		}
		catch ( EndOfStreamException e )
		{
			songStream = null;
		}

		stream.WriteData( buffer );
		lastWritten = 0f;
	}
}
