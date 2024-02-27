namespace Sauna;

public enum EquipSlot : byte
{
	Head,
	Face,
	Body,
	Legs,
	Feet,
	Hand
}

public class ItemEquipment : ItemComponent
{
	[Property, Category( "Equipment" )] public EquipSlot Slot { get; set; }
	[Property, Category( "Equipment" )] public HiddenBodyGroup HideBodygroups { get; set; }

	public ModelRenderer Renderer { get; private set; }

	private bool _equipped = false;
	[Sync] public bool Equipped
	{
		get => _equipped;
		set
		{
			_equipped = value;

			Renderer = Components.Get<SkinnedModelRenderer>( FindMode.InSelf ) ?? Components.Get<ModelRenderer>( FindMode.InSelf );
			Renderer.Enabled = value;

			if ( Renderer is SkinnedModelRenderer skinned )
			{
				skinned.BoneMergeTarget = value
					? GameObject.Parent?.Components.Get<SkinnedModelRenderer>( FindMode.EverythingInChildren )
					: null;
			}
		}
	}

	// todo: base onstart, also create box model renderer and collider for world.
}
