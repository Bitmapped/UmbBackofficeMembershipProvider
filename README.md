# UmbBackofficeMembershipProvider
Code to allow Umbraco 7.4.2+ to use MembershipProvider-based providers for Active Directory authentication.

## What's inside
This project includes a DLL that will allow you to use a traditional `MembershipProvider` for logging in Umbraco backoffice users.

## System requirements
1. NET Framework 4.5
2. Umbraco 7.4.2+

# NuGet availability
This project is available on [NuGet](https://www.nuget.org/packages/UmbBackofficeMembershipProvider/).

## Usage instructions
### Getting started
1. Before making any configuration file changes, make sure that you have an Administrator-level user account in Umbraco with the same username as the Active Directory account that you will use to login to Umbraco. It doesn't matter what you set for the password once UmbBackofficeMembershipProvider is enabled as it will check against Active Directory and not Umbraco for the password.

### Installing UmbBackofficeMembershipProvider
1. Add **UmbBackofficeMembershipProvider.dll** as a reference in your project or place it in the **\bin** folder.
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
        <add
          name="UsersMembershipProvider"
          type="Umbraco.Web.Security.Providers.UsersMembershipProvider, Umbraco"
          minRequiredNonalphanumericCharacters="0" minRequiredPasswordLength="8"
          useLegacyEncoding="true" enablePasswordRetrieval="false"
          enablePasswordReset="true" requiresQuestionAndAnswer="false"
          passwordFormat="Hashed" />
      </providers>
     </membership>
 ```
 4. In **config\UmbracoSettings.config**:
   - If you are using the default `Umbraco.Web.Security.Providers.UsersMembershipProvider` class for `UsersMembershipProvider`, you don't need to do anything.

### User accounts
In versions of Umbraco before 7.3.0, Umbraco automatically creates Umbraco user accounts for Active Directory users on first login. In versions 7.3.0 and newer, an administrator must create an Umbraco user account (use the same username) first before an Active Directory user can login. Be careful that you've created an Administrator-level account with the same username as your Active Directory account before enabling UmbBackofficeMembershipProvider.

It does not matter what password you use for local Umbraco accounts. Umbraco will authenticate against Active Directory rather than checking the locally stored passwords.
