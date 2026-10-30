using Microsoft.Extensions.Localization;
using Kepnora.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace Kepnora;

[Dependency(ReplaceServices = true)]
public class KepnoraBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<KepnoraResource> _localizer;

    public KepnoraBrandingProvider(IStringLocalizer<KepnoraResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
