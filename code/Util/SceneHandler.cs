namespace Sauna;

public enum SaunaScene
{
	Creation,
	Game
}

public static class SceneHandler
{
	public static void ChangeScene( SaunaScene scene, ulong? lobby = null )
	{
		var path = scene switch
		{
			SaunaScene.Creation => "scenes/creation_new.scene",
			SaunaScene.Game => "scenes/finland.scene",
			_ => null
		};

		if ( string.IsNullOrEmpty( path ) )
			return;

		Sound.StopAll( 0.5f );

		// If is game.
		if ( scene == SaunaScene.Game )
		{
			// Connect to lobby.
			if ( lobby != null )
			{
				GameNetworkSystem.Connect( lobby.Value );
				return;
			}

			// Connect to local.
			Game.ActiveScene.LoadFromFile( path );
			GameNetworkSystem.CreateLobby();
			return;
		}

		Game.ActiveScene.LoadFromFile( path );
	}
}
