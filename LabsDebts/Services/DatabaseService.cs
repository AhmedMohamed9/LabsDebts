
using SQLite;
using LabsDebts.Models;

namespace LabsDebts.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection? _db;

    private async Task<SQLiteAsyncConnection> GetDatabaseAsync()
    {
        if (_db != null)
            return _db;

        var path = Path.Combine(FileSystem.AppDataDirectory, "labs.db");

        _db = new SQLiteAsyncConnection(path);

        // Create Tables

        await _db.CreateTableAsync<Lab>();
        await _db.CreateTableAsync<LabTransaction>();

        return _db;
    }

    public Task Init()
    {
        return GetDatabaseAsync();
    }

    // =========================
    // Labs
    // =========================

    public async Task<List<Lab>> GetLabs()
    {
        var db = await GetDatabaseAsync();

        return await db.Table<Lab>().ToListAsync();
    }
    public async Task<bool> LabExists(int code, string name)
    {
        var db = await GetDatabaseAsync();

        return await db.Table<Lab>()
            .Where(x =>
                x.Code == code ||
                x.Name.ToLower() == name.ToLower())
            .CountAsync() > 0;
    }
    public async Task<List<Lab>> GetLabsPaged(
    int page,
    int pageSize)
    {
        var db = await GetDatabaseAsync();

        var labs = await db.Table<Lab>()
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        await Task.WhenAll(
                labs.Select(async lab =>
                {
                    lab.UnpaidTotal = await GetUnpaidTotal(lab.Id);
                }));

        return labs;
    }
    public async Task<List<Lab>> SearchLabs(string text)
    {
        var db = await GetDatabaseAsync();

        text = text.Trim();

        List<Lab> labs;

        if (int.TryParse(text, out int code))
        {
            labs = await db.Table<Lab>()
                .Where(x =>
                    x.Code == code ||
                    x.Name.Contains(text))
                .ToListAsync();
        }
        else
        {
            labs = await db.Table<Lab>()
                .Where(x => x.Name.Contains(text))
                .ToListAsync();
        }

        await Task.WhenAll(
            labs.Select(async lab =>
            {
                lab.UnpaidTotal = await GetUnpaidTotal(lab.Id);
            }));

        return labs;
    }
    public async Task<int> AddLab(Lab lab)
    {
        var db = await GetDatabaseAsync();

        return await db.InsertAsync(lab);
    }

    public async Task<int> DeleteLab(Lab lab)
    {
        var db = await GetDatabaseAsync();

        return await db.DeleteAsync(lab);
    }

    // =========================
    // Transactions
    // =========================

    public async Task<int> AddTransaction(LabTransaction transaction)
    {
        var db = await GetDatabaseAsync();

        return await db.InsertAsync(transaction);
    }

    public async Task<List<LabTransaction>> GetTransactions(int labId)
    {
        var db = await GetDatabaseAsync();

        return await db.Table<LabTransaction>()
            .Where(x => x.LabId == labId)
            .OrderByDescending(x => x.Date)
            .ToListAsync();
    }
    public async Task<List<LabTransaction>> GetTransactionsPaged(
    int labId,
    int page,
    int pageSize)
    {
        var db = await GetDatabaseAsync();

        return await db.Table<LabTransaction>()
            .Where(x => x.LabId == labId)
            .OrderByDescending(x => x.Date)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
    public async Task<int> GetUnpaidTotal(int labId)
    {
        var db = await GetDatabaseAsync();

        var result = await db.ExecuteScalarAsync<int>(
            """
        SELECT IFNULL(SUM(Amount), 0)
        FROM LabTransaction
        WHERE LabId = ?
        AND IsPaid = 0
        """,
            labId);

        return result;
    }
    public async Task<int> DeleteTransaction(LabTransaction transaction)
    {
        var db = await GetDatabaseAsync();

        return await db.DeleteAsync(transaction);
    }

    public async Task<int> UpdateTransaction(LabTransaction transaction)
    {
        var db = await GetDatabaseAsync();

        return await db.UpdateAsync(transaction);
    }
    public async Task<List<Lab>> GetLabsWithTotals()
    {
        var db = await GetDatabaseAsync();

        var labs = await db.Table<Lab>().ToListAsync();

        foreach (var lab in labs)
        {
            lab.UnpaidTotal =
                await db.ExecuteScalarAsync<int>(
                    """
                SELECT IFNULL(SUM(Amount), 0)
                FROM LabTransaction
                WHERE LabId = ?
                AND IsPaid = 0
                """,
                    lab.Id);
        }

        return labs;
    }

}
