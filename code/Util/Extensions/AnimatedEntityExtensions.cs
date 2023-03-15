namespace Sauna;

public static class AnimatedEntityExtensions
{
	/// <summary>
	/// Copies the morphs from an entity to another. 
	/// This method skips the morphs that don't match indices.
	/// </summary>
	/// <param name="entity"></param>
	/// <param name="other"></param>
	public static void CopyMorphs( this AnimatedEntity entity, AnimatedEntity other )
	{
		// Copy matching index morhps.
		for ( int i = 0; i < entity.Morphs.Count; i++ )
		{
			if ( other.Morphs.GetName( i ) != entity.Morphs.GetName( i ) )
				continue;

			entity.Morphs.Set( i, other.Morphs.Get( i ) );
		}
	}
}
