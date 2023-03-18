namespace Sauna;

partial class Player
{
	/// <summary>
	/// Please do not use this. 
	/// Use Eventlogger.Send(...) instead.
	/// </summary>
	/// <param name="data"></param>
	[ClientRpc]
	public static void _sendEventlog( byte[] data )
	{
		Eventlogger.FromBytes( data );
	}


	/// <summary>
	/// Please do not use this.
	/// </summary>
	/// <param name="indent"></param>
	/// <param name="message"></param>
	[ClientRpc]
	public static void _sendMessage( int indent, string message )
	{
		if ( Entity.FindByIndex( indent ) is not Player player )
			return;

		Speechbubble.Create( message, player );
	}

	[ConCmd.Server]
	public static void SendMessage( string message )
	{
		if ( ConsoleSystem.Caller.Pawn is not Player pawn )
			return;

		_sendMessage( pawn.NetworkIdent, message );
	}
}
