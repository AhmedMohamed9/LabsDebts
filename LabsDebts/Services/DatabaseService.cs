
using SQLite;
using LabsDebts.Models;
using LabsDebts.DTOs;

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
    public async Task<List<Lab>> SearchLabsPaged(
        string? text,
        DateTime fromDate,
        DateTime toDate,
        bool hasTransactionsOnly,
        int page,
        int pageSize)
    {
        var db = await GetDatabaseAsync();

        string sql;
        List<object> parameters = new();

        if (hasTransactionsOnly)
        {
            sql =
            """
        SELECT DISTINCT l.*
        FROM Lab l
        INNER JOIN LabTransaction t
            ON t.LabId = l.Id
        WHERE t.IsPaid = 0
        AND t.Date IS NOT NULL
        AND t.Date >= ?
        AND t.Date <= ?
        """;

            parameters.Add(fromDate);
            parameters.Add(toDate);
        }
        else
        {
            sql =
            """
        SELECT *
        FROM Lab
        WHERE 1 = 1
        """;
        }

        if (!string.IsNullOrWhiteSpace(text))
        {
            if (int.TryParse(text, out int code))
            {
                sql += " AND (Code = ? OR Name LIKE ?)";
                parameters.Add(code);
                parameters.Add($"%{text}%");
            }
            else
            {
                sql += " AND Name LIKE ?";
                parameters.Add($"%{text}%");
            }
        }

        sql += " ORDER BY Name LIMIT ? OFFSET ?";

        parameters.Add(pageSize);
        parameters.Add((page - 1) * pageSize);

        var labs = await db.QueryAsync<Lab>(
            sql,
            parameters.ToArray());

        await Task.WhenAll(
            labs.Select(async lab =>
            {
                lab.UnpaidTotal =
                    await GetUnpaidTotal(
                        lab.Id,
                        fromDate,
                        toDate);
            }));

        return labs;
    }
    //public async Task<bool> HasTransactionsInPeriod(
    //int labId,
    //DateTime fromDate,
    //DateTime toDate)
    //{
    //    var db = await GetDatabaseAsync();

    //    return await db.Table<LabTransaction>()
    //        .Where(x =>
    //            x.LabId == labId &&
    //            x.Date >= fromDate &&
    //            x.Date <= toDate)
    //        .CountAsync() > 0;
    //}
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
    public async Task<int> GetUnpaidTotal(
    int labId,
    DateTime fromDate,
    DateTime toDate)
    {
        var db = await GetDatabaseAsync();

        return await db.ExecuteScalarAsync<int>(
            """
        SELECT IFNULL(SUM(Amount), 0)
        FROM LabTransaction
        WHERE LabId = ?
        AND IsPaid = 0
        AND Date IS NOT NULL
        AND Date >= ?
        AND Date <= ?
        """,
            labId,
            fromDate,
            toDate);
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
    public async Task<List<UnpaidExportRow>> GetUnpaidTransactionsForExport(
    DateTime? fromDate,
    DateTime? toDate)
    {
        var db = await GetDatabaseAsync();

        return await db.QueryAsync<UnpaidExportRow>(
            """
        SELECT
            l.Code AS LabCode,
            l.Name AS LabName,
            t.Amount,
            t.Note,
            t.Date,
            t.DueDate
        FROM LabTransaction t
        INNER JOIN Lab l
            ON l.Id = t.LabId
        WHERE t.IsPaid = 0
        AND (? IS NULL OR t.Date >= ?)
        AND (? IS NULL OR t.Date <= ?)
        ORDER BY t.Date DESC
        """,
            fromDate,
            fromDate,
            toDate,
            toDate);
    }
}
