namespace Sauna;

partial class Sauna
{
	public static DitheringHook Dithering { get; private set; }
	public static EyeProtector Censoring { get; private set; }
	public static ScreenEffects Effects { get; private set; }

	[Event.Hotload]
	protected static void LoadRenderHooks()
	{
		if ( Game.IsServer )
			return;

		Dithering = Camera.Main.FindOrCreateHook<DitheringHook>();
		Censoring = Camera.Main.FindOrCreateHook<EyeProtector>();

		Effects = Camera.Main.FindOrCreateHook<ScreenEffects>();
		Effects.Vignette.Smoothness = 0.9f;
		Effects.Vignette.Intensity = 0.5f;

		Effects.Order = 0;
	}
}
