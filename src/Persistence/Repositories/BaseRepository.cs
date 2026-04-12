using GridironFrontOffice.Domain;
using GridironFrontOffice.Framework;
using GridironFrontOffice.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GridironFrontOffice.Persistence.Repositories;

public class BaseRepository<T> : IBaseRepository<T> where T : BaseEntity
{
	private readonly IDatabaseConnectionFactory _databaseConnectionFactory;

	public BaseRepository(IDatabaseConnectionFactory databaseConnectionFactory)
	{
		_databaseConnectionFactory = databaseConnectionFactory;
	}

	public async Task BulkInsertAsync(IEnumerable<T> entities)
	{
		try
		{
			var entityList = entities.ToList();
			if (entityList.Count == 0)
			{
				return;
			}

			await using var context = GetDbContext();

			await context.Set<T>().AddRangeAsync(entityList);
			var rowsAffected = await context.SaveChangesAsync();

			if (rowsAffected == 0)
			{
				var ex = new DomainException($"Failed to insert {typeof(T).Name} entities during bulk insert.", $"{typeof(T).Name.ToUpper()}_BULK_INSERT_FAILED");
				throw ex;
			}
		}
		catch (Exception ex)
		{
			var domainEx = new DomainException($"An error occurred during bulk insert of {typeof(T).Name} entities.", $"{typeof(T).Name.ToUpper()}_BULK_INSERT_ERROR", ex);
			throw domainEx;
		}
	}

	public async Task<bool> DeleteAsync(int entityId)
	{
		try
		{
			await using var context = GetDbContext();
			var entity = await this.GetByIDAsync(entityId);
			if (entity == null)
			{
				return false;
			}

			context.Set<T>().Remove(entity);
			var rowsAffected = await context.SaveChangesAsync();
			return rowsAffected > 0;
		}
		catch (Exception ex)
		{
			var domainEx = new DomainException($"An error occurred while deleting {typeof(T).Name} entity with ID {entityId}.", $"{typeof(T).Name.ToUpper()}_DELETE_ERROR", ex);
			domainEx.Details.Add("EntityID", entityId);
			throw domainEx;
		}
	}

	public async Task<IEnumerable<T>> GetAllAsync()
	{
		await using var context = GetDbContext();
		return await context.Set<T>().AsNoTracking().ToListAsync();
	}

	public async Task<T?> GetByIDAsync(int entityId)
	{
		await using var context = GetDbContext();
		return await context.FindAsync<T>(entityId);
	}

	public async Task<int> InsertAsync(T entity)
	{
		try
		{
			await using var context = GetDbContext();
			await context.Set<T>().AddAsync(entity);
			var rowsAffected = await context.SaveChangesAsync();

			if (rowsAffected == 0)
			{
				var ex = new DomainException($"Failed to insert {typeof(T).Name} entity.", $"{typeof(T).Name.ToUpper()}_INSERT_FAILED");
				throw ex;
			}

			return entity.ID;
		}
		catch (Exception ex)
		{
			var domainEx = new DomainException($"An error occurred while inserting {typeof(T).Name} entity.", $"{typeof(T).Name.ToUpper()}_INSERT_ERROR", ex);
			throw domainEx;
		}
	}

	public async Task<int> UpdateAsync(T entity)
	{
		await using var context = GetDbContext();
		context.Set<T>().Update(entity);
		var rowsAffected = await context.SaveChangesAsync();

		if (rowsAffected == 0)
		{
			var ex = new DomainException($"Failed to update {typeof(T).Name} entity.", $"{typeof(T).Name.ToUpper()}_UPDATE_FAILED");
			throw ex;
		}

		return rowsAffected;
	}

	protected GridironFrontOfficeDbContext GetDbContext()
	{
		if (_databaseConnectionFactory.HasActiveConnection == false)
		{
			throw new DomainException("No active database connection.", "NO_ACTIVE_DB_CONNECTION");
		}

		return _databaseConnectionFactory.CreateDbContext();
	}
}