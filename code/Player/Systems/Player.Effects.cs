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
	/// Removes the effect.
	/// Should only be called from server.
	/// </summary>
	/// <param name="effect"></param>
	public void Remove( BaseEffect effect )
	{
		Game.AssertServer();

		using var stream = new MemoryStream();
		using var writer = new BinaryWriter( stream );

		All.Remove( effect );
		writeRemove( writer, effect );
		sendPayload( stream );
	}

	/// <summary>
	/// Applies an effect to the player. 
	/// Should only be called from server.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public void Apply<T>( float duration = 1f, int stackAmount = 1 ) where T : BaseEffect
	{
		Game.AssertServer();

		using var stream = new MemoryStream();
		using var writer = new BinaryWriter( stream );
		var time = Time.Now;

		while ( true ) // Just loop.
		{
			// Check for stacking.
			var existing = All.FirstOrDefault( effect => effect?.GetType() == typeof( T ) );

			if ( existing != null )
			{
				// Add time to existing.
				existing.Duration = Math.Min( existing.Duration + duration, existing.MaxDuration );
				writeDuration( writer, existing );

				// Add stacks to existing.
				existing.Stacks = Math.Min( existing.Stacks + stackAmount, existing.MaxStacks );
				writeStack( writer, existing );

				break;
			}

			// Create new effect.
			var effect = GlobalGameNamespace.TypeLibrary.Create<T>( typeof( T ) );
			effect.Duration = Math.Min( duration, effect.MaxDuration );
			effect.Stacks = stackAmount;

			All.Add( effect );
			writeAdd( writer, effect );

			break;
		}

		// Send the payload.
		sendPayload( stream );
	}
}

partial class Player
{
	public EffectManager Effects { get; private set; }

	[Event( "onSpawn" )]
	private static void onSpawn( Player player )
	{		
		if ( Game.IsServer || player == Game.LocalPawn )
			player.Effects = new( player );
	}

	protected void EffectSimulate( IClient cl )
	{
		for ( int i = 0; i < Effects.All.Count; i++ )
		{
			var effect = Effects.All[i];
			if ( effect == null ) 
				continue;

			if ( effect.Duration <= 0 )
			{
				if ( Game.IsServer )
					Effects.Remove( effect );

				continue;
			}

			effect.Duration -= Time.Delta;
			effect.Simulate( cl );
			
			DebugOverlay.ScreenText( $"{effect.Text}{(effect.MaxStacks > 1 ? $" {effect.Stacks}x" : "")} : {effect.Duration:N1}s", i );
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

			switch ( type )
			{
				case EffectType.Stack:
					var stacks = reader.ReadInt32();

					if ( effect != null )
						effect.Stacks = stacks;

					break;

				case EffectType.Duration:
					var duration = reader.ReadSingle();

					if ( effect != null )
						effect.Duration = duration;

					break;

				case EffectType.Add:
					var typeName = reader.ReadString();
					var newDuration = reader.ReadSingle();
					var newStacks = reader.ReadInt32();

					var newEffect = GlobalGameNamespace.TypeLibrary.Create( typeName, typeof( BaseEffect ) ) as BaseEffect;
					newEffect.Duration = newDuration;
					newEffect.Stacks = newStacks;

					pawn.Effects?.All.Insert( index, newEffect );

					break;

				case EffectType.Remove:
					if ( effect != null )
						pawn.Effects?.All.Remove( effect );

					break;
			}
		}
	}
}
