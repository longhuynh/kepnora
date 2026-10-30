using Kepnora.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace Kepnora.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(KepnoraEntityFrameworkCoreModule),
    typeof(KepnoraApplicationContractsModule)
)]
public class KepnoraDbMigratorModule : AbpModule
{
}
