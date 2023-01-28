namespace Sauna;

public partial class Radio : ModelEntity, IInteractable
{
	string IInteractable.DisplayTitle => "Mankka";

	public struct Song
	{
		public string Producer;
		public string Name;
		public string Path;
	}

	List<Song> sounds = FileSystem.Mounted.FindFile( "sounds/music/" )
		.Where( file => file.EndsWith( ".sound" ) )
		.Select( path =>
		{
			var separate = path
				.Substring( 0, path.Length - 6 )
				.Replace( '_', ' ' )
				.Split( "-" );
			
			return new Song 
			{ 
				Producer = separate[0],
				Name = separate[1],
				Path = $"sounds/music/{path}",
			};
		} )
		.ToList();

	Sound? sound;
	Song? current;

	public bool Playing => sound != null;

	public Radio()
	{
		var interactable = this as IInteractable;

		interactable.AddInteraction( InputButton.Use, new()
		{
			Predicate = ( Player pawn ) => true,
			Function = ( Player pawn ) =>
			{
				if ( Game.IsClient ) return;
				
				if ( Playing )
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

		sound?.Stop();
		sound = null;
		current = null;
	}

	/// <summary>
	/// Play a song.
	/// </summary>
	/// <param name="song"></param>
	public void Play( Song song )
	{
		Game.AssertServer();

		Stop();
		current = song;
		sound = Sound.FromEntity( song.Path, this );
	}

	[Event.Tick.Server]
	void tick()
	{
		if ( sound != null && (sound?.Finished ?? false) )
		{
			var random = sounds[Game.Random.Int( sounds.Count - 1 )];
			Play( random );
		}
	}
}
