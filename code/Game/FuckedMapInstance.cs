namespace Sauna;

public class FuckedMapInstance : MapInstance
{
	bool set = false;
	string realMap = "";

	protected override void OnUpdate()
	{
		// Calls the map reloading, so only allow in editor.
		if ( set || Game.IsPlaying )
			return;

		if ( !string.IsNullOrEmpty( realMap ) ) 
			MapName = realMap;

		base.OnUpdate(); 
	}

	protected override async Task OnLoad()
	{
		await base.OnLoad();
		realMap = MapName;
		MapName = Steam.SteamId.ToString();
		set = true;
	}
}
