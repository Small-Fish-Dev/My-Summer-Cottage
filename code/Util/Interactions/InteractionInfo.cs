namespace Sauna;

public struct InteractionInfo
{
	public Predicate<Player> Predicate;
	public Action<Player> Function;
#nullable enable
	public string? Text;
	public Func<string>? TextFunction;
#nullable disable

	public string Result => Text ?? TextFunction?.Invoke();
}
