﻿namespace Sauna;

public class FuckedMapInstance : MapInstance
{
	const string MAP_NAME = "untitled_2";

	protected override void OnStart()
	{
		if ( !Connection.Local.IsHost )
		{
			MapName = MAP_NAME;
			base.OnUpdate();
		}
	}

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
