using Xunit;

namespace Kepnora.EntityFrameworkCore;

[CollectionDefinition(KepnoraTestConsts.CollectionDefinitionName)]
public class KepnoraEntityFrameworkCoreCollection : ICollectionFixture<KepnoraEntityFrameworkCoreFixture>
{

}
