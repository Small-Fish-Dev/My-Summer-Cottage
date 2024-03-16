namespace Sauna;

public enum SaunaScene
{
	Creation,
	Game,
	MainMenu
}

public static class SceneHandler
{
	public static async void ChangeScene( SaunaScene scene, ulong? lobby = null, bool stopSound = true )
	{
		var path = scene switch
		{
			SaunaScene.Creation => "scenes/creation.scene",
			SaunaScene.Game => "scenes/finland.scene",
			SaunaScene.MainMenu => "scenes/menu.scene",
			_ => null
		};

		if ( string.IsNullOrEmpty( path ) )
			return;

		if ( !ResourceLibrary.TryGet<GameResource>( path, out var resource ) )
			return;

		if ( stopSound )
			Sound.StopAll( 5f );

		// If is game.
		if ( scene == SaunaScene.Game && lobby.HasValue )
		{
			var connected = await GameNetworkSystem.TryConnectSteamId( lobby.Value );
			if ( connected )
				return;
		}

		Game.ActiveScene.Load( resource );
		return;
	}
}
