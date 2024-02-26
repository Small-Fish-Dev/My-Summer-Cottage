namespace Sauna;

partial class SlotMachine
{
	public enum Slot : byte
	{
		Poop,
		Orange,
		Apple,
		Banana,
		Star,
		Avocado,
		Gold,
		Jackpot,
		Any
	}

	private Dictionary<(Slot a, Slot b, Slot c), int> _winPayouts = new()
	{
		[(Slot.Jackpot, Slot.Jackpot, Slot.Jackpot)] = 200,
		[(Slot.Jackpot, Slot.Jackpot, Slot.Star)] = 100,
		[(Slot.Gold, Slot.Gold, Slot.Gold)] = 150,
		[(Slot.Gold, Slot.Gold, Slot.Star)] = 100,
		[(Slot.Star, Slot.Star, Slot.Star)] = 80,
		[(Slot.Star, Slot.Star, Slot.Any)] = 60,
		[(Slot.Avocado, Slot.Avocado, Slot.Avocado)] = 40,
		[(Slot.Avocado, Slot.Avocado, Slot.Star)] = 50,
		[(Slot.Banana, Slot.Banana, Slot.Banana)] = 30,
		[(Slot.Banana, Slot.Banana, Slot.Star)] = 40,
		[(Slot.Orange, Slot.Orange, Slot.Orange)] = 24,
		[(Slot.Orange, Slot.Orange, Slot.Star)] = 36,
		[(Slot.Apple, Slot.Apple, Slot.Apple)] = 20,
		[(Slot.Apple, Slot.Apple, Slot.Star)] = 30,
		[(Slot.Poop, Slot.Any, Slot.Any)] = 12,
		[(Slot.Poop, Slot.Poop, Slot.Any)] = 8
	};

	private Dictionary<int, Slot[]> _conversionTable = new()
	{
		[1] = new Slot[] { Slot.Poop, Slot.Orange, Slot.Apple, Slot.Banana, Slot.Star, Slot.Avocado, Slot.Gold, Slot.Jackpot },
		[2] = new Slot[] { Slot.Jackpot, Slot.Gold, Slot.Avocado, Slot.Star, Slot.Banana, Slot.Avocado, Slot.Orange, Slot.Poop },
		[3] = new Slot[] { Slot.Banana, Slot.Gold, Slot.Orange, Slot.Jackpot, Slot.Star, Slot.Avocado, Slot.Apple, Slot.Poop },
	};

	private int Normalize( int value, int modulo )
	{
		var remainder = (value % modulo);
		return (remainder < 0) ? (modulo + remainder) : remainder;
	}

	private int GetRollIndex( int wheel, BetFlag flag )
		=> Normalize( RollResult[wheel] + (flag switch { BetFlag.Second => 1, BetFlag.Third => -1, _ => 0 }), 8 );

	public (int Amount, IReadOnlyList<(Slot, Slot, Slot)> Wins) CalculateWinResult()
	{
		var total = 0;
		var wins = new List<(Slot, Slot, Slot)>();

		foreach ( var flag in Enum.GetValues<BetFlag>() )
		{
			if ( !BetFlags.HasFlag( flag ) 
				|| flag == BetFlag.None 
				|| flag == BetFlag.All ) continue;

			var a = _conversionTable[Wheels.a][GetRollIndex( 1, flag )];
			var b = _conversionTable[Wheels.b][GetRollIndex( 2, flag )];
			var c = _conversionTable[Wheels.c][GetRollIndex( 3, flag )];

			if ( _winPayouts.TryGetValue( (a, b, c), out var payout ) )
			{
				total += payout;
				wins.Add( (a, b, c) );
				continue;
			}
			
			if ( _winPayouts.TryGetValue( (a, b, Slot.Any), out payout ) )
			{
				total += payout;
				wins.Add( (a, b, Slot.Any) );
				continue;
			}

			if ( _winPayouts.TryGetValue( (a, Slot.Any, Slot.Any), out payout ) )
			{
				total += payout;
				wins.Add( (a, Slot.Any, Slot.Any) );
			}
		}

		return (total, wins);
	}
}
