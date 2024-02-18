namespace Sauna;

[GameResource("Sound with subtitles", "sws", "A sound that also has a subtitle attached to it.", Icon = "record_voice_over")]
public class SoundWithSubtitlesResource : GameResource
{
	public enum SubtitlePriority
	{
		Speech,
		SoundEffect
	}
	
	[Property] public SoundEvent SoundEvent { get; set; }
	[Property] public SubtitlePriority Priority { get; set; }
	
	/// <summary>
	/// A dictionary that maps an ISO 639-1 Country Code to the appropriate translation
	/// </summary>
	[Property] public Dictionary<string, string> Text { get; set; }
}
