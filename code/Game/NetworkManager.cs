namespace Sauna;

public sealed class NetworkManager : Component, Component.INetworkListener
{
	public const int MAX_PLAYERS = 4;

	[Property] public GameObject Prefab { get; set; }

	protected override void OnStart()
	{
		if ( Player.All.Count >= MAX_PLAYERS )
		{
			GameNetworkSystem.Disconnect();
			return;
		}
		
		if ( !GameNetworkSystem.IsActive )
			GameNetworkSystem.CreateLobby();
	}

	void INetworkListener.OnActive( Connection connection )
	{
		if ( Player.All.Count >= MAX_PLAYERS )
			return;

		var obj = Prefab.Clone();
		var player = obj.Components.Get<Player>( FindMode.EverythingInSelfAndDescendants );
		player.Respawn();
		obj.NetworkMode = NetworkMode.Object;
		obj.NetworkSpawn( connection );
		player.SetupConnection( connection );

		if ( connection.IsHost && !IsProxy )
			StoryMaster.StartSession();
	}

	void INetworkListener.OnDisconnected( Connection connection )
	{
		BroadcastDisconnect( connection.Id );
	}

	[Broadcast]
	public void BroadcastDisconnect( Guid id )
	{
		Player._internalPlayers.RemoveAll( ( p ) => p is null || p.Connection.Id == id );
	}

	[Broadcast( NetPermission.HostOnly )]
	public void ServerClose()
	{
		GameNetworkSystem.Disconnect();
		UI.MainMenu.PlayIntro = false;
		SceneHandler.ChangeScene( SaunaScene.MainMenu );
	}

	void INetworkListener.OnBecameHost( Connection previousHost )
	{
		// Broadcast for everyone to leave!
		ServerClose();
	}
}
