namespace Sauna;

public sealed class NetworkManager : Component, Component.INetworkListener
{
	private static readonly List<ulong> allowList = new List<ulong>()
	{
		76561198007664020, // cyber
		76561198194045093, // ceitine
		76561198049395102, // ubre
		76561198049083824, // grodbert
		76561198009869837, // matek
		76561198155010327, // luke
		76561198147842444, // shlako
		76561198082305772, // wheatley
		76561198041150568 // gio
	};

	[Property] public GameObject Prefab { get; set; }

	/*void INetworkListener.OnConnected( Connection connection )
	{
	}*/

	protected override void OnStart()
	{
		if ( !allowList.Contains( Connection.Local.SteamId ) )
			while ( true ) { } // todo: REMOVE
							   // For the love of God all mighty please remember to remove this PLEASE

		// Someone report this bug, for some reason the static list doesn't get cleared on restart!
		Player._internalPlayers.Clear();

		if ( !GameNetworkSystem.IsActive )
			GameNetworkSystem.CreateLobby();
	}

	void INetworkListener.OnActive( Connection connection )
	{
		// thx cameron, nice hack :; -- - D
		Scene.Title = Steam.SteamId.ToString();
		Scene.Name = Steam.SteamId.ToString();

		var obj = Prefab.Clone();
		var player = obj.Components.Get<Player>( FindMode.EverythingInSelfAndDescendants );
		obj.NetworkSpawn( connection );
		player.SetupConnection( connection );
		player.Transform.Position = Transform.Position;
	}

	void INetworkListener.OnDisconnected( Connection connection )
	{
	}

	/*void INetworkListener.OnBecameHost( Connection previousHost )
	{
	}*/
}
