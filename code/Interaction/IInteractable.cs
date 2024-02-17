namespace Sauna;

public interface IInteractable
{
	/// <summary>
	/// The displayed title of this interactable.
	/// </summary>
	public string DisplayTitle { get; }

	/// <summary>
	/// The color used for the displayed text of this interactable.
	/// </summary>
	public Color DisplayColor => Color.Orange;
}
