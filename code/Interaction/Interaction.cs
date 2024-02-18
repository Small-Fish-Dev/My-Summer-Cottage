namespace Sauna;

public class Interaction : Component
{
	public delegate void InteractionEvent( Player interactor, GameObject obj );

	[Property]
	[InputAction]
	public string Keybind;

	[Property]
	public string Description;

	[Property]
	public Color Color = Color.Orange;

	[Property]
	public InteractionEvent Action;
}
