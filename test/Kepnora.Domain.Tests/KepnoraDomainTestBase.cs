using Volo.Abp.Modularity;

namespace Kepnora;

/* Inherit from this class for your domain layer tests. */
public abstract class KepnoraDomainTestBase<TStartupModule> : KepnoraTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
