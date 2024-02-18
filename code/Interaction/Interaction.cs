namespace Sauna;

public class Interaction : Component
{
	public delegate void InteractionEvent( Player interactor, GameObject obj );

	[Property]
	[InputAction]
	public string Keybind { get; set; }

	[Property]
	public string Description { get; set; }

	[Property]
	public Color Color { get; set; } = Color.White;

	[Property]
	public InteractionEvent Action { get; set; }
}
