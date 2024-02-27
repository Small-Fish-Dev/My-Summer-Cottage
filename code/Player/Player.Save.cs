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
	private static PlayerSave? _saveData;

	public static (PlayerSave Save, bool Has) GetSave()
	{
		if ( _saveData.HasValue )
			return (_saveData.Value, true);

		if ( !FileSystem.Data.FileExists( PlayerSave.FILE_PATH ) )
			return (default, false);

		_saveData = FileSystem.Data.ReadJson<PlayerSave>( PlayerSave.FILE_PATH );

		return (_saveData.Value, true);
	}

	public static void Save( PlayerSave save )
		=> FileSystem.Data.WriteJson( PlayerSave.FILE_PATH, save );

	public static bool Setup( Player player = null )
	{
		player ??= Local;

		var tuple = GetSave();
		if ( !tuple.Has )
			return false;

		// Setup basic player information.
		var save = tuple.Save;

		player.Firstname = save.Firstname.ToLower().ToTitleCase();
		player.Lastname = save.Lastname.ToLower().ToTitleCase();

		player.Money = save.Money;

		player.Fatness = save.Fatness;
		player.Height = save.Height;

		player.SkinColor = save.SkinColor;

		// Go through all clothes.
		if ( save.Clothes != null )
			foreach ( var cloth in save.Clothes )
			{
				if ( !ResourceLibrary.TryGet<PrefabFile>( cloth.Path, out var prefab ) )
					continue;

				var o = SceneUtility.GetPrefabScene( prefab ).Clone();
				o.Enabled = true;
				var equipment = o.Components.Get<ItemEquipment>();
				player.Inventory.GiveItem( equipment );
				player.Inventory.EquipItem( equipment );
				o.NetworkSpawn();
			}

		// todo @ceitine: Go through items.

		return true;
	}
}
