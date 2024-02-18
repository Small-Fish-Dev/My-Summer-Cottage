namespace Sauna.UI;

[StyleSheet]
public class Subtitles : Panel
{
	protected override int BuildHash() => HashCode.Combine( SoundSystem009.The.SubtitleSounds );
}
