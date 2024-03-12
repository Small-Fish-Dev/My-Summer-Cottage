namespace Sauna.UI.EpicIntro;

[Category( "Intro" )]
public class IntroComponent : Component
{
	[Property] public bool ForceShowIntro = false;
	[Property] public SceneFile MenuScene;
	[Property] public PanelComponent IntroPanel;

	protected override void OnStart()
	{
		// TODO: should we have other conditions for the intro to be skipped?
		if ( !ForceShowIntro && FileSystem.Data.FileExists( PlayerSave.FILE_PATH ) )
			GoToMenu();
		else
			IntroPanel.Enabled = true;
	}

	public void GoToMenu()
	{
		Scene.Load( MenuScene );
	}
}
