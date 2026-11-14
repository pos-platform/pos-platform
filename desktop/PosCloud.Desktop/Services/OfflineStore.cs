using System.IO;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using PosCloud.Desktop.Models;

namespace PosCloud.Desktop.Services;

/// <summary>
/// SQLite-based lokal kesh va oflayn savdo navbati.
/// DB fayli: %LOCALAPPDATA%\PosPlatform\offline.db
/// </summary>
public class OfflineStore : IDisposable
{
    private readonly SqliteConnection _db;
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public static string DefaultDbPath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PosPlatform"); // eski papka nomi ataylab saqlangan (mavjud ma'lumotlar yo'qolmasin)
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "offline.db");
    }

    public OfflineStore(string? dbPath = null)
    {
        _db = new SqliteConnection($"Data Source={dbPath ?? DefaultDbPath()}");
        _db.Open();
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        Exec(@"
            CREATE TABLE IF NOT EXISTS ProductCache (
                Id          TEXT PRIMARY KEY,
                Name        TEXT NOT NULL,
                Barcode     TEXT,
                MxikCode    TEXT NOT NULL,
                Price       REAL NOT NULL,
                Quantity    REAL NOT NULL,
                Unit        TEXT NOT NULL,
                CategoryId  TEXT NOT NULL,
                ImageUrl    TEXT,
                IsVat       INTEGER NOT NULL DEFAULT 1
            );
            CREATE TABLE IF NOT EXISTS CategoryCache (
                Id   TEXT PRIMARY KEY,
                Name TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS ShiftCache (
                Key     TEXT PRIMARY KEY,
                Payload TEXT
            );
            CREATE TABLE IF NOT EXISTS PendingSales (
                Id        TEXT PRIMARY KEY,
                Payload   TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS PendingShift (
                Key          TEXT PRIMARY KEY,
                OpeningCash  REAL NOT NULL,
                OpenedAt     TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS PendingShiftClose (
                Key          TEXT PRIMARY KEY,
                CountedCash  REAL NOT NULL,
                QueuedAt     TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS PendingClientErrors (
                Id        TEXT PRIMARY KEY,
                Payload   TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                Attempts  INTEGER NOT NULL DEFAULT 0
            );
            INSERT OR IGNORE INTO ShiftCache(Key, Payload) VALUES('current', NULL);
        ");

        // Eski keshda IsVat ustuni bo'lmasligi mumkin — bo'lmasa qo'shamiz (fiskal chek QQSi uchun).
        if (!ColumnExists("ProductCache", "IsVat"))
            Exec("ALTER TABLE ProductCache ADD COLUMN IsVat INTEGER NOT NULL DEFAULT 1");

        // Tarozi shtrix-kodlari oflaynda ham ishlashi uchun (PLU bo'yicha qidiruv).
        if (!ColumnExists("ProductCache", "IsWeighed"))
            Exec("ALTER TABLE ProductCache ADD COLUMN IsWeighed INTEGER NOT NULL DEFAULT 0");
        if (!ColumnExists("ProductCache", "PluNumber"))
            Exec("ALTER TABLE ProductCache ADD COLUMN PluNumber INTEGER");

        // Navbatdagi savdo serverga yuborilmasa sababini saqlaymiz. Blocked=1 bo'lgan yozuv
        // avtomatik sinovdan chetlatiladi — aks holda u butun navbatni to'sib qo'yardi
        // ("Sync (17)" hech qachon kamaymasligining asosiy sababi).
        if (!ColumnExists("PendingSales", "Attempts"))
            Exec("ALTER TABLE PendingSales ADD COLUMN Attempts INTEGER NOT NULL DEFAULT 0");
        if (!ColumnExists("PendingSales", "LastError"))
            Exec("ALTER TABLE PendingSales ADD COLUMN LastError TEXT");
        if (!ColumnExists("PendingSales", "Blocked"))
            Exec("ALTER TABLE PendingSales ADD COLUMN Blocked INTEGER NOT NULL DEFAULT 0");
    }

    /// <summary>Jadvalda ustun mavjudligini tekshiradi (sxema migratsiyasi uchun).</summary>
    private bool ColumnExists(string table, string column)
    {
        using var cmd = Cmd($"PRAGMA table_info({table})");
        using var r = cmd.ExecuteReader();
        while (r.Read())
            if (string.Equals(r.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    // ─── Mahsulotlar ────────────────────────────────────────────────────────

    public void SaveProducts(IEnumerable<ProductDto> products)
    {
        using var tx = _db.BeginTransaction();
        Exec("DELETE FROM ProductCache", tx);
        foreach (var p in products)
        {
            var cmd = Cmd(
                "INSERT OR REPLACE INTO ProductCache(Id,Name,Barcode,MxikCode,Price,Quantity,Unit,CategoryId,ImageUrl,IsVat,IsWeighed,PluNumber) " +
                "VALUES(@Id,@Name,@Barcode,@MxikCode,@Price,@Qty,@Unit,@CatId,@Img,@IsVat,@IsWeighed,@Plu)", tx);
            cmd.Parameters.AddWithValue("@Id",     p.Id.ToString());
            cmd.Parameters.AddWithValue("@Name",   p.Name);
            cmd.Parameters.AddWithValue("@Barcode",p.Barcode ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@MxikCode",p.MxikCode);
            cmd.Parameters.AddWithValue("@Price",  (double)p.Price);
            cmd.Parameters.AddWithValue("@Qty",    (double)p.Quantity);
            cmd.Parameters.AddWithValue("@Unit",   p.Unit);
            cmd.Parameters.AddWithValue("@CatId",  p.CategoryId.ToString());
            cmd.Parameters.AddWithValue("@Img",    p.ImageUrl ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@IsVat",  p.IsVat ? 1 : 0);
            cmd.Parameters.AddWithValue("@IsWeighed", p.IsWeighed ? 1 : 0);
            cmd.Parameters.AddWithValue("@Plu",    p.PluNumber.HasValue ? p.PluNumber.Value : (object)DBNull.Value);
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }

    public List<ProductDto> LoadProducts()
    {
        var list = new List<ProductDto>();
        using var cmd = Cmd("SELECT Id,Name,Barcode,MxikCode,Price,Quantity,Unit,CategoryId,ImageUrl,IsVat,IsWeighed,PluNumber FROM ProductCache ORDER BY Name");
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new ProductDto
            {
                Id         = Guid.Parse(r.GetString(0)),
                Name       = r.GetString(1),
                Barcode    = r.IsDBNull(2) ? null : r.GetString(2),
                MxikCode   = r.GetString(3),
                Price      = (decimal)r.GetDouble(4),
                Quantity   = (decimal)r.GetDouble(5),
                Unit       = r.GetString(6),
                CategoryId = Guid.Parse(r.GetString(7)),
                ImageUrl   = r.IsDBNull(8) ? null : r.GetString(8),
                IsVat      = !r.IsDBNull(9) && r.GetInt64(9) != 0,
                IsWeighed  = !r.IsDBNull(10) && r.GetInt64(10) != 0,
                PluNumber  = r.IsDBNull(11) ? null : (int)r.GetInt64(11)
            });
        }
        return list;
    }

    public bool HasProducts()
    {
        using var cmd = Cmd("SELECT COUNT(*) FROM ProductCache");
        return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
    }

    // ─── Kategoriyalar ──────────────────────────────────────────────────────

    public void SaveCategories(IEnumerable<CategoryDto> cats)
    {
        using var tx = _db.BeginTransaction();
        Exec("DELETE FROM CategoryCache", tx);
        foreach (var c in cats)
        {
            var cmd = Cmd("INSERT OR REPLACE INTO CategoryCache VALUES(@Id,@Name)", tx);
            cmd.Parameters.AddWithValue("@Id",   c.Id.ToString());
            cmd.Parameters.AddWithValue("@Name", c.Name);
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }

    public List<CategoryDto> LoadCategories()
    {
        var list = new List<CategoryDto>();
        using var cmd = Cmd("SELECT Id, Name FROM CategoryCache ORDER BY Name");
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new CategoryDto { Id = Guid.Parse(r.GetString(0)), Name = r.GetString(1) });
        return list;
    }

    // ─── Smena ──────────────────────────────────────────────────────────────

    public void SaveShift(ShiftDto? shift)
    {
        var payload = shift is null ? null : JsonSerializer.Serialize(shift, _json);
        var cmd = Cmd("UPDATE ShiftCache SET Payload=@p WHERE Key='current'");
        cmd.Parameters.AddWithValue("@p", payload ?? (object)DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public ShiftDto? LoadShift()
    {
        using var cmd = Cmd("SELECT Payload FROM ShiftCache WHERE Key='current'");
        var val = cmd.ExecuteScalar() as string;
        return string.IsNullOrEmpty(val) ? null : JsonSerializer.Deserialize<ShiftDto>(val, _json);
    }

    // ─── Oflayn smena ───────────────────────────────────────────────────────

    /// <summary>Serverda ochilmagan, lokal smena yozuvini saqlaydi.</summary>
    public void SavePendingShift(decimal openingCash)
    {
        var cmd = Cmd("INSERT OR REPLACE INTO PendingShift VALUES('pending',@Cash,@At)");
        cmd.Parameters.AddWithValue("@Cash", (double)openingCash);
        cmd.Parameters.AddWithValue("@At",   DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    public (decimal OpeningCash, DateTime OpenedAt)? GetPendingShift()
    {
        using var cmd = Cmd("SELECT OpeningCash, OpenedAt FROM PendingShift WHERE Key='pending'");
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        return ((decimal)r.GetDouble(0), DateTime.Parse(r.GetString(1)).ToUniversalTime());
    }

    public void ClearPendingShift()
        => Exec("DELETE FROM PendingShift WHERE Key='pending'");

    public bool HasPendingShift() =>
        Convert.ToInt32(Cmd("SELECT COUNT(*) FROM PendingShift").ExecuteScalar()) > 0;

    // ─── Oflayn smena yopish navbati ────────────────────────────────────────

    public void SavePendingShiftClose(decimal countedCash)
    {
        var cmd = Cmd("INSERT OR REPLACE INTO PendingShiftClose VALUES('pending',@Cash,@At)");
        cmd.Parameters.AddWithValue("@Cash", (double)countedCash);
        cmd.Parameters.AddWithValue("@At",   DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    public decimal? GetPendingShiftClose()
    {
        using var cmd = Cmd("SELECT CountedCash FROM PendingShiftClose WHERE Key='pending'");
        var val = cmd.ExecuteScalar();
        return val is null or DBNull ? null : (decimal)(double)val;
    }

    public void ClearPendingShiftClose()
        => Exec("DELETE FROM PendingShiftClose WHERE Key='pending'");

    public bool HasPendingShiftClose() =>
        Convert.ToInt32(Cmd("SELECT COUNT(*) FROM PendingShiftClose").ExecuteScalar()) > 0;

    // ─── Oflayn savdo navbati ────────────────────────────────────────────────

    public void EnqueueSale(string localId, CreateSaleRequest request)
    {
        var cmd = Cmd("INSERT OR REPLACE INTO PendingSales(Id,Payload,CreatedAt,Attempts,LastError,Blocked) " +
                      "VALUES(@Id,@Payload,@At,0,NULL,0)");
        cmd.Parameters.AddWithValue("@Id",      localId);
        cmd.Parameters.AddWithValue("@Payload", JsonSerializer.Serialize(request, _json));
        cmd.Parameters.AddWithValue("@At",      DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    /// <summary>Yuborilishi kerak bo'lgan savdolar (bloklanganlari kirmaydi).</summary>
    public List<(string Id, CreateSaleRequest Request)> GetPendingSales()
    {
        var list = new List<(string, CreateSaleRequest)>();
        using var cmd = Cmd("SELECT Id, Payload FROM PendingSales WHERE Blocked = 0 ORDER BY CreatedAt");
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var req = JsonSerializer.Deserialize<CreateSaleRequest>(r.GetString(1), _json)!;
            list.Add((r.GetString(0), req));
        }
        return list;
    }

    /// <summary>Server rad etgan (qayta yuborish foyda bermaydigan) savdoni navbatdan chetlatadi,
    /// lekin o'chirmaydi — kassir sababini ko'rishi va kerak bo'lsa qayta urinishi mumkin.</summary>
    public void MarkSaleFailed(string id, string error)
    {
        var cmd = Cmd("UPDATE PendingSales SET Blocked = 1, Attempts = Attempts + 1, LastError = @Err WHERE Id = @Id");
        cmd.Parameters.AddWithValue("@Err", error.Length > 500 ? error.Substring(0, 500) : error);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Vaqtinchalik xato — savdo navbatda qoladi, faqat urinishlar soni oshadi.</summary>
    public void MarkSaleRetry(string id, string error)
    {
        var cmd = Cmd("UPDATE PendingSales SET Attempts = Attempts + 1, LastError = @Err WHERE Id = @Id");
        cmd.Parameters.AddWithValue("@Err", error.Length > 500 ? error.Substring(0, 500) : error);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.ExecuteNonQuery();
    }

    public List<(string Id, DateTime CreatedAt, string Error)> GetBlockedSales()
    {
        var list = new List<(string, DateTime, string)>();
        using var cmd = Cmd("SELECT Id, CreatedAt, LastError FROM PendingSales WHERE Blocked = 1 ORDER BY CreatedAt");
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            DateTime.TryParse(r.GetString(1), out var at);
            list.Add((r.GetString(0), at, r.IsDBNull(2) ? "" : r.GetString(2)));
        }
        return list;
    }

    /// <summary>Navbatda turgan (bloklanmagan) savdoning oxirgi xato sababi — kassirga
    /// «nega yuborilmayapti» degan savolga javob berish uchun.</summary>
    public string? LastPendingError()
    {
        using var cmd = Cmd("SELECT LastError FROM PendingSales WHERE Blocked = 0 AND LastError IS NOT NULL " +
                            "ORDER BY CreatedAt LIMIT 1");
        return cmd.ExecuteScalar() as string;
    }

    public int BlockedCount()
    {
        using var cmd = Cmd("SELECT COUNT(*) FROM PendingSales WHERE Blocked = 1");
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>Bloklangan savdolarni navbatga qaytaradi (kassir "Qayta urinish" desa).</summary>
    public void UnblockAllSales() => Exec("UPDATE PendingSales SET Blocked = 0 WHERE Blocked = 1");

    public void RemovePendingSale(string id)
    {
        var cmd = Cmd("DELETE FROM PendingSales WHERE Id=@Id");
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Yuborilishi kutilayotgan savdolar soni (bloklanganlari hisobga olinmaydi).</summary>
    public int PendingCount()
    {
        using var cmd = Cmd("SELECT COUNT(*) FROM PendingSales WHERE Blocked = 0");
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    // ─── Xatolik hisobotlari navbati (10.4, /api/client-errors) ─────────────

    /// <summary>Navbatda saqlanadigan eng ko'p hisobot — eskilari o'chiriladi (disk to'lmasin).</summary>
    private const int MaxPendingClientErrors = 200;

    public void EnqueueClientError(string payloadJson)
    {
        var cmd = Cmd("INSERT OR REPLACE INTO PendingClientErrors(Id,Payload,CreatedAt,Attempts) VALUES(@Id,@P,@At,0)");
        cmd.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString("N"));
        cmd.Parameters.AddWithValue("@P", payloadJson);
        cmd.Parameters.AddWithValue("@At", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
        Exec($"DELETE FROM PendingClientErrors WHERE Id NOT IN " +
             $"(SELECT Id FROM PendingClientErrors ORDER BY CreatedAt DESC LIMIT {MaxPendingClientErrors})");
    }

    public List<(string Id, string Payload, int Attempts)> GetPendingClientErrors(int limit = 20)
    {
        var list = new List<(string, string, int)>();
        using var cmd = Cmd("SELECT Id, Payload, Attempts FROM PendingClientErrors ORDER BY CreatedAt LIMIT @L");
        cmd.Parameters.AddWithValue("@L", limit);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add((r.GetString(0), r.GetString(1), r.GetInt32(2)));
        return list;
    }

    public void RemovePendingClientError(string id)
    {
        var cmd = Cmd("DELETE FROM PendingClientErrors WHERE Id=@Id");
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.ExecuteNonQuery();
    }

    public void MarkClientErrorRetry(string id)
    {
        var cmd = Cmd("UPDATE PendingClientErrors SET Attempts = Attempts + 1 WHERE Id=@Id");
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.ExecuteNonQuery();
    }

    // ─── Yordamchilar ───────────────────────────────────────────────────────

    private SqliteCommand Cmd(string sql, SqliteTransaction? tx = null)
    {
        var cmd = _db.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = tx;
        return cmd;
    }

    private void Exec(string sql, SqliteTransaction? tx = null)
    {
        using var cmd = Cmd(sql, tx);
        cmd.ExecuteNonQuery();
    }

    public void Dispose() => _db.Dispose();
}
