using Kepnora.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace Kepnora.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class KepnoraController : AbpControllerBase
{
    protected KepnoraController()
    {
        LocalizationResource = typeof(KepnoraResource);
    }
}
