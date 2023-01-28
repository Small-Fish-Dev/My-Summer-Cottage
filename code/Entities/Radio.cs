namespace Sauna;

public partial class Radio : ModelEntity, IInteractable
{
	public struct Song
	{
		public string Producer;
		public string Name;
		public string Path;
	}

	public TimeSince? ElapsedTime { get; private set; }

	string IInteractable.DisplayTitle => "Mankka";

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
			
			return new Song 
			{ 
				Producer = separate[0],
				Name = separate[1],
				Path = $"sounds/music/{name}.vsnd",
			};
		} )
		.ToList();

	Sound? sound;
	SoundStream stream;
	SoundFile currentFile;

	TimeSince lastWritten;
	int offset;
	short[] samples;

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

		// Stop playing song on client.
		playOnClient( To.Everyone, "", 0f );
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

		// Load file on client and start playing at desired time.
		playOnClient( target ?? To.Everyone, song.Path, 0f );
	}

	[ClientRpc]
	private async void playOnClient( string path, float elapsedTime )
	{
		sound?.Stop();
		stream?.Delete();

		if ( path == "" )
			return;

		sound = Sound.FromEntity( "audiostream.default", this );

		currentFile = SoundFile.Load( path );
		var loaded = await currentFile.LoadAsync();
		samples = await currentFile.GetSamplesAsync();
		stream = sound?.CreateStream(); // TODO: Please.... Work...............
		offset = (int)(elapsedTime * currentFile.Rate);
	}

	[Event.Tick]
	void tick()
	{
		if ( stream == null || sound == null || !stream.IsValid() )
			return;

		var delay = (float)stream.MaxWriteSampleCount / sizeof( short ) / 44100;
		if ( lastWritten >= delay )
		{
			var buffer = new short[stream.MaxWriteSampleCount];
			for ( int i = 0; i < buffer.Length; i++ )
				if ( offset + i > samples.Length - 1 )
				{
					break;
				}
				else
				{
					buffer[i] = samples[i + offset];
				}

			stream?.WriteData( buffer.AsSpan() );
			offset += buffer.Length;
			lastWritten = 0f;
		}
	}
}
