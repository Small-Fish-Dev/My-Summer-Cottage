namespace Sauna;

public class FuckedMapInstance : MapInstance
{
	// todo @ceitine: make loading screen
	public static bool Loaded { get; private set; } = false;
	const string MAP_NAME = "untitled_2";

	protected override void OnUpdate()
	{
		// Calls the map reloading, so only allow in editor.
		if ( Game.IsPlaying )
			return;

		base.OnUpdate(); 
	}

	protected override async Task OnLoad()
	{
		if ( !Connection.Local.IsHost )
			MapName = MAP_NAME;

		Loaded = false;
		await base.OnLoad();

		if ( !Game.IsPlaying )
			return;

		MapName = Steam.SteamId.ToString();
		Loaded = true;
	}
}
