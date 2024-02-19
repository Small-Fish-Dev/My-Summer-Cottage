using Sauna.Util.Extensions;

namespace Sauna.UI.Subtitles;

[StyleSheet]
public class Subtitles : PanelComponent
{
	[Property] public uint MaxSubtitlesOnScreen = 3;
	[Property] public uint ForceRemoveWhenOverLimit = 1;
	
	private Dictionary<SoundSystem009.SubtitlePopup, SubtitlePanel> _subtitlePanelsDictionary = new();

	protected override void OnAwake()
	{
		base.OnAwake();

		var soundSystem = Scene.SoundSystem();
		soundSystem.OnSoundPlayed += OnSoundPlayed;
		soundSystem.OnSoundStopped += OnSoundStopped;
	}

	protected override void OnDestroy()
	{
		var soundSystem = Scene.SoundSystem();
		soundSystem.OnSoundPlayed -= OnSoundPlayed;
		soundSystem.OnSoundStopped -= OnSoundStopped;
		
		base.OnDestroy();
	}

	private void OnSoundPlayed( SoundSystem009.SubtitlePopup subtitlePopup )
	{
		var panel = Panel.AddChild<SubtitlePanel>();
		panel.Resource = subtitlePopup.Resource;
		
		_subtitlePanelsDictionary[subtitlePopup] = panel;

		if ( Panel.ChildrenCount <= MaxSubtitlesOnScreen )
			return;

		var panelToRemove = Panel.Children.LastOrDefault();
		if ( panelToRemove is null )
			// Huh? That's weird
			return;

		var entry = _subtitlePanelsDictionary.First( kv => kv.Value == panelToRemove ).Key;
		_subtitlePanelsDictionary.Remove( entry );
		
		var forceRemove = Panel.ChildrenCount > MaxSubtitlesOnScreen + ForceRemoveWhenOverLimit;	
		panelToRemove.Delete( forceRemove );
	}

	private void OnSoundStopped( SoundSystem009.SubtitlePopup subtitlePopup )
	{
		if ( !_subtitlePanelsDictionary.Remove(subtitlePopup, out var panel) )
			return;

		panel.Delete();
	}

	protected override int BuildHash() => HashCode.Combine( _subtitlePanelsDictionary );
}
