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

		// Someone report this bug, for some reason the static list doesn't get cleared on restart!
		Player._internalPlayers.Clear();

		if ( !GameNetworkSystem.IsActive && Connection.Local.IsHost )
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

		if ( Connection.Local.IsHost )
		{
			TaskMaster._instance.SendCurrentTaskProgress( TaskMaster.PackageTaskProgress( TaskMaster.ActiveTasks ).Serialize() );
		}
	}

	void INetworkListener.OnDisconnected( Connection connection )
	{
		Player._internalPlayers.RemoveAll( ( p ) => p is null || p.Connection.Id == connection.Id );
	}

	/*void INetworkListener.OnBecameHost( Connection previousHost )
	{
	}*/
}
