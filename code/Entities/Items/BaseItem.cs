namespace Sauna;

public class BaseItem : AnimatedEntity, IInteractable
{
	/// <summary>
	/// Path to the model that this item uses.
	/// </summary>
	public new virtual string Model => "";

	/// <summary>
	/// The displayed title of this item.
	/// </summary>
	public virtual string Title => "Item";

	/// <summary>
	/// The displayed color of this item.
	/// </summary>
	public virtual Color Color => Color.Orange;

	/// <summary>
	/// The amount that the displayed interaction is offset.
	/// </summary>
	public virtual Vector3 TitleOffset => Vector3.Zero;

	string IInteractable.DisplayTitle => Title;
	Color IInteractable.DisplayColor => Color;
	InteractionOffset IInteractable.Offset => TitleOffset;

	public override void Spawn()
	{
		SetModel( Model );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
	}
}
