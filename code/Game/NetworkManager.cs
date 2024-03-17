namespace Sauna;

public sealed class NetworkManager : Component, Component.INetworkListener
{
	public const int MAX_PLAYERS = 4;

	[Property] public GameObject Prefab { get; set; }
	[HostSync] public static Guid HostId { get; set; }

	protected override void OnStart()
	{
		if ( !GameNetworkSystem.IsActive && !IsProxy )
		{
			// Spawn host player. 
			var obj = Prefab.Clone();
			var player = obj.Components.Get<Player>( FindMode.EverythingInSelfAndDescendants );
			player.Respawn();
			obj.NetworkMode = NetworkMode.Object;
			obj.NetworkSpawn();
			obj.Name = $"Host Player";

			// Setup host stuff.
			Player._internalPlayers?.Clear();
			Player._internalPlayers?.Add( player );
			Player.Local = player;

			// Start story.
			StoryMaster.StartSession();
		}

		if ( Player.All.Count >= MAX_PLAYERS )
		{
			UI.MainMenu.PlayIntro = false;
			SceneHandler.ChangeScene( SaunaScene.MainMenu );
			GameNetworkSystem.Disconnect();
			return;
		}
	}

	public static void ToggleLobby()
	{
		if ( !Connection.Local.IsHost )
			return;

		// Start lobby.
		if ( !GameNetworkSystem.IsActive )
		{
			GameNetworkSystem.CreateLobby();
			return;
		}

		// Close lobby.
		ServerClose( true );
		GameNetworkSystem.Disconnect();

		for ( int i = 0; i < Player.All.Count; i++ )
		{
			var p = Player.All.ElementAtOrDefault( i );
			if ( p is null || p == Player.Local )
				continue;

			Player._internalPlayers.Remove( p );
			p.Destroy();
		}
	}

	void INetworkListener.OnActive( Connection connection )
	{
		if ( connection.IsHost )
		{
			HostId = connection.Id;
			Player.Local.ConnectionID = connection.Id;
			return;
		}

		if ( Player.All.Count >= MAX_PLAYERS )
			return;

		var obj = Prefab.Clone();
		var player = obj.Components.Get<Player>( FindMode.EverythingInSelfAndDescendants );
		player.Respawn();
		obj.NetworkMode = NetworkMode.Object;
		obj.NetworkSpawn( connection );
		player.SetupConnection( connection );
	}

	void INetworkListener.OnDisconnected( Connection connection )
	{
		if ( connection.IsHost )
			ServerClose( true );

		BroadcastDisconnect( connection.Id );
	}

	[Broadcast]
	public void BroadcastDisconnect( Guid id )
	{
		Player._internalPlayers.RemoveAll( ( p ) => p is null || p.Connection.Id == id );
	}

	[Broadcast( NetPermission.HostOnly )]
	public static void ServerClose( bool ignoreHost )
	{
		if ( ignoreHost && Connection.Local.Id == HostId )
			return;

		GameNetworkSystem.Disconnect();
		UI.MainMenu.PlayIntro = false;
		SceneHandler.ChangeScene( SaunaScene.MainMenu );
	}

	void INetworkListener.OnBecameHost( Connection previousHost )
	{
		// Broadcast for everyone to leave!
		ServerClose( false );
	}
}
