namespace Sauna;

public class FuckedMapInstance : MapInstance
{
	protected override void OnUpdate()
	{
		// Calls the map reloading, so only allow in editor.
		if ( !Game.IsPlaying )
			base.OnUpdate(); 
	}

	protected override async Task OnLoad()
	{
		await base.OnLoad();
		MapName = Steam.SteamId.ToString();
	}
}
