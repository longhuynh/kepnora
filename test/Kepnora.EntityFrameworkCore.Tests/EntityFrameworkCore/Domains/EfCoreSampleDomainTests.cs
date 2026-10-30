using Kepnora.Samples;
using Xunit;

namespace Kepnora.EntityFrameworkCore.Domains;

[Collection(KepnoraTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<KepnoraEntityFrameworkCoreTestModule>
{

}
