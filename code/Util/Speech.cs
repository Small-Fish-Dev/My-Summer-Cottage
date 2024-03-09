namespace Sauna;

public struct Speech
{
	const int SOUNDS = 15;
	private List<SoundHandle> sounds;

	public bool Stopped { get; private set; }

	public static Speech Create( string text, int delay = 240, SoundHandle handle = null )
	{
		var speech = new Speech()
		{
			sounds = new(),
		};

		var characters = text
			.ToLower()
			.RemoveDiacritics()
			.ToCharArray();

		// Begin generating speech.
		new Action( async () =>
		{
			for ( int i = 0; i < characters.Length; i++ )
			{
				var character = characters[i];
				if ( char.IsWhiteSpace( character ) )
				{
					await GameTask.Delay( delay );
					continue;
				}

				var index = Game.Random.Int( 1, SOUNDS );
				var sound = Sound.PlayFile( SoundFile.Load( $"sounds/speech/{index}.sound" ) ); // Figure out something better for this cuz dis shit sucks!
				sound.Rotation = handle?.Rotation ?? default;
				sound.Position = handle?.Position ?? default;
				sound.Volume = handle?.Volume ?? 1;
				sound.Pitch = handle?.Pitch ?? 1;
				sound.Decibels = handle?.Decibels ?? 70;
				sound.ListenLocal = handle?.ListenLocal ?? default;

				speech.sounds.Add( sound );
				await GameTask.Delay( delay );
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
		var sound = Sound.PlayFile( SoundFile.Load( "sounds/speech/1.sound" ) );
		sound.ListenLocal = true;
		sound.Pitch = 0.6f;
		Create( input, handle: sound );
	}
}
