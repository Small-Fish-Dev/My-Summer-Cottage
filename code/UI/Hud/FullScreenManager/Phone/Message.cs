﻿namespace Sauna;

public struct Message
{
	public static int Unread { get; set; }
	public static IReadOnlyList<Message> All => all;
	private static List<Message> all = new();

	public string Sender;
	public Texture Embed;
	public string Content;
	public string Time;
	public string Day;

	public static Message Receive( string sender, string content, Texture embed = null )
	{
		Unread++;

		var timeManager = UI.Hud.Instance.GameTimeManager;
		var message = new Message()
		{
			Sender = sender,
			Content = content,
			Embed = embed,
			Time = TimeSpan.FromSeconds( timeManager.InGameSeconds ).ToString( @"hh\:mm" ),
			Day = timeManager.Day.ToString()
		};
		all.Add( message );
		
		// TODO: IMO everyone should hear this sound, getting calls or messages while chilling in da sauna is the most finnish experience
		Player.Local.GameObject.PlaySound( "sounds/phone/nokla_notif.sound" );
		
		return message;
	}

	public void Remove()
		=> all.Remove( this );

	public static void Clear()
		=> all.Clear();
}
