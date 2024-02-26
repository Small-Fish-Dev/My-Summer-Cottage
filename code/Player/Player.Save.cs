namespace Sauna;

public struct ItemSave
{
	[JsonInclude] public string Path;
	[JsonInclude] public Dictionary<string, object> Data;
}

public struct PlayerSave
{
	public const string FILE_PATH = "saunasona.json";

	[JsonInclude] public string Firstname;
	[JsonInclude] public string Lastname;

	[JsonInclude] public int Money;

	[JsonInclude] public float Height;
	[JsonInclude] public float Fatness;

	[JsonInclude] public Color SkinColor;

	[JsonInclude] public ItemSave[] Clothes;
	[JsonInclude] public ItemSave[] Inventory;
}

partial class Player
{
	public static void Save( PlayerSave save )
		=> FileSystem.Data.WriteJson( PlayerSave.FILE_PATH, save );

	public static bool Load( Player player = null )
	{
		player ??= Local;

		if ( !FileSystem.Data.FileExists( PlayerSave.FILE_PATH ) )
			return false;

		var save = FileSystem.Data.ReadJson<PlayerSave>( PlayerSave.FILE_PATH );

		// Setup basic player information.
		player.Firstname = save.Firstname.ToLower().ToTitleCase();
		player.Firstname = save.Lastname.ToLower().ToTitleCase();

		player.Money = save.Money;

		player.Fatness = save.Fatness;
		player.Height = save.Height;

		player.SkinColor = save.SkinColor;

		// Go through all clothes.
		if ( save.Clothes != null )
			foreach ( var cloth in save.Clothes )
			{
				if ( !PrefabLibrary.TryGetByPath( cloth.Path, out var prefab ) )
					continue;

				var o = SceneUtility.GetPrefabScene( prefab.Prefab ).Clone();
				var equipment = o.Components.Get<ItemEquipment>();
				player.Inventory.GiveItem( equipment );
				player.Inventory.EquipItem( equipment );
				o.Enabled = true;
			}

		// todo @ceitine: Go through items.

		return true;
	}
}
