namespace Sauna;

partial class Sauna
{
	public static CensorHook Censoring { get; private set; }
	public static DitheringHook Dithering { get; private set; }
	public static ScreenEffects Effects { get; private set; }

	[Event.Hotload]
	protected static void LoadRenderHooks()
	{
		if ( Game.IsServer )
			return;

		Censoring = Camera.Main.FindOrCreateHook<CensorHook>();
		Dithering = Camera.Main.FindOrCreateHook<DitheringHook>();

		Effects = Camera.Main.FindOrCreateHook<ScreenEffects>();
		Effects.Vignette.Smoothness = 0.9f;
		Effects.Vignette.Intensity = 0.45f;
	}
}
