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

		if ( !GameNetworkSystem.IsActive && Connection.Local.IsHost )
		{
			Player._internalPlayers.Clear(); // Someone report this bug, for some reason the static list doesn't get cleared on restart!
			GameNetworkSystem.CreateLobby();
		}
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

		if ( Connection.Local.IsHost )
			StoryMaster.StartSession();
	}

	void INetworkListener.OnDisconnected( Connection connection )
	{
		Player._internalPlayers.RemoveAll( ( p ) => p is null || p.Connection.Id == connection.Id );
	}

	/*void INetworkListener.OnBecameHost( Connection previousHost )
	{
	}*/
}
