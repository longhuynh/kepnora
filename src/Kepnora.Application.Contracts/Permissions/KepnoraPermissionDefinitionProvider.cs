using Kepnora.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace Kepnora.Permissions;

public class KepnoraPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(KepnoraPermissions.GroupName);

        var booksPermission = myGroup.AddPermission(KepnoraPermissions.Books.Default, L("Permission:Books"));
        booksPermission.AddChild(KepnoraPermissions.Books.Create, L("Permission:Books.Create"));
        booksPermission.AddChild(KepnoraPermissions.Books.Edit, L("Permission:Books.Edit"));
        booksPermission.AddChild(KepnoraPermissions.Books.Delete, L("Permission:Books.Delete"));
        //Define your own permissions here. Example:
        //myGroup.AddPermission(KepnoraPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<KepnoraResource>(name);
    }
}
