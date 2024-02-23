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
		obj.NetworkSpawn( connection );
		obj.Enabled = true;
	}

	void INetworkListener.OnDisconnected( Connection connection )
	{
	}

	/*void INetworkListener.OnBecameHost( Connection previousHost )
	{
	}*/
}
