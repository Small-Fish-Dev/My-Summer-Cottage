namespace Sauna;

public class Inventory : Component
{
	[Property] public Player Player { get; set; }

	public const int MAX_BACKPACK_SLOTS = 18;
	public const int MAX_WEIGHT_IN_GRAMS = 30000;

	public int Weight { get; private set; }
	public IReadOnlyList<ItemComponent> BackpackItems => _backpackItems;
	public IReadOnlyList<ItemComponent> EquippedItems => _equippedItems;

	private readonly List<ItemComponent> _backpackItems;
	private readonly List<ItemComponent> _equippedItems;

	public Inventory()
	{
		_backpackItems = new List<ItemComponent>( new ItemComponent[MAX_BACKPACK_SLOTS] );
		_equippedItems = new List<ItemComponent>( new ItemComponent[Enum.GetNames( typeof( EquipSlot ) ).Length] );
	}

	/// <summary>
	/// Returns the index of an item regardless of whether it is equipped or in the backpack.
	/// </summary>
	public int IndexOf( ItemComponent item )
		=> (item is ItemEquipment equipment && equipment.Equipped ? _equippedItems : _backpackItems).IndexOf( item );

	/// <summary>
	/// Returns true if the backpack has any free slots.
	/// </summary>
	/// <returns></returns>
	public bool HasSpaceInBackpack()
		=> _backpackItems.IndexOf( null ) != -1;

	public bool IsSlotOccupied( EquipSlot slot ) => _equippedItems.ElementAtOrDefault( (int)slot ) is not null;

	/// <summary>
	/// Item is given to the inventory system if they have free slots.
	/// </summary>
	public bool GiveItem( ItemComponent item )
	{
		var firstFreeSlot = _backpackItems.IndexOf( null );
		if ( firstFreeSlot == -1 )
			return false;

		SetOwner( item );
		GiveBackpackItem( item, firstFreeSlot );
		item.State = ItemState.Backpack;
		TaskMaster.SubmitTriggerSignal( $"item.received.{item.Name}", Player );

		return true;
	}

	/// <summary>
	/// The item is equipped from the backpack and swaps out any previously equipped item.
	/// </summary>
	public bool EquipItemFromBackpack( ItemComponent item )
	{
		var index = _backpackItems.IndexOf( item );
		if ( index == -1 )
			return false;

		if ( item is not ItemEquipment equipment )
			return false;

		RemoveBackpackItem( item, index );

		var slotIndex = (int)equipment.Slot;
		var previouslyEquippedItem = _equippedItems[slotIndex];

		if ( previouslyEquippedItem is not null )
		{
			RemoveEquipmentItem( previouslyEquippedItem as ItemEquipment );
			GiveBackpackItem( previouslyEquippedItem, index );
			previouslyEquippedItem.State = ItemState.Backpack;
		}

		GiveEquipmentItem( equipment );
		equipment.State = ItemState.Equipped;
		TaskMaster.SubmitTriggerSignal( $"item.equipped.{item.Name}", Player );

		return true;
	}

	public bool EquipItemFromWorld( ItemComponent item )
	{
		if ( item is not ItemEquipment equipment )
			return false;

		if ( IsSlotOccupied( equipment.Slot ) )
			return false;

		SetOwner( item );
		GiveEquipmentItem( equipment );
		equipment.State = ItemState.Equipped;
		TaskMaster.SubmitTriggerSignal( $"item.received.{item.Name}", Player );
		TaskMaster.SubmitTriggerSignal( $"item.equipped.{item.Name}", Player );

		return true;
	}

	/// <summary>
	/// The item is unequipped and placed into the backpack if there are free slots. 
	/// </summary>
	public bool UnequipItem( ItemComponent item )
	{
		if ( item is not ItemEquipment equipment || !equipment.Equipped )
			return false;

		var firstFreeSlot = _backpackItems.IndexOf( null );
		if ( firstFreeSlot == -1 )
			return false;

		RemoveEquipmentItem( equipment );
		GiveBackpackItem( equipment, firstFreeSlot );
		equipment.State = ItemState.Backpack;
		TaskMaster.SubmitTriggerSignal( $"item.unequipped.{item.Name}", Player );

		return true;
	}

	/// <summary>
	/// The item is removed completely from the inventory system.
	/// </summary>
	public bool DropItem( ItemComponent item )
	{
		if ( item is ItemEquipment equipment && equipment.Equipped )
			RemoveEquipmentItem( equipment );
		else
			RemoveBackpackItem( item, _backpackItems.IndexOf( item ) );

		item.State = ItemState.None;

		TaskMaster.SubmitTriggerSignal( $"item.dropped.{item.Name}", Player );

		item.GameObject.Parent = null;

		var trace = Scene.Trace.FromTo( Player.ViewRay.Position + Player.ViewRay.Forward, Player.ViewRay.Position + Player.ViewRay.Forward * 20f )
				.IgnoreGameObject( Player.GameObject )
				.IgnoreGameObject( item.GameObject )
				.Radius( 1.0f )
				.Run();

		item.GameObject.Transform.Rotation = Rotation.Identity;
		item.GameObject.Transform.Position = trace.EndPosition;

		var velocity = Player.Velocity + Player.ViewRay.Forward * 150f;
		if ( item.GameObject.Components.TryGet<Rigidbody>( out var rigidbody, FindMode.EverythingInSelf ) )
			rigidbody.Velocity = velocity;
		else if ( item.GameObject.Components.TryGet<ModelPhysics>( out var modelPhysics, FindMode.EverythingInSelf ) )
		{
			item.GameObject.Enabled = false;
			item.GameObject.Transform.Position = trace.EndPosition;
			item.GameObject.Enabled = true;
			modelPhysics.PhysicsGroup?.AddVelocity( velocity ); // todo: LOL WHY??
		}

		return true;
	}

	/// <summary>
	/// A swap is performed from the backpack to the equipment slots.
	/// </summary>
	public bool SwapItems( int index, EquipSlot slot )
	{
		var item = _backpackItems.ElementAtOrDefault( index );
		if ( item is null )
			return false;

		if ( item is not ItemEquipment equipment || equipment.Slot != slot )
			return false;

		RemoveBackpackItem( item, index );

		var previouslyEquippedItem = _equippedItems[(int)slot];
		if ( previouslyEquippedItem is not null )
		{
			RemoveEquipmentItem( previouslyEquippedItem as ItemEquipment );
			GiveBackpackItem( previouslyEquippedItem, index );
			previouslyEquippedItem.State = ItemState.Backpack;
		}

		GiveEquipmentItem( equipment );
		equipment.State = ItemState.Equipped;

		return true;
	}

	private bool CanStack( ItemComponent first, ItemComponent second )
		=> first.Prefab == second.Prefab
		&& first.IsStackable && second.IsStackable
		&& second.Count < second.MaxStack;

	/// <summary>
	/// A swap is performed from a backpack slot to another backpack slot.
	/// </summary>
	public bool SwapItems( int firstIndex, int secondIndex )
	{
		var firstItem = _backpackItems.ElementAtOrDefault( firstIndex );
		if ( firstItem is null )
			return false;

		RemoveBackpackItem( firstItem, firstIndex );

		var secondItem = _backpackItems.ElementAtOrDefault( secondIndex );
		var invert = false;

		if ( secondItem is not null )
		{
			RemoveBackpackItem( secondItem, secondIndex );

			// Stacking
			if ( CanStack( firstItem, secondItem ) )
			{
				var from = firstItem;
				var to = secondItem;
				invert = true;

				var amount = Math.Min( Math.Abs( to.Count - to.MaxStack ), from.Count );
				RemoveAmount( from, amount );
				to.Count += amount;

				if ( from == null || from.Count <= 0 )
				{
					GiveBackpackItem( to, secondIndex );
					return true;
				}
			}
			else if ( CanStack( secondItem, firstItem ) )
			{
				var from = secondItem;
				var to = firstItem;
				invert = true;

				var amount = Math.Min( Math.Abs( to.Count - to.MaxStack ), from.Count );
				RemoveAmount( from, amount );
				to.Count += amount;

				if ( from == null || from.Count <= 0 )
				{
					GiveBackpackItem( to, secondIndex );
					return true;
				}
			}

			GiveBackpackItem( secondItem, invert ? secondIndex : firstIndex );
		}

		GiveBackpackItem( firstItem, invert ? firstIndex : secondIndex );

		return true;
	}

	/// <summary>
	/// A swap is performed from the equipment slot to the backpack.
	/// </summary>
	public bool SwapItems( EquipSlot slot, int index )
	{
		var item = _equippedItems[(int)slot];
		if ( item is null )
			return false;

		var previousBackpackItem = _backpackItems[index];
		if ( previousBackpackItem is not null && (previousBackpackItem is not ItemEquipment itemToEquip || slot != itemToEquip.Slot) )
			return false;

		RemoveEquipmentItem( item as ItemEquipment );

		if ( previousBackpackItem is not null )
		{
			RemoveBackpackItem( previousBackpackItem, index );
			GiveEquipmentItem( previousBackpackItem as ItemEquipment );
			previousBackpackItem.State = ItemState.Equipped;
		}

		GiveBackpackItem( item, index );
		item.State = ItemState.Backpack;

		return true;
	}

	/// <summary>
	/// Bypasses any restrictions and sets the item at the given index. Do not use this for regular inventory usage.
	/// </summary>
	public void SetItem( ItemComponent item, int index )
	{
		SetOwner( item );
		GiveBackpackItem( item, index );
		item.State = ItemState.Backpack;
	}

	/// <summary>
	/// Removes a specific amount from an item if the item is stackable and has more than the amount.
	/// </summary>
	/// <param name="item"></param>
	/// <param name="count"></param>
	/// <param name="destroy"></param>
	/// <returns></returns>
	public bool RemoveAmount( ItemComponent item, int count = 1, bool destroy = true )
	{
		if ( !item.IsStackable || count > item.Count )
			return false;

		item.Count -= count;

		if ( item.Count <= 0 )
		{
			ClearItem( item );
			if ( destroy )
			{
				item.State = ItemState.None;
				item?.GameObject?.Destroy();
			}
		}

		Weight = GetTotalWeightInGrams();

		return true;
	}

	/// <summary>
	/// Bypasses any restrictions and clears the item. Do not use this for regular inventory usage.
	/// </summary>
	public void ClearItem( ItemComponent item )
	{
		if ( _backpackItems.Contains( item ) )
			_backpackItems[_backpackItems.IndexOf( item )] = null;
		else if ( _equippedItems.Contains( item ) )
			_equippedItems[_equippedItems.IndexOf( item )] = null;

		item.State = ItemState.None;
	}

	public int GetTotalWeightInGrams()
	{
		return _backpackItems.Sum( i => i?.WeightInGrams ?? 0 ) + _equippedItems.Sum( i => i?.WeightInGrams ?? 0 );
	}

	private void SetOwner( ItemComponent item )
	{
		item.GameObject.Network.TakeOwnership();
		item.GameObject.Parent = Player.GameObject;
		item.GameObject.Transform.Position = Player.GameObject.Transform.Position;
		item.GameObject.Transform.Rotation = Player.GameObject.Transform.Rotation;
		item.LastOwner = Player;
	}

	/// <summary>
	/// The item is given to the backpack.
	/// </summary>
	private void GiveBackpackItem( ItemComponent item, int index )
	{
		if ( index >= 0 && index < _backpackItems.Count )
			_backpackItems[index] = item;

		Weight = GetTotalWeightInGrams();
	}

	/// <summary>
	/// The item is removed from the backpack.
	/// </summary>
	private void RemoveBackpackItem( ItemComponent item, int index )
	{
		if ( index >= 0 && index < _backpackItems.Count )
			_backpackItems[index] = null;

		Weight = GetTotalWeightInGrams();
	}

	/// <summary>
	/// The item is equipped.
	/// </summary>
	private void GiveEquipmentItem( ItemEquipment equipment )
	{
		_equippedItems[(int)equipment.Slot] = equipment;
		UpdateBodygroups();

		Weight = GetTotalWeightInGrams();
	}

	/// <summary>
	/// The item is unequipped.
	/// </summary>
	private void RemoveEquipmentItem( ItemEquipment equipment )
	{
		_equippedItems[(int)equipment.Slot] = null;
		UpdateBodygroups();

		Weight = GetTotalWeightInGrams();
	}

	private void UpdateBodygroups()
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


	[ConCmd]
	public static void UnEquip( int index )
	{
		var player = Game.ActiveScene.GetAllComponents<Player>().FirstOrDefault();
		player.Inventory.UnequipItem( player.Inventory.EquippedItems[(int)EquipSlot.Body] );
	}

	// todo @ceitine: remove debug command
	[ConCmd]
	public static void GiveItem( string name )
	{
		var player = Player.Local;
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
		obj.NetworkSpawn();
		player.Inventory.GiveItem( obj );
	}
}
