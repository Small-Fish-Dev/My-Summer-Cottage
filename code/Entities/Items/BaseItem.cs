namespace Sauna;

[Prefab, Title( "Item Entity Prefab" )]
public partial class BaseItem : AnimatedEntity, IInteractable
{
	/// <summary>
	/// The displayed title of this item.
	/// </summary>
	[Prefab, Category( "Interaction" ), Net]
	public string Title { get; set; } = "Item";

	/// <summary>
	/// The displayed color of this item.
	/// </summary>
	[Prefab, Category( "Interaction" ), Net]
	public Color Color { get; set; } = Color.Orange;

	/// <summary>
	/// The amount that the displayed interaction is offset.
	/// </summary>
	[Prefab, Category( "Interaction" ), Net]
	public Vector3 TitleOffset { get; set; } = Vector3.Zero;

	string IInteractable.DisplayTitle => Title;
	Color IInteractable.DisplayColor => Color;
	InteractionOffset IInteractable.Offset => TitleOffset;

	// On spawned.
	public override void Spawn()
	{
		// Initialize components on server.
		var components = Components.GetAll<ItemComponent>();
		foreach ( var component in components )
			component.Initialize();
	}

	public override void ClientSpawn()
	{
		// Initialize components on client.
		var components = Components.GetAll<ItemComponent>();
		foreach ( var component in components )
			component.Initialize();
	}

	// On delete.
	protected override void OnDestroy()
	{
		// Call on destroy for all components.
		var components = Components.GetAll<ItemComponent>();
		foreach ( var component in components )
			component.OnDestroy();
	}

	// On model change.
	public override void OnNewModel( Model model )
	{
		base.OnNewModel( model );

		// Call on new model for all components.
		var components = Components.GetAll<ItemComponent>();
		foreach ( var component in components )
			component.OnNewModel( model );
	}
}
