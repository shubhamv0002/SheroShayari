using Microsoft.EntityFrameworkCore;
using SheroShayari.API.Data;
using SheroShayari.API.Models;

namespace SheroShayari.API.Repositories;

/// <summary>
/// Interface for Shayari repository operations.
/// </summary>
public interface IShayariRepository
{
    /// <summary>
    /// Gets all Shayaris created by a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of user's Shayaris.</returns>
    Task<List<Shayari>> GetUserShayarisAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for Shayaris by query string.
    /// </summary>
    /// <param name="query">Search query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching Shayaris.</returns>
    Task<List<Shayari>> SearchAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all Shayaris from the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all Shayaris.</returns>
    Task<List<Shayari>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a Shayari by ID.
    /// </summary>
    /// <param name="id">The Shayari ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Shayari or null if not found.</returns>
    Task<Shayari?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new Shayari to the database.
    /// </summary>
    /// <param name="shayari">The Shayari to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added Shayari with ID.</returns>
    Task<Shayari> AddAsync(Shayari shayari, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing Shayari.
    /// </summary>
    /// <param name="shayari">The Shayari to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated Shayari.</returns>
    Task<Shayari> UpdateAsync(Shayari shayari, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a Shayari by ID.
    /// </summary>
    /// <param name="id">The Shayari ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deletion was successful.</returns>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of Shayari repository.
/// </summary>
public class ShayariRepository : IShayariRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<ShayariRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the ShayariRepository.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">Logger instance.</param>
    public ShayariRepository(AppDbContext context, ILogger<ShayariRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Searches for Shayaris by query string.
    /// </summary>
    /// <param name="query">Search query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching Shayaris.</returns>
    public async Task<List<Shayari>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return await GetAllAsync(cancellationToken);
            }

            var normalizedQuery = query.ToLower();

            var results = await _context.Shayaris
                .Where(s => s.Content.ToLower().Contains(normalizedQuery) ||
                           s.Poet.ToLower().Contains(normalizedQuery) ||
                           s.Category.ToLower().Contains(normalizedQuery) ||
                           s.Meaning!.ToLower().Contains(normalizedQuery))
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync(cancellationToken);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Shayari search.");
            return new List<Shayari>();
        }
    }

    /// <summary>
    /// Gets all Shayaris from the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all Shayaris.</returns>
    public async Task<List<Shayari>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Shayaris
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all Shayaris.");
            return new List<Shayari>();
        }
    }

    /// <summary>
    /// Gets all Shayaris created by a specific user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of user's Shayaris.</returns>
    public async Task<List<Shayari>> GetUserShayarisAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Shayaris
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching Shayaris for user {userId}.");
            return new List<Shayari>();
        }
    }

    /// <summary>
    /// Gets a Shayari by ID.
    /// </summary>
    /// <param name="id">The Shayari ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Shayari or null if not found.</returns>
    public async Task<Shayari?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Shayaris.FindAsync(new object[] { id }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching Shayari with ID {id}.");
            return null;
        }
    }

    /// <summary>
    /// Adds a new Shayari to the database.
    /// </summary>
    /// <param name="shayari">The Shayari to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added Shayari with ID.</returns>
    public async Task<Shayari> AddAsync(Shayari shayari, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.Shayaris.Add(shayari);
            await _context.SaveChangesAsync(cancellationToken);
            return shayari;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding new Shayari.");
            throw;
        }
    }

    /// <summary>
    /// Updates an existing Shayari.
    /// </summary>
    /// <param name="shayari">The Shayari to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated Shayari.</returns>
    public async Task<Shayari> UpdateAsync(Shayari shayari, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.Shayaris.Update(shayari);
            await _context.SaveChangesAsync(cancellationToken);
            return shayari;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating Shayari with ID {shayari.Id}.");
            throw;
        }
    }

    /// <summary>
    /// Deletes a Shayari by ID.
    /// </summary>
    /// <param name="id">The Shayari ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deletion was successful.</returns>
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var shayari = await GetByIdAsync(id, cancellationToken);
            if (shayari == null)
            {
                return false;
            }

            _context.Shayaris.Remove(shayari);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting Shayari with ID {id}.");
            throw;
        }
    }
}
