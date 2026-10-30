using Volo.Abp.Modularity;

namespace Kepnora;

[DependsOn(
    typeof(KepnoraApplicationModule),
    typeof(KepnoraDomainTestModule)
)]
public class KepnoraApplicationTestModule : AbpModule
{

}
