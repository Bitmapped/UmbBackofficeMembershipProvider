using System;
using System.Threading.Tasks;
using Umbraco.Core.Models.Identity;
using Umbraco.Core.Security;
using System.Web.Security;

namespace UmbBackofficeMembershipProvider
{
    public class BackofficeMembershipProviderPasswordChecker : IBackOfficeUserPasswordChecker
    {
        /// <summary>
        /// Determines if a username and password are valid using the BackofficeMembershipProvider.
        /// </summary>
        /// <param name="user">User to test.</param>
        /// <param name="password">Password to test.</param>
        /// <returns>Object showing if user credentials are valid or not.</returns>
        public Task<BackOfficeUserPasswordCheckerResult> CheckPasswordAsync(BackOfficeIdentityUser user, string password)
        {
            // Access provider.
            if (Membership.Providers["BackofficeMembershipProvider"] == null)
            {
                throw new InvalidOperationException("Provider 'BackofficeMembershipProvider' is not defined.");
            }
            var adProvider = Membership.Providers["BackofficeMembershipProvider"];

            // Check the user's password.
            var validUser = adProvider.ValidateUser(user.UserName, password) ? Task.FromResult(BackOfficeUserPasswordCheckerResult.ValidCredentials) : Task.FromResult(BackOfficeUserPasswordCheckerResult.InvalidCredentials);

            return validUser;
        }
    }
}
