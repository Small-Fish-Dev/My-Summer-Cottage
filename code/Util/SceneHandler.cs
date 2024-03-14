namespace Sauna;

public enum SaunaScene
{
	Creation,
	Game
}

public static class SceneHandler
{
	public static async void ChangeScene( SaunaScene scene, ulong? lobby = null, bool stopSound = true )
	{
		var path = scene switch
		{
			SaunaScene.Creation => "scenes/creation_new.scene",
			SaunaScene.Game => "scenes/finland.scene",
			_ => null
		};

		if ( string.IsNullOrEmpty( path ) )
			return;

		if ( stopSound )
			Sound.StopAll( 5f );

		// If is game.
		if ( scene == SaunaScene.Game )
		{
			// Connect to lobby.
			if ( lobby.HasValue )
			{
				var connected = await GameNetworkSystem.TryConnectSteamId( lobby.Value );
				if ( connected )
					return;
			}

			// Connect to local.
			Game.ActiveScene.LoadFromFile( path );
			GameNetworkSystem.CreateLobby();
			return;
		}

		Game.ActiveScene.LoadFromFile( path );
		return;
	}
}
