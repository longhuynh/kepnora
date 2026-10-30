using Kepnora.Localization;
using Volo.Abp.Application.Services;

namespace Kepnora;

/* Inherit your application services from this class.
 */
public abstract class KepnoraAppService : ApplicationService
{
    protected KepnoraAppService()
    {
        LocalizationResource = typeof(KepnoraResource);
    }
}
