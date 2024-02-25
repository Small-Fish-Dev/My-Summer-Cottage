namespace Sauna;

public struct RadioChannel
{
	public readonly static RadioChannel[] All = new RadioChannel[]
	{
		new RadioChannel { Name = "YLE X", URL = "https://yleradiolive.akamaized.net/hls/live/2027674/in-YleX/256/variant.m3u8" },
		new RadioChannel { Name = "YLE Radio 1", URL = "https://yleradiolive.akamaized.net/hls/live/2027672/in-YleRadio1/256/variant.m3u8" },
		new RadioChannel { Name = "Järviradio FM", URL = "https://jarviradio.radiotaajuus.fi:9000/jr" },
		new RadioChannel { Name = "Loop", URL = "https://nelonenmedia-pmd-ads-audio.nm-stream.nelonenmedia.fi/Sanomista_vastuullisuudesta_Puffi2.mp3" },
		new RadioChannel { Name = "Aito Iskelmä", URL = "https://nelonenmedia-pmd-ads-audio.nm-stream.nelonenmedia.fi/Mynthon_TukalatTilanteet2_Radio_20s_48khz_16bit_MASTER_300123.mp3" }
	};

	public string Name;
	public string URL;
}
