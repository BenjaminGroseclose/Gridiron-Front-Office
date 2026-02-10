using GridironFrontOffice.Domain;
using SQLite;

namespace GridironFrontOffice.Persistence.Interfaces;

/// <summary>
/// Repository interface for League entity data access operations.
/// </summary>
public interface IBaseRepository<T> where T : BaseEntity
{
	/// <summary>
	/// Gets a league by its ID.
	/// </summary>
	/// <param name="entityId">The entity ID.</param>
	/// <returns>The entity if found, otherwise null.</returns>
	Task<T?> GetByIdAsync(int entityId);

	/// <summary>
	/// Gets all entities.
	/// </summary>
	/// <returns>A collection of all entities.</returns>
	Task<IEnumerable<T>> GetAllAsync();

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
	/// <param name="entityId">The entity ID to delete.</param>
	/// <returns>The number of rows deleted.</returns>
	Task<bool> DeleteAsync(int entityId);
}
