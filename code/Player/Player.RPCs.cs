namespace Sauna;

partial class Player
{
	/// <summary>
	/// Please do not use this.
	/// Use Eventlogger.Send(...) instead.
	/// </summary>
	/// <param name="text"></param>
	/// <param name="time"></param>
	[ClientRpc]
	public static void _addEventlog( string text, float time )
	{
		Eventlogger.Instance?.Append( text, time );
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

		if ( player.Position.Distance( Camera.Position ) < 350 )
			Eventlogger.Instance.Append( $"{(player == Game.LocalPawn ? "You" : player.Client.Name)} said: " 
				+ $"<gray>\"{message}\"" );

		Speechbubble.Create( message, player );
	}

	/// <summary>
	/// Send a chat message from client to server.
	/// </summary>
	/// <param name="message"></param>
	[ConCmd.Server]
	public static void SendMessage( string message )
	{
		if ( ConsoleSystem.Caller.Pawn is not Player pawn )
			return;

		_sendMessage( pawn.NetworkIdent, message );
	}
}
