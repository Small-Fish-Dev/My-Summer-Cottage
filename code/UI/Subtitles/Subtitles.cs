using Sauna.Components;
using Sauna.Util.Extensions;

namespace Sauna.UI.Subtitles;

[StyleSheet]
public class Subtitles : PanelComponent
{
	[Property] public uint MaxSubtitlesOnScreen = 3;
	[Property] public uint ForceRemoveWhenOverLimit = 1;
	
	private Dictionary<SoundSystem009.SubtitlePopup, SubtitlePanel> _subtitlePanelsDictionary = new();

	private Panel _container;

	protected override void OnEnabled()
	{
		base.OnEnabled();
		
		_container = Panel.Add.Panel( "container" );
	}

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
		var panel = _container.AddChild<SubtitlePanel>();
		panel.Resource = subtitlePopup.Resource;
		if ( subtitlePopup.Source.IsValid() && subtitlePopup.Source.Components.Get<FancyName>() is FancyName name )
			panel.Name = name;
		
		_subtitlePanelsDictionary[subtitlePopup] = panel;

		if ( _container.ChildrenCount <= MaxSubtitlesOnScreen )
			return;

		var panelToRemove = _container.Children.FirstOrDefault();
		if ( panelToRemove is null )
			// Huh? That's weird
			return;

		var entry = _subtitlePanelsDictionary.First( kv => kv.Value == panelToRemove ).Key;
		_subtitlePanelsDictionary.Remove( entry );
		
		var forceRemove = _container.ChildrenCount > MaxSubtitlesOnScreen + ForceRemoveWhenOverLimit;	
		panelToRemove.Delete( forceRemove );
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		
		_container.SetClass( "hidden", _container.ChildrenCount == 0 );
	}

	private void OnSoundStopped( SoundSystem009.SubtitlePopup subtitlePopup )
	{
		if ( !_subtitlePanelsDictionary.Remove(subtitlePopup, out var panel) )
			return;

		panel.Delete();
	}

	protected override int BuildHash() => HashCode.Combine( _subtitlePanelsDictionary );
}
