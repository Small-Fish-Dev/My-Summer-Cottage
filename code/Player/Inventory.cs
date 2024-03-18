using Sauna.Event;
using System.Linq;

namespace Sauna;

public class Inventory : Component
{
	[Property] public Player Player { get; set; }

	public const int MAX_BACKPACK_SLOTS = 24;
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

	public ItemComponent GetItemInSlot( EquipSlot slot ) => _equippedItems.ElementAtOrDefault( (int)slot );
	public bool IsSlotOccupied( EquipSlot slot ) => GetItemInSlot( slot ) is not null;

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
	/// The item is created from the prefab file and given to the inventory system if they have free slots.
	/// </summary>
	public bool GiveItem( PrefabFile prefabFile )
	{
		var obj = SceneUtility.GetPrefabScene( prefabFile ).Clone();
		obj.NetworkMode = NetworkMode.Object;
		obj.NetworkSpawn();

		var res = GiveItem( obj.Components.Get<ItemComponent>() );
		if ( !res )
			obj.Destroy();

		return res;
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

		return true;
	}

	public bool EquipItemFromWorld( ItemComponent item, bool forceReplace = false )
	{
		if ( item is not ItemEquipment equipment )
			return false;

		if ( IsSlotOccupied( equipment.Slot ) && !forceReplace )
			return false;

		if ( IsSlotOccupied( equipment.Slot ) && forceReplace )
		{
			var equippedItem = GetItemInSlot( equipment.Slot );
			var placedInBackpack = UnequipItem( equippedItem );
			if ( !placedInBackpack )
				DropItem( equippedItem );
		}

		SetOwner( item );
		GiveEquipmentItem( equipment );
		equipment.State = ItemState.Equipped;
		TaskMaster.SubmitTriggerSignal( $"item.received.{item.Name}", Player );

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
		{
			rigidbody.Velocity = velocity;
			rigidbody.MotionEnabled = true;
		}
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

	public bool RemoveAmountEasy( string name, int count = 1, bool destroy = true )
	{
		foreach ( var item in _backpackItems )
			if ( item.Name.ToLower().Replace( " ", "" ) == name.ToLower().Replace( " ", "" ) )
				return RemoveAmount( item, count, destroy );

		return false;
	}

	/// <summary>
	/// Removes a specific amount from an item if the item is stackable and has more than the amount.
	/// </summary>
	/// <param name="item"></param>
	/// <param name="count"></param>
	/// <param name="destroy"></param>
	/// <param name="predicate"></param>
	/// <returns></returns>
	public bool RemoveAmount( ItemComponent item, int count = 1, bool destroy = true, Func<ItemComponent, bool> predicate = null )
	{
		// Non stackables.
		if ( !item.IsStackable )
		{
			var items = BackpackItems
				.Where( x => x != item && x?.Prefab == item?.Prefab && (predicate?.Invoke( x ) ?? true) )
				.ToList();

			if ( count > items.Count + 1 )
				return false;

			ClearItem( item );
			if ( destroy )
			{
				item.State = ItemState.None;
				item?.GameObject?.Destroy();
			}

			for ( int i = 0; i < count - 1; i++ )
			{
				var target = items[i];
				ClearItem( target );
				if ( destroy )
				{
					target.State = ItemState.None;
					target?.GameObject?.Destroy();
				}
			}

			return true;
		}

		// Stackables.
		if ( count > item.Count )
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
		{
			_backpackItems[_backpackItems.IndexOf( item )] = null;
			item.State = ItemState.None;
		}
		else if ( _equippedItems.Contains( item ) )
		{
			_equippedItems[_equippedItems.IndexOf( item )] = null;
			item.State = ItemState.None;
		}
	}

	public int GetTotalWeightInGrams()
	{
		return _backpackItems.Sum( i => i?.WeightInGrams ?? 0 ) + _equippedItems.Sum( i => i?.WeightInGrams ?? 0 );
	}

	private void SetOwner( ItemComponent item )
	{
		item.GameObject.SetupNetworking();
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
		TaskMaster.SubmitTriggerSignal( $"item.equipped.{equipment.Name}", Player );
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

	public int GetTotalItemCount( string name )
	{
		if ( BackpackItems == null ) return 0;
		return BackpackItems.Where( x => x.IsValid() && x.Name.ToLower() == name.ToLower() )?.Count() ?? 0;
	}

	public int GetTotalItemCountWithTag( string tag )
	{
		if ( BackpackItems == null ) return 0;
		return BackpackItems.Where( x => x.IsValid() && x.Tags.Has( tag ) )?.Count() ?? 0;
	}

	public bool HasItem( string name )
	{
		return BackpackItems.Any( x => x.Name == name );
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
		obj.NetworkMode = NetworkMode.Object;
		obj.NetworkSpawn();
		player.Inventory.GiveItem( obj );
	}



	[ConCmd( "sauna_item_give" )]
	public static void DebugGiveItem( string name )
	{
		var allItems = PrefabLibrary.FindByComponent<ItemComponent>();
		var foundItem = allItems.Where( itemPrefab =>
		{
			var toFind = name.ToLower().Replace( " ", "" ).Replace( "_", "" ).Replace( ".", "" );
			var item = itemPrefab.GetComponent<ItemComponent>();
			var itemName = item.Get<string>( "Name" ).ToLower().Replace( " ", "" ).Replace( "_", "" ).Replace( ".", "" );
			var objectName = itemPrefab.Name.ToLower().Replace( " ", "" ).Replace( "_", "" ).Replace( ".", "" );

			if ( itemName == toFind || objectName == toFind )
				return true;

			if ( itemName.Contains( toFind, StringComparison.OrdinalIgnoreCase ) || objectName.Contains( toFind, StringComparison.OrdinalIgnoreCase ) )
				return true;

			return false;
		} ).FirstOrDefault();


		if ( foundItem != null )
		{
			var obj = SceneUtility.GetPrefabScene( foundItem.Prefab ).Clone();
			obj.NetworkMode = NetworkMode.Object;
			obj.NetworkSpawn();
			Player.Local.Inventory.GiveItem( obj );
		}
		else
		{
			Log.Info( $"The item was not found, here is a list of available items:" );

			var availableItems = "";

			foreach ( var availableItem in allItems )
				availableItems += $"[{availableItem.GetComponent<ItemComponent>().Get<string>( "Name" )}], ";

			Log.Info( availableItems );
			Log.Info( "You may also use partial item names or any combination of words and letters, I'll try my best to find the item." );
		}
	}
}
