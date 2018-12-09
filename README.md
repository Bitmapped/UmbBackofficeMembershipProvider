# UmbBackofficeMembershipProvider
Code to allow Umbraco 7.7.2+ to use MembershipProvider-based providers for Active Directory authentication.

Users of Umbraco 7.4.2-7.7.1 should use UmbBackofficeMembershipProvider 3.0.0 ([NuGet](https://www.nuget.org/packages/UmbBackofficeMembershipProvider/3.0.0)). This version requires API changes and bug fixes present in Umbraco 7.7.2+ to function properly.

## What's inside
This project includes a DLL that will allow you to use a traditional `MembershipProvider` for logging in Umbraco backoffice users.

## System requirements
1. NET Framework 4.5
2. Umbraco 7.7.2+

# NuGet availability
This project is available on [NuGet](https://www.nuget.org/packages/UmbBackofficeMembershipProvider/).

## Usage instructions
### Getting started
1. Before making any configuration file changes, make sure that you have an Administrator-level user account in Umbraco with the same username as the Active Directory account that you will use to login to Umbraco. It doesn't matter what you set for the password once UmbBackofficeMembershipProvider is enabled as it will check against Active Directory and not Umbraco for the password (unless you enable the fallback option below).

### Installing UmbBackofficeMembershipProvider
2. Add **UmbBackofficeMembershipProvider.dll** as a reference in your project or place it in the **\bin** folder.
3. In **web.config**, make the following modifications:
   - Add or modify the following line in the `<appSettings>` section:

    ```
    <add key="owin:appStartup" value="BackofficeMembershipProviderCustomOwinStartup" />
    ```
   - Add a LDAP connection string to your LDAP server in the `<connectionStrings>` section, like shown in the example code below. Specify a path to the domain root or a container/OU if you want to limit where the user accounts can be located.
  
    ```
    <add connectionString="LDAP://mydomain.mycompany.com/DC=mydomain,DC=mycompany,DC=com" name="ADConnectionString" />
    ```
   - Add a membership provider named `BackofficeMembershipProvider`, like shown in the example code below. Be sure the `connectionStringName` matches the LDAP connection string you defined. `attributeMapUsername` specifies the username format - `sAMAccountName` for just the username, or `userPrincipalName` to use username@mydomain.mycompany.com. Be sure the usernames you configure in Umbraco use the same format.

  
   - If you are upgrading from a pre-7.3.1 version of Umbraco that used an Active Directory provider for backoffice users, you must change `UsersMembershipProvider` to `Umbraco.Web.Security.Providers.UsersMembershipProvider`. If you have a new installation, this is the default provider already.  
  
```
    <membership defaultProvider="UmbracoMembershipProvider">
      <providers>
        <add
           name="BackofficeMembershipProvider"
           type="System.Web.Security.ActiveDirectoryMembershipProvider, System.Web, Version=4.0.0.0, 
                 Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
           connectionStringName="ADConnectionString"
           attributeMapUsername="sAMAccountName"
           connectionUsername="testdomain\administrator" 
           connectionPassword="password"/>
        <!-- Existing providers appear below -->
        <add name="UmbracoMembershipProvider"
           type="Umbraco.Web.Security.Providers.MembersMembershipProvider, Umbraco"
           minRequiredNonalphanumericCharacters="0" minRequiredPasswordLength="10"
           useLegacyEncoding="false" enablePasswordRetrieval="false" enablePasswordReset="false"
           requiresQuestionAndAnswer="false" defaultMemberTypeAlias="Member"
           passwordFormat="Hashed" allowManuallyChangingPassword="false" />
        <add name="UsersMembershipProvider"
           type="Umbraco.Web.Security.Providers.UsersMembershipProvider, Umbraco" />
      </providers>
     </membership>
```

  

4. In **config\UmbracoSettings.config**:
   - If you are using the default `Umbraco.Web.Security.Providers.UsersMembershipProvider` class for `UsersMembershipProvider`, you don't need to do anything.

### Configure user account creation
This version of UmbBackOfficeMembershipProvider can automatically create Umbraco backoffice user accounts for authenticated users. If you want to enable this functionality, follow these instructions:

5. Insert the following `<appSettings>` keys in **web.config**:
   - `<add key="BackOfficeMembershipProvider:CreateAccounts" value="true" />` - set to `true` to enable automatic account creation
   - `<add key="BackOfficeMembershipProvider:AccountRoles" value="editor" />` - comma-separated list of groups user should be added to; defaults to **editor** if key is not present
   - `<add key="BackOfficeMembershipProvider:AccountCulture" value="en-US" />` - culture/language to use in creating new account; defaults to value of `umbracoDefaultUILanguage` if key is not present
   - `<add key="BackOfficeMembershipProvider:AccountEmailDomain" value="mydomain.com" />` - specifies domain name to be used in setting *username@accountemaildomain* e-mail address for newly created accounts; ignored if username is already a valid e-mail address, hostname of website is used otherwise if key is not present
   
### Configure fallback to internal login
By default, if an attempt login does not authenticate successfully against Active Directory, it fails. If you wish to try logins that fail Active Directory authentication against Umbraco's local user database before failing them entirely, you can enable the fallback option.
6. Insert the following `<appSettings>` key in **web.config**:
   - `<add key="BackOfficeMembershipProvider:FallbackToDefaultChecker" value="true" />` - set to `true` to enable fallback to autehntication against Umbraco's local user database
