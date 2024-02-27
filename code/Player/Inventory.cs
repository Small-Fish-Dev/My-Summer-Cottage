namespace Sauna;

public class Inventory : Component
{
	[Property] public Player Player { get; set; }

	public const int MAX_BACKPACK_SLOTS = 18;
	public const int MAX_WEIGHT_IN_GRAMS = 30000;

	public IReadOnlyList<ItemComponent> BackpackItems => _backpackItems;
	public IReadOnlyList<ItemComponent> EquippedItems => _equippedItems;

	private readonly List<ItemComponent> _backpackItems;
	private readonly List<ItemComponent> _equippedItems;

	public Inventory()
	{
		_backpackItems = new List<ItemComponent>( new ItemComponent[MAX_BACKPACK_SLOTS] );
		_equippedItems = new List<ItemComponent>( new ItemComponent[Enum.GetNames( typeof( EquipSlot ) ).Length] );
	}

	public bool GiveItem( ItemComponent item )
	{
		var firstFreeSlot = _backpackItems.IndexOf( null );
		if ( firstFreeSlot == -1 )
			return false;

		item.GameObject.Network.TakeOwnership();
		item.DrawingEnabled = false;
		item.GameObject.Parent = Player.GameObject;

		_backpackItems[firstFreeSlot] = item;
		return true;
	}

	public void RemoveItem( ItemComponent item )
	{
		if ( _backpackItems.Contains( item ) )
			_backpackItems[_backpackItems.IndexOf( item )] = null;
		else if ( _equippedItems.Contains( item ) )
			_equippedItems[_equippedItems.IndexOf( item )] = null;
	}

	public bool DropItem( ItemComponent item )
	{
		var droppedItem = item is ItemEquipment equipment && equipment.Equipped ? RemoveItem( equipment.Slot ) : RemoveItem( _backpackItems.IndexOf( item ) );
		if ( droppedItem is null )
			return false;

		droppedItem.DrawingEnabled = true;
		droppedItem.GameObject.Parent = null;
		droppedItem.GameObject.Transform.Position = Player.ViewRay.Position + Player.ViewRay.Forward * 2f;
		droppedItem.Network.DropOwnership();

		return true;
	}

	public bool EquipItem( ItemComponent item )
	{
		var index = _backpackItems.IndexOf( item );
		if ( index == -1 )
			return false;

		if ( item is not ItemEquipment equipment )
			return false;

		equipment.Equipped = true;

		var slot = equipment.Slot;
		var slotIndex = (int)slot;

		var equippedItem = _equippedItems[slotIndex];
		if ( equippedItem is not null )
		{
			_backpackItems[index] = equippedItem;
			_equippedItems[slotIndex] = item;
		}
		else
		{
			RemoveItem( index );
			_equippedItems[slotIndex] = item;
		}

		UpdateBodygroups();

		return true;
	}

	public void UpdateBodygroups()
	{
		var bodygroups = HiddenBodyGroup.None;
		foreach ( var item in EquippedItems )
		{
			if ( item is not ItemEquipment equipment || !equipment.Equipped )
				continue;

			bodygroups |= equipment.HideBodygroups;
		}

		Player.HideBodygroups = bodygroups;
	}

	public bool UnequipItem( ItemComponent item )
	{
		if ( item is not ItemEquipment equipment )
			return false;

		var slotIndex = (int)equipment.Slot;
		var equippedItem = _equippedItems[slotIndex];
		var hasGivenItem = GiveItem( equippedItem );
		if ( !hasGivenItem )
			return false;

		equipment.Equipped = false;

		_equippedItems[slotIndex] = null;

		UpdateBodygroups();

		return true;
	}

	public void MoveItem( int startIndex, int endIndex )
	{
		if ( _backpackItems[endIndex] is not null )
		{
			// Perform a swap since we are moving the item to an already occupied index.
			(_backpackItems[startIndex], _backpackItems[endIndex]) =
				(_backpackItems[endIndex], _backpackItems[startIndex]);
		}
		else
		{
			_backpackItems[endIndex] = _backpackItems[startIndex];
			RemoveItem( startIndex );
		}
	}

	public void MoveItem( EquipSlot startIndex, int endIndex )
	{
		if ( _backpackItems[endIndex] is not null )
		{
			// Perform a swap since we are moving the item to an already occupied index.
			(_equippedItems[(int)startIndex], _backpackItems[endIndex]) =
				(_backpackItems[endIndex], _equippedItems[(int)startIndex]);
		}
		else
		{
			_backpackItems[endIndex] = _equippedItems[(int)startIndex];
			RemoveItem( startIndex );
		}
	}

	public void MoveItem( int startIndex, EquipSlot endIndex )
	{
		if ( _equippedItems[(int)endIndex] is not null )
		{
			// Perform a swap since we are moving the item to an already occupied index.
			(_backpackItems[startIndex], _equippedItems[(int)endIndex]) =
				(_equippedItems[(int)endIndex], _backpackItems[startIndex]);
		}
		else
		{
			_equippedItems[(int)endIndex] = _backpackItems[startIndex];
			RemoveItem( startIndex );
		}
	}

	public void MoveItem( EquipSlot startIndex, EquipSlot endIndex )
	{
		if ( _equippedItems[(int)endIndex] is not null )
		{
			// Perform a swap since we are moving the item to an already occupied index.
			(_equippedItems[(int)startIndex], _equippedItems[(int)endIndex]) =
				(_equippedItems[(int)endIndex], _equippedItems[(int)startIndex]);
		}
		else
		{
			_equippedItems[(int)endIndex] = _equippedItems[(int)startIndex];
			RemoveItem( startIndex );
		}
	}

	public int GetTotalWeightInGrams()
	{
		return _backpackItems.Sum( i => i?.WeightInGrams ?? 0 ) + _equippedItems.Sum( i => i?.WeightInGrams ?? 0 );
	}

	public ItemComponent RemoveItem( int index )
	{
		var item = _backpackItems.ElementAtOrDefault( index );
		if ( item is null )
			return null;

		_backpackItems[index] = null;
		return item;
	}

	public ItemComponent RemoveItem( EquipSlot index )
	{
		var item = _equippedItems.ElementAtOrDefault( (int)index );
		if ( item is null )
			return null;

		_equippedItems[(int)index] = null;
		return item;
	}

	[ConCmd]
	public static void UnEquip( int index )
	{
		var player = GameManager.ActiveScene.GetAllComponents<Player>().FirstOrDefault();
		player.Inventory.UnequipItem( player.Inventory.EquippedItems[(int)EquipSlot.Body] );
	}

	[ConCmd]
	public static void GiveItem( string name )
	{
		var player = GameManager.ActiveScene.GetAllComponents<Player>().FirstOrDefault();
		if ( player == null )
			return;

		var item = PrefabLibrary.FindByComponent<ItemComponent>()
			.FirstOrDefault( x => x.Name.ToLower() == name.ToLower() )
			?.Prefab;

		if ( item == null )
		{
			var itemNames = string.Join( ',', PrefabLibrary.FindByComponent<ItemComponent>().Select( v => v.Name ) );
			Log.Warning( $"couldn't find {name}" );
			Log.Warning( $"valid item names: {itemNames}" );
			return;
		}

		var obj = SceneUtility.GetPrefabScene( item ).Clone();
		player.Inventory.GiveItem( obj );
	}
}
