namespace Sauna;

public struct InteractionInfo
{
	public Predicate<Player> Predicate;
	public Action<Player, IInteractable> Function;
	public string Text;
}
