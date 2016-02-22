using Microsoft.Owin;
using Owin;
using Umbraco.Core;
using Umbraco.Core.Security;
using Umbraco.Web.Security.Identity;
using UmbBackofficeMembershipProvider;
using Umbraco.Core.Models.Identity;

//To use this startup class, change the appSetting value in the web.config called 
// "owin:appStartup" to be "BackofficeMembershipProviderCustomOwinStartup"

[assembly: OwinStartup("BackofficeMembershipProviderCustomOwinStartup", typeof(BackofficeMembershipProviderCustomOwinStartup))]

namespace UmbBackofficeMembershipProvider
{
    /// <summary>
    /// A custom way to configure OWIN for Umbraco
    /// </summary>
    /// <remarks>
    /// The startup type is specified in appSettings under owin:appStartup - change it to "BackofficeMembershipProviderCustomOwinStartup" to use this class
    /// 
    /// This startup class would allow you to customize the Identity IUserStore and/or IUserManager for the Umbraco Backoffice
    /// </remarks>
    public class BackofficeMembershipProviderCustomOwinStartup
    {
        public void Configuration(IAppBuilder app)
        {
            //Configure the Identity user manager for use with Umbraco Back office

            var applicationContext = ApplicationContext.Current;
            app.ConfigureUserManagerForUmbracoBackOffice<BackOfficeUserManager, BackOfficeIdentityUser>(
                applicationContext,
                (options, context) =>
                {
                    var membershipProvider = MembershipProviderExtensions.GetUsersMembershipProvider().AsUmbracoMembershipProvider();
                    var userManager = BackOfficeUserManager.Create(options,
                        applicationContext.Services.UserService,
                        applicationContext.Services.ExternalLoginService,
                        membershipProvider);

                    // Call custom passowrd checker.
                    userManager.BackOfficeUserPasswordChecker = new BackofficeMembershipProviderPasswordChecker();

                    return userManager;
                });

            //Ensure owin is configured for Umbraco back office authentication
            app
                .UseUmbracoBackOfficeCookieAuthentication(ApplicationContext.Current)
                .UseUmbracoBackOfficeExternalCookieAuthentication(ApplicationContext.Current);
        }
    }
}
