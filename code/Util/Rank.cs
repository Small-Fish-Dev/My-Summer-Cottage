namespace Sauna;

public readonly struct Rank
{
	public string Title { get; }
	public int Requirement { get; }

	public static readonly IReadOnlyList<Rank> All = new List<Rank>()
	{
		new( "Beginner", 0 ),
		new( "Professional", 150 ),
		new( "Gamer", 420 ),
		new( "Balla'", 696 ),
	};

	public Rank( string title, int requirement )
	{
		Title = title;
		Requirement = requirement;
	}
}
