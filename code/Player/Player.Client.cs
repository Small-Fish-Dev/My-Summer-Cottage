namespace Sauna;

partial class Player
{
	public static Player Local { get; private set; }

	[Sync] public Connection Connection { get; set; }
	public ulong SteamID => Connection.SteamId;
	public string Name => Connection.DisplayName;

	public void SetupConnection()
	{
		Connection = Network.OwnerConnection;
		GameObject.Name = $"{Name} / {SteamID}";

		if ( Network.IsOwner )
			Local = this;
	}
}
