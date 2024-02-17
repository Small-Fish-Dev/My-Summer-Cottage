namespace Sauna;

public interface IInteractable
{
	/// <summary>
	/// The displayed title of this interactable.
	/// </summary>
	public string DisplayTitle => (this as GameObject)?.Name ?? string.Empty;

	/// <summary>
	/// The color used for the displayed text of this interactable.
	/// </summary>
	public Color DisplayColor => Color.Orange;
}
