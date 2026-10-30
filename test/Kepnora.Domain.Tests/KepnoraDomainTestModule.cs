using Volo.Abp.Modularity;

namespace Kepnora;

[DependsOn(
    typeof(KepnoraDomainModule),
    typeof(KepnoraTestBaseModule)
)]
public class KepnoraDomainTestModule : AbpModule
{

}
