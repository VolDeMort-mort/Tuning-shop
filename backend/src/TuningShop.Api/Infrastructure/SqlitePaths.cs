using Microsoft.Data.Sqlite;

namespace TuningShop.Api.Infrastructure;

public static class SqlitePaths
{
    /// <summary>
    /// Відносний шлях до файла бази розв'язується від каталогу застосунку, а не від робочого
    /// каталогу процесу, щоб різні запуски не відкривали непомітно різні бази.
    /// </summary>
    public static string Resolve(string connectionString, string contentRoot)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (builder.DataSource is "" or ":memory:" || builder.Mode == SqliteOpenMode.Memory)
            return connectionString;

        var path = Path.IsPathRooted(builder.DataSource)
            ? builder.DataSource
            : Path.GetFullPath(Path.Combine(contentRoot, builder.DataSource));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        builder.DataSource = path;
        return builder.ToString();
    }
}
