namespace Sauna;

public struct ItemSave
{
	[JsonInclude] public string Path;
	[JsonInclude] public Dictionary<string, string> Data;
	[JsonInclude] public int Index;
}

public struct PlayerSave
{
	public const string FILE_PATH = "saunasona.json";

	[JsonInclude] public string Firstname;
	[JsonInclude] public string Lastname;

	[JsonInclude] public int Money;
	[JsonInclude] public int Experience;
	[JsonInclude] public int Level;

	[JsonInclude] public float Height;
	[JsonInclude] public float Fatness;

	[JsonInclude] public Color SkinColor;

	[JsonInclude] public ItemSave[] Clothes;
	[JsonInclude] public ItemSave[] Inventory;

	[JsonInclude] public Dictionary<string, FishRecord> FishesCaught;
}

[AttributeUsage( AttributeTargets.Property )]
public class TargetSaveAttribute : Attribute
{
}

partial class Player
{
	private static readonly JsonSerializerOptions options = new JsonSerializerOptions()
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	};

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
				SkinColor = player.SkinColor
			};

		var items = PrefabLibrary.FindByComponent<ItemComponent>();

		// Save dynamic data.
		ItemSave Serialize( ItemComponent item )
		{
			if ( item == null )
				return default;

			if ( !ResourceLibrary.TryGet<PrefabFile>( item.Prefab, out var resource ) )
				return default;

			var data = new Dictionary<string, string>();
			foreach ( var component in item.Components.GetAll() )
			{
				var properties = GlobalGameNamespace.TypeLibrary
					?.GetType( component.GetType() )
					?.Properties
					?.Where( x => x.HasAttribute<TargetSaveAttribute>() );

				foreach ( var property in properties )
				{
					var serialized = JsonSerializer.Serialize( property.GetValue( component ), property.PropertyType, options );
					if ( data.ContainsKey( property.Name ) )
						data[property.Name] = serialized;
					else
						data.Add( property.Name, serialized );
				}
			}

			return new ItemSave
			{
				Path = item.Prefab, Data = data.Count > 0 ? data : null, Index = player.Inventory.IndexOf( item )
			};
		}

		_saveData = save with
		{
			Money = player.Money,
			Experience = player.Experience,
			Level = player.Level,
			Clothes = player.Inventory.EquippedItems
				.Where( x => x != null )
				.Select( Serialize )
				.ToArray(),
			Inventory = player.Inventory.BackpackItems
				.Where( x => x != null )
				.Select( Serialize )
				.ToArray(),
			FishesCaught = player.FishesCaught
		};

		// Write save.
		WriteSave( _saveData.Value );
	}

	[ConCmd]
	public static void SaveGame()
	{
		Save();
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
		player.Experience = save.Experience;
		player.Level = save.Level;

		player.Fatness = save.Fatness;
		player.Height = save.Height;

		player.SkinColor = save.SkinColor;

		void ReadData( ItemSave data, GameObject obj )
		{
			var components = obj.Components.GetAll();
			foreach ( var component in components )
			{
				var properties = GlobalGameNamespace.TypeLibrary
					?.GetType( component.GetType() )
					?.Properties
					?.Where( x => x.HasAttribute<TargetSaveAttribute>() );

				foreach ( var property in properties )
				{
					if ( data.Data == null || !data.Data.TryGetValue( property.Name, out var serialized ) )
						continue;

					var deserialized = JsonSerializer.Deserialize( serialized, property.PropertyType, options );
					property.SetValue( component, deserialized );
				}
			}
		}

		// Go through all clothes.
		if ( save.Clothes != null )
			foreach ( var data in save.Clothes )
			{
				if ( !ResourceLibrary.TryGet<PrefabFile>( data.Path, out var prefab ) )
					continue;

				var o = SceneUtility.GetPrefabScene( prefab ).Clone();
				o.NetworkSpawn();
				var equipment = o.Components.Get<ItemEquipment>();
				if ( equipment == null )
					continue;

				player.Inventory.EquipItemFromWorld( equipment );
				ReadData( data, o );
			}

		// Go through all items.
		if ( save.Inventory != null )
			foreach ( var data in save.Inventory )
			{
				if ( !ResourceLibrary.TryGet<PrefabFile>( data.Path, out var prefab ) )
					continue;

				var o = SceneUtility.GetPrefabScene( prefab ).Clone();
				o.NetworkSpawn();
				var item = o.Components.Get<ItemComponent>();
				if ( item == null )
					continue;

				player.Inventory.SetItem( item, data.Index );
				ReadData( data, o );
			}

		if ( save.FishesCaught != null )
			player.FishesCaught = save.FishesCaught.Where( kv => PrefabLibrary
					.TryGetByPath( kv.Key, out _ ) )
				.ToDictionary( kv => kv.Key, kv => kv.Value );

		return true;
	}
}
