using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Kepnora.Data;

/* This is used if database provider does't define
 * IKepnoraDbSchemaMigrator implementation.
 */
public class NullKepnoraDbSchemaMigrator : IKepnoraDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
