namespace Sauna;

public static class StringExtensions
{
	public static string RemoveDiacritics( this string text )
	{
		var normalizedString = text.Normalize( NormalizationForm.FormD );
		var stringBuilder = new StringBuilder( capacity: normalizedString.Length );

		for ( int i = 0; i < normalizedString.Length; i++ )
		{
			char c = normalizedString[i];
			var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory( c );
			if ( unicodeCategory != UnicodeCategory.NonSpacingMark )
			{
				stringBuilder.Append( c );
			}
		}

		return stringBuilder
			.ToString()
			.Normalize( NormalizationForm.FormC );
	}
}
