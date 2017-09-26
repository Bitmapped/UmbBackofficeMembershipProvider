using System;
using System.Threading.Tasks;
using Umbraco.Core.Models.Identity;
using Umbraco.Core.Security;
using System.Web.Security;
using System.Web;
using System.Configuration;
using umbraco;
using Microsoft.AspNet.Identity;
using Umbraco.Core.Logging;
using Umbraco.Core;

namespace UmbBackofficeMembershipProvider
{
    public class BackofficeMembershipProviderPasswordChecker : IBackOfficeUserPasswordChecker
    {
        /// <summary>
        /// Role for new accounts.
        /// </summary>
        public virtual string AccountRole
        {
            get
            {                
                return ConfigurationManager.AppSettings["BackOfficeMembershipProvider:AccountRole"];
            }
        }

        /// <summary>
        /// E-mail domain name suffix for newly created accounts, if needed.
        /// </summary>
        public virtual string AccountEmailDomain
        {
            get
            {
                // Return domain from configuration settings or current hostname if not specified.
                return ConfigurationManager.AppSettings["BackOfficeMembershipProvider:AccountEmailDomain"] ?? ConfigurationManager.AppSettings["ActiveDirectoryDomain"] ?? HttpContext.Current.Request.Url.Host;
            }
        }

        /// <summary>
        /// Get culture to use in creating new accounts.
        /// </summary>
        public virtual string AccountCulture
        {
            get
            {
                return GlobalSettings.DefaultUILanguage;
            }
        }

        /// <summary>
        /// Determine if accounts should be created if missing.
        /// </summary>
        public virtual bool CreateAccounts
        {
            get
            {
                bool createAccounts;
                Boolean.TryParse(ConfigurationManager.AppSettings["BackOfficeMembershipProvider:CreateAccounts"], out createAccounts);

                return createAccounts;
            }
        }

        /// <summary>
        /// Access configured membership provider.
        /// </summary>
        public virtual MembershipProvider MembershipProvider
        {
            get
            {
                // Access provider.
                if (Membership.Providers["BackofficeMembershipProvider"] == null)
                {
                    throw new InvalidOperationException("Provider 'BackofficeMembershipProvider' is not defined.");
                }

                return Membership.Providers["BackofficeMembershipProvider"];
            }
        }

        protected virtual IdentityResult CreateUser(BackOfficeIdentityUser user, string email, string culture)
        {
            // Get information for the user from Active Directory.
            var adUser = MembershipProvider.GetUser(user.UserName, false);
            if (adUser != null)
            {
                var adEmail = adUser.Email ?? user.UserName;
                var email = adEmail.Contains("@") ? adEmail : String.Format("{0}@{1}", adEmail, AccountEmailDomain);

                var newUser = BackOfficeIdentityUser.CreateNew(user.UserName, email, AccountCulture);

                // Set name. Username already set.
                user.Name = adUser.UserName;

                // Specify e-mail address, appending domain name suffix if needed.
                var adEmail = adUser.Email ?? user.UserName;
                var email = adEmail.Contains("@") ? adEmail : String.Format("{0}@{1}", adEmail, AccountEmailDomain);

                // Set user culture
                user.Culture = GlobalSettings.DefaultUILanguage;                

                // Attempt to create user.
                var userManager = HttpContext.Current.GetOwinContext().GetBackOfficeUserManager();
                if (userManager != null)
                {
                    var createUserTask = userManager.CreateAsync(user);

                    // Assign role to newly persisted user.
                    if (createUserTask.Result.Succeeded && !String.IsNullOrWhiteSpace(AccountRole))
                    {
                        user.AddRole(AccountRole);
                    }

                    return createUserTask.Result;
                }

                // Return that attempt failed.
                return IdentityResult.Failed("Could not access BackOfficeUserManager.");
            }

            return IdentityResult.Failed("Could not load user from Active Directory.");
        }

        /// <summary>
        /// Determines if a username and password are valid using the BackofficeMembershipProvider.
        /// </summary>
        /// <param name="user">User to test.</param>
        /// <param name="password">Password to test.</param>
        /// <returns>Object showing if user credentials are valid or not.</returns>
        public Task<BackOfficeUserPasswordCheckerResult> CheckPasswordAsync(BackOfficeIdentityUser user, string password)
        {  
            // Check the password against Active Directory.
            var validPassword = MembershipProvider.ValidateUser(user.UserName, password);

            // Automatically create a user account if needed.
            if (validPassword && !user.HasIdentity && CreateAccounts)
            {
                // Create user.
                var userResult = CreateUser(user);

                if (userResult.Succeeded)
                {
                    LogHelper.Info(typeof(BackofficeMembershipProviderPasswordChecker), String.Format("Created user account {0} with role {1}.", user.UserName, this.AccountRole));
                }
                else
                {
                    LogHelper.Warn(typeof(BackofficeMembershipProviderPasswordChecker), String.Format("Failed to create user account {0} with error: {1}.", userResult.Errors.ToString()));
                }
            }

            return validPassword ? Task.FromResult(BackOfficeUserPasswordCheckerResult.ValidCredentials) : Task.FromResult(BackOfficeUserPasswordCheckerResult.InvalidCredentials);
        }
    }
}
