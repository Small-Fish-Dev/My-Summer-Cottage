namespace Sauna;

public sealed class PrefabInitializer : Component, Component.ExecuteInEditor
{
	protected override void OnAwake() => PrefabLibrary.Initialize();
}
