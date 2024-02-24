namespace Sauna;

public static class EnumExtensions
{
	public static string GetIcon( this EquipSlot slot )
	{
		var path = "/ui/hud/" + slot switch
		{
			EquipSlot.Head => "clothes_face.png",
			EquipSlot.Face => "clothes_face.png",
			EquipSlot.Body => "clothes_torso.png",
			EquipSlot.Legs => "clothes_legs.png",
			EquipSlot.Feet => "clothes_boots.png",
			EquipSlot.Hand => "hand_slot.png",
			_ => ""
		};

		return path;
	}
}
