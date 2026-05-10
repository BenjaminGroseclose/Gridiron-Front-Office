using System.Linq.Expressions;
using GridironFrontOffice.Domain;

namespace GridironFrontOffice.Persistence.Interfaces;

/// <summary>
/// Repository interface for entity data access operations.
/// </summary>
public interface IBaseRepository<T> where T : BaseEntity
{
	/// <summary>
	/// Gets an entity by its ID.
	/// </summary>
	/// <param name="entityID">The entity ID.</param>
	/// <param name="includes">Optional navigation properties to include.</param>
	/// <returns>The entity if found, otherwise null.</returns>
	Task<T?> GetByIDAsync(int entityID, params Expression<Func<T, object>>[] includes);

	/// <summary>
	/// Gets all entities.
	/// </summary>
	/// <param name="includes">Optional navigation properties to include.</param>
	/// <returns>A collection of all entities.</returns>
	Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes);

	/// <summary>
	/// Inserts a new entity.
	/// </summary>
	/// <param name="entity">The entity to insert.</param>
	/// <returns>The number of rows inserted.</returns>
	Task<int> InsertAsync(T entity);

	/// <summary>
	/// Updates an existing entity.
	/// </summary>
	/// <param name="entity">The entity to update.</param>
	/// <returns>The number of rows updated.</returns>
	Task<int> UpdateAsync(T entity);

	/// <summary>
	/// Deletes an entity by its ID.
	/// </summary>
	/// <param name="entityID">The entity ID to delete.</param>
	/// <returns>The number of rows deleted.</returns>
	Task<bool> DeleteAsync(int entityID);

	/// <summary>
	/// Inserts multiple entities in bulk.
	/// </summary>
	/// <param name="entities">The collection of entities to insert.</param>
	Task BulkInsertAsync(IEnumerable<T> entities);
}
