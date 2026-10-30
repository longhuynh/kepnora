using Volo.Abp.Settings;

namespace Kepnora.Settings;

public class KepnoraSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(KepnoraSettings.MySetting1));
    }
}
