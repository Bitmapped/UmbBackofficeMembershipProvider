# UmbMembershipProvider
Code to allow Umbraco 7.3.1+ to use MembershipProvider-based providers for Active Directory authentication.

## What's inside
This project includes a DLL that will allow you to use a traditional `MembershipProvider` for logging in Umbraco backoffice users.

## System requirements
1. NET Framework 4.5
2. Umbraco 7.3.1+

# NuGet availability
This project is available on [NuGet](https://www.nuget.org/packages/UmbMembershipProvider/).

## Usage instructions
### Getting started
1. Add **UmbBackofficeMembershipProvider.dll** as a reference in your project or place it in the **\bin** folder.
2. In **web.config**, make the following two modifications:
  - Add or modify the following line in the `<appSettings>` section:

    ```
    <add key="owin:appStartup" value="BackofficeMembershipProviderCustomOwinStartup" />
    ```
  
  - Add or modify a membership provider named `BackofficeMembershipProvider`, like shown in the example code below:
  
    ```
    <membership defaultProvider="UmbracoMembershipProvider">
      <providers>
        <add
           name="BackofficeMembershipProvider"
           type="System.Web.Security.ActiveDirectoryMembershipProvider, System.Web, Version=2.0.0.0, 
                 Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
           connectionStringName="ADConnectionString"
           connectionUsername="testdomain\administrator" 
           connectionPassword="password"/>
      </providers>
     </membership>
 ```
