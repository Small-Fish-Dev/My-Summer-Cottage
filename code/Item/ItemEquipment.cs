namespace Sauna;

public enum EquipSlot : byte
{
	Head,
	Body,
	Legs,
	Feet,
	Hand
}

public class ItemEquipment : Component
{
	[Property] public EquipSlot Slot { get; set; }

	private bool _equipped = false;
	public bool Equipped
	{
		get => _equipped;
		set
		{
			_equipped = value;

			var renderer = Components.Get<SkinnedModelRenderer>( FindMode.InSelf ) ?? Components.Get<ModelRenderer>( FindMode.InSelf );
			renderer.Enabled = value;

			if ( renderer is SkinnedModelRenderer skinned )
			{
				skinned.BoneMergeTarget = value
					? GameObject.Parent?.Components.Get<SkinnedModelRenderer>( FindMode.EverythingInChildren )
					: null;
			}
		}
	}
}
