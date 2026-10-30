using Kepnora.Samples;
using Xunit;

namespace Kepnora.EntityFrameworkCore.Applications;

[Collection(KepnoraTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<KepnoraEntityFrameworkCoreTestModule>
{

}
