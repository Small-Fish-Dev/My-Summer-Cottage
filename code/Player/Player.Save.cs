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

	/// <summary>
	/// Gets current local save data.
	/// </summary>
	/// <returns></returns>
	public static (PlayerSave Save, bool Has) GetSave()
	{
		if ( _saveData.HasValue )
			return (_saveData.Value, true);

		if ( !FileSystem.Data.FileExists( PlayerSave.FILE_PATH ) )
			return (default, false);

		_saveData = FileSystem.Data.ReadJson<PlayerSave>( PlayerSave.FILE_PATH );

		return (_saveData.Value, true);
	}

	/// <summary>
	/// Writes a pure PlayerSave struct into a local save.
	/// </summary>
	/// <param name="save"></param>
	public static void WriteSave( PlayerSave save )
		=> FileSystem.Data.WriteJson( PlayerSave.FILE_PATH, save );

	/// <summary>
	/// Writes a local save based on player or local.
	/// </summary>
	/// <param name="player"></param>
	public static void Save( Player player = null )
	{
		player ??= Local;

		// Get the data that sticks.
		var data = GetSave();
		var save = data.Has 
			? data.Save 
			: new PlayerSave()
			{
				Firstname = player.Firstname,
				Lastname = player.Lastname,
				Fatness = player.Fatness,
				Height = player.Height,
				SkinColor= player.SkinColor
			};

		// Save dynamic data.
		ItemSave Serialize( ItemComponent item )
		{
			return default; // TODO @ceitine: figure out way to fetch prefab path, add attribute to store data, go through all components and check if any property has that attribute
		}

		_saveData = save with
		{
			Money = player.Money,

			Clothes = player.Inventory.EquippedItems
				.Select( x => Serialize( x ) )
				.ToArray(),

			Inventory = player.Inventory.BackpackItems
				.Select( x => Serialize( x ) )
				.ToArray()
		};

		// Write save.
		WriteSave( _saveData.Value );
	}

	/// <summary>
	/// Sets up everything for a player or local from local save.
	/// </summary>
	/// <param name="player"></param>
	/// <returns></returns>
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
