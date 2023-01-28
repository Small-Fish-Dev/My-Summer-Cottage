namespace Sauna;

public struct InteractionInfo
{
	public Predicate<Player> Predicate;
	public Action<Player> Function;
	public string Text;
}
