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

	public string GetLocalized()
	{
		if ( Text.TryGetValue( GameSettings.GetLanguage, out var text ) )
			return text;

		if ( Text.TryGetValue( "en", out text ) )
			return text;

		if ( Text.Count == 0 )
			throw new Exception( $"Invalid sound resource {ResourcePath} has no subtitles" );

		return Text.First().Value;
	}
}
