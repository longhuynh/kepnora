using System.Threading.Tasks;

namespace Kepnora.Data;

public interface IKepnoraDbSchemaMigrator
{
    Task MigrateAsync();
}
