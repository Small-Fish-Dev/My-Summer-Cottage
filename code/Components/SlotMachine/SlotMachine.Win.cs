namespace Sauna;

partial class SlotMachine
{
	private enum Slot : byte
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

	private Dictionary<(Slot, Slot, Slot), int> _winPayouts = new()
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

	public int CalculateWin()
	{
		return 10;
	}
}
