namespace Sauna;

public struct Interaction
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

	/// <summary>
	/// Whether or not the interaction is able to be performed
	/// </summary>
	[Property]
	public Func<string> DynamicText { get; set; }

	/// <summary>
	/// The text that should actually be displayed.
	/// </summary>
	public string Text => DynamicText?.Invoke() ?? Description;
}

public class Interactions : Component
{
	[Property]
	public List<Interaction> ObjectInteractions { get; set; }

	protected override void DrawGizmos()
	{
		
	}
}
