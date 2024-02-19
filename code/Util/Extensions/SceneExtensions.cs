namespace Sauna.Util.Extensions;

public static class SceneExtensions
{
	public static SoundSystem009 SoundSystem( this Scene scene ) => scene.GetSystem<SoundSystem009>();
}
