using System.Security.Claims;

using Landscape.Tsi.Application.Identity;

using Microsoft.AspNetCore.Authorization;

namespace Landscape.Tsi.Tests.Identity;

public sealed class PermissionAuthorizationHandlerTests
{
    [Fact]
    public async Task HandleAsync_ClaimMatches_Succeeds()
    {
        var requirement = new PermissionRequirement(Permissions.UserManage);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(CustomClaimTypes.Permission, Permissions.UserManage)], "test"));
        var context = new AuthorizationHandlerContext([requirement], principal, null);

        await new PermissionAuthorizationHandler().HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_ClaimMissing_DeniesByDefault()
    {
        var requirement = new PermissionRequirement(Permissions.UserManage);
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(), null);

        await new PermissionAuthorizationHandler().HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }
}