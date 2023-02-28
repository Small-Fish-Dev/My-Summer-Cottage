namespace Sauna;

public partial class Radio : ModelEntity, IInteractable
{
	public struct Song
	{
		public string Producer;
		public string Name;
		public string Path;
	}

	public Song? CurrentSong { get; private set; }
	public TimeSince? ElapsedTime { get; private set; }
	public float? StartTime { get; private set; }

	string IInteractable.DisplayTitle => $"Mankka";

	private static string[] songFromPath( string path )
		=> path
			.Substring( 0, path.Length - 4 )
			.Replace( '_', ' ' )
			.Split( "-" );

	List<Song> sounds = FileSystem.Mounted.FindFile( "sounds/qoa/" )
		.Where( file => file.EndsWith( ".qoa" ) )
		.Select( path =>
		{
			var name = path.Substring( 0, path.Length - 4 );
			var separate = songFromPath( path );

			return new Song 
			{ 
				Producer = separate[0],
				Name = separate[1],
				Path = $"sounds/qoa/{name}.qoa",
			};
		} )
		.ToList();

	private Sound? sound;
	private SoundStream stream;

	private TimeSince lastWritten;
	private QOA.Decoder decoder;

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
				Play( song: random );
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
	/// <param name="target"></param>
	/// <param name="song"></param>
	public void Play( To? target = null, Song? song = null )
	{
		Game.AssertServer();

		if ( song != null )
		{
			ElapsedTime = 0f;
			CurrentSong = song;
			StartTime = Time.Now;
		}
		
		// Load file on client and start playing at desired time.
		playOnClient( target ?? To.Everyone, sounds.IndexOf( CurrentSong.Value ), StartTime.Value );
	}

	[ClientRpc]
	private async void playOnClient( int index, float startTime )
	{
		sound?.Stop();
		stream?.Delete();
		CurrentSong = null;

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

		var elapsedTime = Time.Now - startTime;
		decoder.SeekToSample( (int)(decoder.SampleRate * elapsedTime) );
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
		/*// Pick a new random song.
		if ( Game.IsServer && (CurrentSong?.Loaded ?? false) && ElapsedTime > CurrentSong?.Duration )
		{
			var random = sounds[Game.Random.Int( sounds.Count - 1 )];
			Play( random );

			return;
		}*/

		if ( stream == null || sound == null || !stream.IsValid() || decoder == null )
			return;

		// Handle getting the samples and writing them to the SoundStream.
		var amount = QOA.Base.MaxFrameSamples;
		var delay = (float)amount / decoder.SampleRate / 2; // 1 channel and 44100 sample rate, but give the room for two sample writes to avoid hiccups
		if ( lastWritten < delay ) 
			return;
		
		var buffer = new short[amount];
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

	[Event( "ClientConnect" )]
	static void onConnect( IClient client )
	{
		foreach ( var radio in Entity.All.OfType<Radio>() )
			radio.Play( To.Single( client ) );
	}
}
