namespace Sauna;

public class SpeechSettings
{
	public float Volume { get; set; } = 1;
	public Vector3 Position { get; set; } = 0;
	public Rotation Rotation { get; set; } = Rotation.Identity;
	public float Pitch { get; set; } = 1;
	public float Decibels { get; set; } = 70;
	public bool ListenLocal { get; set; } = false;
	public int Delay { get; set; } = 180;
	public int Accuracy { get; set; } = 2;
	public GameObject GameObject { get; set; }
}

public class Speech
{
	const int SOUNDS = 15;
	private List<SoundHandle> sounds;

	public SpeechSettings Settings { get; private set; }
	public bool Stopped { get; private set; }
	public float Duration { get; private set; }

	public static Speech Create( string text, SpeechSettings settings = default )
	{
		var speech = new Speech()
		{
			sounds = new(),
			Settings = settings,
			Duration = (float)text.Length / settings.Accuracy * settings.Delay / 1000f
		};

		var characters = text
			.ToLower()
			.RemoveDiacritics()
			.ToCharArray();

		var accuracy = Math.Max( settings.Accuracy, 1 );

		// Begin generating speech.
		new Action( async () =>
		{
			for ( int i = 0; i < characters.Length / accuracy; i++ )
			{
				var character = characters[Math.Min( i * accuracy, characters.Length - 1 )];
				if ( char.IsWhiteSpace( character ) )
				{
					await GameTask.Delay( settings.Delay );
					continue;
				}

				Game.SetRandomSeed( (byte)character );
				var index = Game.Random.Int( 1, SOUNDS );
				var soundFile = SoundFile.Load( $"sounds/speech/{index}.sound" );
				var sound = settings.GameObject.IsValid() 
					? settings.GameObject.PlaySound( soundFile.ResourcePath )
					: Sound.PlayFile( soundFile );

				if ( !settings.GameObject.IsValid() )
				{
					sound.Rotation = settings.Rotation;
					sound.Position = settings.Position;
				}

				sound.Volume = settings.Volume;
				sound.Pitch = settings.Pitch;
				sound.Decibels = settings.Decibels;
				sound.ListenLocal = settings.ListenLocal;

				speech.sounds.Add( sound );
				await GameTask.Delay( settings.Delay );
			}
		} ).Invoke();

		return speech;
	}

	public void Stop()
	{
		if ( Stopped )
			return;

		foreach ( var sound in sounds )
			sound?.Stop();

		Stopped = true;
	}

	[ConCmd]
	public static void TestSound( string input = "blablabla fuck you bithc!!!" )
	{
		Create( input, settings: new() { GameObject = Player.Local.GameObject, Pitch = 0.7f } );
	}
}
