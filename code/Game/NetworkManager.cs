namespace Sauna;

public sealed class NetworkManager : Component, Component.INetworkListener
{
	[Property] public GameObject Prefab { get; set; }

	/*void INetworkListener.OnConnected( Connection connection )
	{
	}*/

	protected override void OnStart()
	{
		if ( !GameNetworkSystem.IsActive )
			GameNetworkSystem.CreateLobby();
	}

	void INetworkListener.OnActive( Connection connection )
	{
		var obj = Prefab.Clone();
		var player = obj.Components.Get<Player>( FindMode.EverythingInSelfAndDescendants );
		obj.NetworkSpawn( connection );
		player.SetupConnection( connection );
	}

	void INetworkListener.OnDisconnected( Connection connection )
	{
	}

	/*void INetworkListener.OnBecameHost( Connection previousHost )
	{
	}*/
}
