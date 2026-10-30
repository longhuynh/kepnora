using Volo.Abp.Modularity;

namespace Kepnora;

public abstract class KepnoraApplicationTestBase<TStartupModule> : KepnoraTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
