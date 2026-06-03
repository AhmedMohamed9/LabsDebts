
using SQLite;
using LabsApp.Models;

namespace LabsApp.Services;

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

}//using SQLite;
//using LabsApp.Models;

//namespace LabsApp.Services;

//public class DatabaseService
//{
//    private SQLiteAsyncConnection? _db;

//    private async Task<SQLiteAsyncConnection> GetDatabaseAsync()
//    {
//        if (_db != null)
//            return _db;

//        var path = Path.Combine(FileSystem.AppDataDirectory, "labs.db");

//        _db = new SQLiteAsyncConnection(path);

//        await _db.CreateTableAsync<Lab>();

//        return _db;
//    }
//    public async Task<int> DeleteLab(Lab lab)
//    {
//        await Init();

//        return await _db!.DeleteAsync(lab);
//    }
//    public Task Init()
//    {
//        return GetDatabaseAsync();
//    }

//    public async Task<List<Lab>> GetLabs()
//    {
//        var db = await GetDatabaseAsync();

//        return await db.Table<Lab>().ToListAsync();
//    }

//    public async Task<int> AddLab(Lab lab)
//    {
//        var db = await GetDatabaseAsync();

//        return await db.InsertAsync(lab);
//    }
//}
