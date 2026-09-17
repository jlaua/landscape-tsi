using Landscape.Tsi.Domain.Identity;

using Microsoft.AspNetCore.Identity;

namespace Landscape.Tsi.Infrastructure.Identity;

public static class PasswordResetWorkflow
{
    public static async Task<IdentityResult> ResetAndUnlockAsync(
        UserManager<IamUsuario> userManager,
        IamUsuario user,
        string password)
    {
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, password);
        if (!result.Succeeded)
        {
            return result;
        }

        await userManager.ResetAccessFailedCountAsync(user);
        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.UpdateSecurityStampAsync(user);
        return result;
    }
}