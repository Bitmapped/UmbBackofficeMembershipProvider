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
using Umbraco.Web.Security.Identity;
using Umbraco.Core.Services;
using Microsoft.Owin;
using System.Linq;

namespace UmbBackofficeMembershipProvider
{
    public class BackofficeMembershipProviderPasswordChecker : IBackOfficeUserPasswordChecker
    {
        private BackOfficeUserManager<BackOfficeIdentityUser> _userManager;

        /// <summary>
        /// Get culture to use in creating new accounts.
        /// </summary>
        public virtual string AccountCulture
        {
            get
            {
                // Get role from config file if specified. Otherwise, default to editor.
                var configCulture = ConfigurationManager.AppSettings["BackOfficeMembershipProvider:AccountCulture"];
                configCulture = String.IsNullOrWhiteSpace(configCulture) ? GlobalSettings.DefaultUILanguage : configCulture;

                return configCulture;
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
        /// Role for new accounts.
        /// </summary>
        public virtual string[] AccountRoles
        {
            get
            {
                // Get role from config file if specified. Otherwise, default to editor.
                var configRoles = ConfigurationManager.AppSettings["BackOfficeMembershipProvider:AccountRoles"];
                configRoles = String.IsNullOrWhiteSpace(configRoles) ? "editor" : configRoles;

                // Split roles. Trim unnecessary commas and whitespace.
                return configRoles.Trim(',').Split(',').Select(role => role.Trim()).ToArray();
            }
        }

        /// <summary>
        /// Determine if accounts should be created if missing.
        /// </summary>
        public virtual bool CreateAccounts
        {
            get
            {
                bool createAccounts = false;
                Boolean.TryParse(ConfigurationManager.AppSettings["BackOfficeMembershipProvider:CreateAccounts"], out createAccounts);

                return createAccounts;
            }
        }

        /// <summary>
        /// Determine if default Umbraco password checker should be used when user was not authenticated in Active Directory.
        /// </summary>
        public virtual bool FallbackToDefaultChecker
        {
            get
            {
                bool fallbackToDefaultChecker = false;
                Boolean.TryParse(ConfigurationManager.AppSettings["BackOfficeMembershipProvider:FallbackToDefaultChecker"], out fallbackToDefaultChecker);

                return fallbackToDefaultChecker;
            }
        }

        /// <summary>
        /// Returns a ServiceContext
        /// </summary>
        public ServiceContext Services
        {
            get { return ApplicationContext.Current.Services; }
        }

        protected BackOfficeUserManager<BackOfficeIdentityUser> UserManager
        {
            get { return _userManager ?? (_userManager = OwinContext.GetBackOfficeUserManager()); }
        }

        protected IOwinContext OwinContext
        {
            get { return HttpContext.Current.GetOwinContext(); }
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

        /// <summary>
        /// Create a new user account.
        /// </summary>
        /// <param name="user">Back office user account</param>
        /// <param name="userGroups">Groups to assign to user</param>
        /// <param name="culture">Culture for user</param>
        /// <param name="email">Email address for user account</param>
        /// <param name="name">Name for user account</param>
        /// <returns></returns>
        protected async virtual Task<IdentityResult> NewCreateUser(BackOfficeIdentityUser user, string[] userGroups, string culture, string email = null, string name = null)
        {
            // Mandate that parameters must be specified.
            Mandate.ParameterNotNull<BackOfficeIdentityUser>(user, "user");
            Mandate.ParameterNotNullOrEmpty<string>(userGroups, "userGroups");
            Mandate.ParameterNotNull<string>(culture, "culture");

            // Assign name to user if not already specified. Use name if specified, otherwise use email address.
            user.Name = user.Name ?? name ?? user.UserName;

            // Assign email to user if not already specified.
            user.Email = user.Email ?? email;
            if (String.IsNullOrWhiteSpace(user.Email))
            {
                throw new ArgumentNullException("email");
            }

            // Assign user to specified groups.
            var groups = Services.UserService.GetUserGroupsByAlias(userGroups);
            foreach (var userGroup in groups)
            {
                user.AddRole(userGroup.Alias);
            }

            // Create user account.
            var userCreationResults = await UserManager.CreateAsync(user);

            return userCreationResults;
        }

        protected virtual async Task<IdentityResult> CreateUserForLogin(BackOfficeIdentityUser user)
        {
            // Get information for the user from Active Directory.
            var adUser = MembershipProvider.GetUser(user.UserName, false);
            if (adUser != null)
            {
                // Determine e-mail address for user.
                var adEmail = adUser.Email ?? user.UserName;
                var email = adEmail.Contains("@") ? adEmail : String.Format("{0}@{1}", adEmail, AccountEmailDomain);

                // Assign username as name for user.
                var name = user.UserName;

                // Assign roles for user.
                var roles = AccountRoles;

                // Assign culture for user.
                var culture = AccountCulture;

                // Create user.
                var createUserTask = await NewCreateUser(user, roles, culture, email, name);

                if (createUserTask.Succeeded)
                {
                    LogHelper.Info(typeof(BackofficeMembershipProviderPasswordChecker), String.Format("Created user account {0}.", user.UserName));
                }
                else
                {
                    LogHelper.Warn(typeof(BackofficeMembershipProviderPasswordChecker), String.Format("Failed to create user account {0} with error: {1}.", createUserTask.Errors.ToString()));
                }

                return createUserTask;
            }

            return IdentityResult.Failed("Could not load user from Active Directory.");
        }

        /// <summary>
        /// Determines if a username and password are valid using the BackofficeMembershipProvider.
        /// </summary>
        /// <param name="user">User to test.</param>
        /// <param name="password">Password to test.</param>
        /// <returns>Object showing if user credentials are valid or not.</returns>
        public async Task<BackOfficeUserPasswordCheckerResult> CheckPasswordAsync(BackOfficeIdentityUser user, string password)
        {
            // Check the password against Active Directory.
            var validPassword = MembershipProvider.ValidateUser(user.UserName, password);

            // Automatically create a user account if needed.
            if (validPassword && !user.HasIdentity && CreateAccounts)
            {
                // Create user.
                var userResult = await CreateUserForLogin(user);
            }

            return validPassword ? BackOfficeUserPasswordCheckerResult.ValidCredentials :
                FallbackToDefaultChecker ? BackOfficeUserPasswordCheckerResult.FallbackToDefaultChecker : BackOfficeUserPasswordCheckerResult.InvalidCredentials;
        }
    }
}
