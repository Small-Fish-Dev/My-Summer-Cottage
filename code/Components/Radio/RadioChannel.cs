namespace Sauna;

public struct RadioChannel
{
	public readonly static RadioChannel[] All = new RadioChannel[]
	{
		new RadioChannel { Name = "YLE X", URL = "https://yleradiolive.akamaized.net/hls/live/2027674/in-YleX/256/variant.m3u8" },
		new RadioChannel { Name = "Järviradio FM", URL = "https://jarviradio.radiotaajuus.fi:9000/jr" },
		new RadioChannel { Name = "Zakkujo - Ero vaimosta", URL = "sounds/music/zakkujo-ero_vaimosta.sound", Local = true },
		new RadioChannel { Name = "Gus and the Ganders - Surfin' 81", URL = "sounds/music/dawdle-gus_and_the_ganders_-_surfin_81__game.sound", Local = true },
	};

	public string Name;
	public string URL;
	public bool Local;
}
