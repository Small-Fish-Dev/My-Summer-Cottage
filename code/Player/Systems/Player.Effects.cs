namespace Sauna;

public enum EffectType
{
	Stack,
	Duration,
	Add,
	Remove
}

public class EffectManager
{
	Player player;
	int payloadCount;

	/// <summary>
	/// List of all the effects that the player has.
	/// </summary>
	public List<BaseEffect> All = new();

	public EffectManager( Player player )
	{
		this.player = player;
	}

	private void writeStack( BinaryWriter writer, BaseEffect effect )
	{
		writer.Write( (int)EffectType.Stack );

		var index = All.IndexOf( effect );
		writer.Write( index );
		writer.Write( effect.Stacks );

		payloadCount++;
	}

	private void writeDuration( BinaryWriter writer, BaseEffect effect )
	{
		writer.Write( (int)EffectType.Duration );

		var index = All.IndexOf( effect );
		writer.Write( index );
		writer.Write( effect.Duration );
		writer.Write( effect.Permanent );

		payloadCount++;
	}

	private void writeAdd( BinaryWriter writer, BaseEffect effect )
	{
		writer.Write( (int)EffectType.Add );

		var index = All.IndexOf( effect );
		writer.Write( index );
		writer.Write( effect.GetType().FullName );
		writer.Write( effect.Duration );
		writer.Write( effect.Stacks );
		writer.Write( effect.Permanent );

		payloadCount++;
	}

	private void writeRemove( BinaryWriter writer, BaseEffect effect )
	{
		writer.Write( (int)EffectType.Remove );

		var index = All.IndexOf( effect );
		writer.Write( index );

		payloadCount++;
	}

	private void sendPayload( MemoryStream dataStream )
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter( stream );
		writer.Write( payloadCount );
		writer.Write( dataStream.ToArray() );

		// Send the data as an array of bytes.
		Player.EffectUpdate( To.Single( player ), stream.ToArray() );
		payloadCount = 0;
	}

	/// <summary>
	/// Removes an effect of specific type.
	/// Should only be called from server.
	/// </summary>
	/// <param name="effect"></param>
	public void Remove( BaseEffect? effect )
	{
		Game.AssertServer();

		if ( effect == null )
			return;

		using var stream = new MemoryStream();
		using var writer = new BinaryWriter( stream );

		writeRemove( writer, effect );
		sendPayload( stream );

		effect.OnEnd( player );

		All.Remove( effect );
		effect = null;
	}

	/// <summary>
	/// Removes an effect of type T.
	/// </summary>
	public void Remove<T>() where T : BaseEffect
		=> Remove( Get<T>() );

	/// <summary>
	/// Get effect of type T.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public T? Get<T>() where T : BaseEffect
		=> Get( typeof( T ) ) as T;

	/// <summary>
	/// Get effect of specific type.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public BaseEffect? Get( Type type )
		=> All.FirstOrDefault( effect => effect.GetType() == type );

	/// <summary>
	/// Applies an effect to the player. 
	/// Should only be called from server.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="duration"></param>
	/// <param name="stacks"></param>
	/// <param name="permanent"></param>
	/// <returns></returns>
	public T Apply<T>( float duration = 1f, int stacks = 1, bool permanent = false ) where T : BaseEffect
	{
		return Apply( typeof( T ), duration, stacks, permanent ) as T;
	}

	/// <summary>
	/// Applies an effect of specific type to a player.
	/// Should only be called from server.
	/// </summary>
	/// <param name="type"></param>
	/// <param name="duration"></param>
	/// <param name="stacks"></param>
	/// <param name="permanent"></param>
	/// <returns></returns>
	public BaseEffect Apply( Type type, float duration = 1f, int stacks = 1, bool permanent = false )
	{
		Game.AssertServer();

		using var stream = new MemoryStream();
		using var writer = new BinaryWriter( stream );
		var time = Time.Now;

		BaseEffect effect = null;

		while ( true ) // Just loop.
		{
			// Check for stacking.
			var existing = All.FirstOrDefault( effect => effect?.GetType() == type );

			if ( existing != null )
			{
				// Add time to existing.
				existing.Duration = existing.MaxDuration == 0f 
					? existing.Duration + duration
					: Math.Min( existing.Duration + duration, existing.MaxDuration );
				existing.Permanent = permanent;
				writeDuration( writer, existing );

				// Add stacks to existing.
				existing.Stacks = Math.Min( existing.Stacks + stacks, existing.MaxStacks );
				writeStack( writer, existing );

				break;
			}

			// Create new effect.
			effect = TypeLibrary.Create( type.FullName, type ) as BaseEffect;
			effect.Duration = effect.MaxDuration == 0f
				? duration
				: Math.Min( duration, effect.MaxDuration );
			effect.Stacks = stacks;
			effect.Permanent = permanent;

			All.Add( effect );
			writeAdd( writer, effect );

			break;
		}

		// Send the payload.
		sendPayload( stream );
		return effect;
	}
}

partial class Player
{
	public EffectManager Effects { get; private set; }

	[SaunaEvent.OnSpawn]
	private static void createEffectManager( Player player )
	{	
		// Only server and local player need to manage the effects.
		if ( Game.IsServer || player == Game.LocalPawn )
			player.Effects = new( player );
	}

	private void effectSimulate( IClient cl )
	{
		for ( int i = 0; i < Effects.All.Count; i++ )
		{
			var effect = Effects.All[i];
			if ( effect == null ) 
				continue;

			if ( !effect.Permanent )
			{
				if ( effect.Duration <= 0 )
				{
					if ( Game.IsServer )
						Effects.Remove( effect );

					continue;
				}

				effect.Duration -= Time.Delta;
			}

			effect.Simulate( this );
		}
	}

	[ClientRpc]
	public static void EffectUpdate( byte[] data )
	{
		if ( Game.LocalPawn is not Player pawn )
			return;

		using var stream = new MemoryStream( data );
		using var reader = new BinaryReader( stream );

		var payloadCount = reader.ReadInt32();
		for ( int i = 0; i < payloadCount; i++ )
		{
			var type = (EffectType)reader.ReadInt32();
			var index = reader.ReadInt32();
			var effect = pawn.Effects?.All.ElementAtOrDefault( index );

			if ( effect == null && type != EffectType.Add )
				continue;

			switch ( type )
			{
				case EffectType.Stack:
					var stacks = reader.ReadInt32();
					effect.Stacks = stacks;

					break;

				case EffectType.Duration:
					var duration = reader.ReadSingle();
					var permanent = reader.ReadBoolean();
					effect.Duration = duration;
					effect.Permanent = permanent;

					break;

				case EffectType.Add:
					var typeName = reader.ReadString();
					var newDuration = reader.ReadSingle();
					var newStacks = reader.ReadInt32();
					var newPermanent = reader.ReadBoolean();

					var newEffect = TypeLibrary.Create( typeName, typeof( BaseEffect ) ) as BaseEffect;
					newEffect.Duration = newDuration;
					newEffect.Stacks = newStacks;
					newEffect.Permanent = newPermanent;

					pawn.Effects?.All.Insert( index, newEffect );

					break;

				case EffectType.Remove:
					effect.OnEnd( pawn );
					pawn.Effects?.All.Remove( effect );

					break;
			}
		}
	}
}
