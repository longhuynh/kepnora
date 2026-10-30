using Kepnora.Books;
using Xunit;

namespace Kepnora.EntityFrameworkCore.Applications.Books;

[Collection(KepnoraTestConsts.CollectionDefinitionName)]
public class EfCoreBookAppService_Tests : BookAppService_Tests<KepnoraEntityFrameworkCoreTestModule>
{

}