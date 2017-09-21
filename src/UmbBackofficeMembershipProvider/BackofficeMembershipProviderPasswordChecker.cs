using System;
using System.Threading.Tasks;
using Umbraco.Core.Models.Identity;
using Umbraco.Core.Security;
using System.Web.Security;
using System.Web;

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

            // Check the password against Active Directory.
            var validPassword = adProvider.ValidateUser(user.UserName, password);

            // Automatically create a user account if needed.
            if (validPassword && !user.HasIdentity)
            {
                // Get user from Active Directory and populate into account if possible.
                var adUser = adProvider.GetUser(user.UserName, false);
                if (adUser != null)
                {
                    user.Name = adUser.UserName;
                    user.Email = adUser.Email ?? String.Format("{0}@{1}", user.UserName, HttpContext.Current.Request.Url.Host); // TODO: Add suffix if doesn't contain @

                    //TODO: Configure group, culture

                    // Get user manager
                    var userManager = HttpContext.Current.GetOwinContext().GetBackOfficeUserManager();
                    if (userManager != null)
                    {
                        var createUserTask = userManager.CreateAsync(user);
                        var createUserResult = createUserTask.Result;
                        throw new Exception("Gonna stop here");
                    }
                    throw new Exception("Gonna stop here");
                }
                
            }

            // Check the user's password.
            var validUser =  validPassword ? Task.FromResult(BackOfficeUserPasswordCheckerResult.ValidCredentials) : Task.FromResult(BackOfficeUserPasswordCheckerResult.InvalidCredentials);

            return validUser;
        }
    }
}
