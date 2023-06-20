namespace Sauna;

[Flags]
public enum Interactability
{
	None,
	Ground = 1 << 1,
	Hold = 1 << 2,
	Inventory = 1 << 3,
}

public struct InteractionInfo
{
	public Predicate<Player> Predicate;
	public Action<Player> Function;
#nullable enable
	public string? Text;
	public Func<string>? TextFunction;
#nullable disable
	public Interactability Interactability;

	public string Result => Text ?? TextFunction?.Invoke();

	public InteractionInfo()
	{
		Interactability = Interactability.Ground | Interactability.Inventory;
	}
}
