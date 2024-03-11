namespace Sauna;

public class FuckedMapInstance : MapInstance
{
	protected override void OnUpdate()
	{
		// Calls the map reloading, so only allow in editor.
		if ( Game.IsPlaying )
			return;

		base.OnUpdate(); 
	}

	protected override async Task OnLoad()
	{
		await base.OnLoad();

		if ( !Game.IsPlaying )
			return;

		MapName = Steam.SteamId.ToString();
	}
}
