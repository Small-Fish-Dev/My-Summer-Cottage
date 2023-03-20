namespace Sauna;

public class ItemComponent : EntityComponent<BaseItem>
{
	/// <summary>
	/// The ItemPrefab entity this component is attached to.
	/// </summary>
	public BaseItem Item => Entity;

	/// <summary>
	/// The interactable interface of this component.
	/// </summary>
	public IInteractable Interactable => Entity;

	/// <summary>
	/// Called when the ItemPrefab constructor is called.
	/// </summary>
	public virtual void Initialize() { }

	/// <summary>
	/// Called when the ItemPrefab is destroyed.
	/// </summary>
	public virtual void OnDestroy() { }

	/// <summary>
	/// Called when the ItemPrefab SceneObject's model changes. 
	/// </summary>
	/// <param name="model"></param>
	public virtual void OnNewModel( Model model ) { }
}
