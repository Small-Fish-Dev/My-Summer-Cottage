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
	private MemoryStream currentSongStream;

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

		stream = sound?.CreateStream(44100, 1); // TODO: Please.... Work...............
		var songStream = new MemoryStream(stream.MaxWriteSampleCount);
		var samples = await currentFile.GetSamplesAsync();
		Log.Error($"{samples.Length}");
		// TODO: when this shite gets whitelisted
		//var samplesBytes = new byte[samples.Length * 2];
		//Buffer.BlockCopy(samples, 0, samplesBytes, 0, samplesBytes.Length);
		// TODO: or...
		//await using (var bw = new BinaryWriter(currentSongStream))
		//	foreach (var sample in samples)
		//		bw.Write(sample);
		foreach (var sample in samples)
		{
			songStream.WriteByte((byte) (sample & 0xFF));
			songStream.WriteByte((byte) (sample >> 8));
		}
		Log.Error($"{songStream.Length}");
		songStream.Position = 0;
		currentSongStream = songStream;
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
		if ( stream == null || sound == null || !stream.IsValid() || currentSongStream == null )
			return;

		var delay = (float)stream.MaxWriteSampleCount / 44100 / 2; // 1 channel and 44100 sample rate, but give the room for two sample writes to avoid hiccups
		if (lastWritten < delay) return;
		
		var buffer = new short[stream.MaxWriteSampleCount - stream.QueuedSampleCount];
		var br = new BinaryReader(currentSongStream); // WARNING: `using` disposes the stream!
		try
		{
			for (var i = 0; i < buffer.Length; i++)
				buffer[i] = br.ReadInt16();
		}
		catch (EndOfStreamException e)
		{
			Log.Info("The end of song!");
			currentSongStream = null;
		}

		stream.WriteData( buffer );
		lastWritten = 0f;
	}
}
