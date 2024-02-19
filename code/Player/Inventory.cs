namespace Sauna;

public class Inventory : Component
{
	[Property]
	public Player Player { get; set; }

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

		item.DrawingEnabled = false;
		item.GameObject.Parent = Player.GameObject;

		_backpackItems[firstFreeSlot] = item;
		return true;
	}

	public bool DropItem( ItemComponent item )
	{
		var droppedItem = RemoveItem( _backpackItems.IndexOf( item ) );
		if ( droppedItem is null )
			return false;

		droppedItem.DrawingEnabled = true;
		droppedItem.GameObject.Transform.Position = Player.ViewRay.Position;
		droppedItem.GameObject.Parent = null;

		return true;
	}

	public bool EquipItem( ItemComponent item )
	{
		var index = _backpackItems.IndexOf( item );
		if ( index == -1 )
			return false;

		var equipment = item.Components.Get<ItemEquipment>();

		if ( equipment is null )
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

		return true;
	}

	public bool UnequipItem( ItemComponent item )
	{
		var equipment = item.Components.Get<ItemEquipment>();
		if ( equipment is null )
			return false;

		var slotIndex = (int)equipment.Slot;
		var equippedItem = _equippedItems[slotIndex];
		var hasGivenItem = GiveItem( equippedItem );
		if ( !hasGivenItem )
			return false;

		equipment.Equipped = false;

		_equippedItems[slotIndex] = null;
		return true;
	}

	public void MoveItem( int startIndex, int endIndex )
	{
		if ( _backpackItems[endIndex] is not null )
		{
			// Perform a swap since we are moving the item to an already occupied index.
			(_backpackItems[startIndex], _backpackItems[endIndex]) = (_backpackItems[endIndex], _backpackItems[startIndex]);
		}
		else
		{
			_backpackItems[endIndex] = _backpackItems[startIndex];
			RemoveItem( startIndex );
		}
	}

	public int GetTotalWeightInGrams()
	{
		return _backpackItems.Sum( i => i?.WeightInGrams ?? 0 ) + _equippedItems.Sum( i => i?.WeightInGrams ?? 0 );
	}

	private ItemComponent RemoveItem( int index )
	{
		var item = _backpackItems.ElementAtOrDefault( index );
		if ( item is null )
			return null;

		_backpackItems[index] = null;
		return item;
	}
}
