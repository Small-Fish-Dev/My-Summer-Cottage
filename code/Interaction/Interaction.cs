namespace Sauna;

public class Interaction : Component
{
	/// <summary>
	/// The keybind used to trigger this interaction
	/// </summary>
	[Property]
	[InputAction]
	public string Keybind { get; set; }

	/// <summary>
	/// The UI description displayed when interacting
	/// </summary>
	[Property]
	public string Description { get; set; }

	/// <summary>
	/// The action that is performed when interacted with
	/// </summary>
	[Property]
	public InteractionEvent Action { get; set; }
	public delegate void InteractionEvent( Player interactor, GameObject obj );

	/// <summary>
	/// Whether or not the interaction is able to be performed
	/// </summary>
	[Property]
	public Func<bool> Disabled { get; set; }
	public bool IsInteractable => Disabled is null || !Disabled();
}
