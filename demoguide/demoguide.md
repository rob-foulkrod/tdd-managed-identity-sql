[comment]: <> (CONTRIBUTOR: Replace [YOUR-PLACEHOLDER] text and add screenshots to screenshots subfolder)
[comment]: <> (Keep the ***, <div> elements, and section 1/2 titles for consistency with other demoguides)


[comment]: <> (this is the section for the Note: item; please do not make any changes here)
***
### Azure Managed Identity Demo: System vs User-Assigned (Azure SQL)

<div style="background: lightgreen; 
            font-size: 14px; 
            color: black;
            padding: 5px; 
            border: 1px solid lightgray; 
            margin: 5px;">

**Note:** Below demo steps should be used **as a guideline** for doing your own demos. Please consider contributing to add additional demo steps.
</div>

[comment]: <> (this is the section for the Tip: item; consider adding a Tip, or remove the section between <div> and </div> if there is no tip)

***
### 1. What Resources are getting deployed

This scenario deploys a simple product-catalog web app and an Azure SQL Database loaded with the AdventureWorksLT sample. The app authenticates to Azure SQL using a **System-assigned Managed Identity** by default, and includes a **User-assigned Managed Identity** code path that can be enabled by configuration to demonstrate a clean identity migration.

Provide a bullet list of the Resource Group and all deployed resources with name and brief functionality within the scenario: 

* rg-[environmentname] - Azure Resource Group
* plan-[token] - App Service plan hosting the web app
* app-[token] - Azure App Service (Linux) hosting the ASP.NET Core catalog site (System-assigned MI enabled)
* sql-[token] - Azure SQL Server
* sqldb-[token] - Azure SQL Database (AdventureWorksLT sample)
* id-[token] - User-assigned Managed Identity (created for the conversion demo)

[Add a screenshot of the deployed Resource Group with resources]

<img src="https://raw.githubusercontent.com/rob-foulkrod/tdd-managed-identity-sql/main/demoguide/screenshots/screenshot1.png" alt="[Your Resource Group Description]" style="width:70%;">
<br></br>

<img src="https://raw.githubusercontent.com/rob-foulkrod/tdd-managed-identity-sql/main/demoguide/screenshots/screenshot2.png" alt="[Your Resource Details]" style="width:70%;">
<br></br>

### 2. What can I demo from this scenario after deployment

This demo has two parts:

1) Use **System-assigned Managed Identity** (default)
2) Convert the app to **User-assigned Managed Identity** by changing only Azure configuration (plus a matching database user)

Before the app can query Azure SQL using Managed Identity, the database must contain a user for the managed identity.

> Note: This template includes an `azd` `postprovision` hook that can automatically set the SQL Azure AD admin (when permitted) and create a least-privilege database role (`catalog_reader`) with the managed identity users already added.

> Optional (recommended for teaching): This template uses a hook-based prompt during `azd up`.
> - When prompted **"Run automatic SQL bootstrap after provision? (Y/n)"**, choose:
>   - `Y` (default) to auto-create the `catalog_reader` role and identity users
>   - `N` to skip automation so you can run the SQL steps manually (or demo a failure first)
>
> You can also pre-set the choice (avoids prompting) for the current azd environment:
> - `azd env set SQL_BOOTSTRAP_ENABLED false`
> - Re-run `azd provision` / `azd up` (the SQL bootstrap step will be skipped)

**Demo Steps:**
1. Get the deployment outputs
    1. Run `azd env get-values`.
    2. Note these values (used throughout the demo):
        - `APP_ENDPOINT`
        - `APP_SERVICE_NAME`
        - `SQL_SERVER_FQDN`
        - `SQL_DATABASE_NAME`
        - `USER_ASSIGNED_MI_NAME`
        - `USER_ASSIGNED_MI_CLIENT_ID`

2. Configure Azure SQL for System-assigned Managed Identity
    1. In the Azure Portal, open the deployed SQL server (sql-[token]).
    2. Set an **Azure AD admin** for the SQL server (use your instructor account).
    3. Open the deployed SQL database (sqldb-[token]) and select **Query editor** in the Azure Portal.
        - Sign in with the same account you configured as the SQL Azure AD admin.
    4. If the `postprovision` hook did not run (or you want to show the manual steps), run the following SQL to create a custom role and authorize the system-assigned identity user:

        ```sql
        CREATE ROLE [catalog_reader];
        GRANT SELECT ON SCHEMA::SalesLT TO [catalog_reader];

        CREATE USER [<APP_SERVICE_NAME>] FROM EXTERNAL PROVIDER;
        ALTER ROLE [catalog_reader] ADD MEMBER [<APP_SERVICE_NAME>];
        ```

        Tip: Replace `<APP_SERVICE_NAME>` with the output value (example: `app-abc123`).

        5. (Optional) Confirm the role, permissions, and membership

                ```sql

                    -- Confirm the role exists
                    select name
                    from sys.database_principals
                    where type = 'R'
                        and name = 'catalog_reader';
    
                    -- Confirm schema permission was granted
                    select
                            dp.state_desc,
                            dp.permission_name,
                            s.name as schema_name,
                            grantee.name as grantee
                    from sys.database_permissions dp
                    join sys.database_principals grantee on grantee.principal_id = dp.grantee_principal_id
                    left join sys.schemas s on s.schema_id = dp.major_id
                    where grantee.name = 'catalog_reader'
                        and dp.class_desc = 'SCHEMA';
    
                    -- Confirm the system-assigned identity is a member of the role
                    select
                            rolep.name as role_name,
                            memberp.name as member_name
                    from sys.database_role_members drm
                    join sys.database_principals rolep on rolep.principal_id = drm.role_principal_id
                    join sys.database_principals memberp on memberp.principal_id = drm.member_principal_id
                    where rolep.name = 'catalog_reader'
                        and memberp.name = '<APP_SERVICE_NAME>';

                ```

3. Verify the app is using System-assigned Managed Identity
    1. Browse to `APP_ENDPOINT`.
    2. Open **Products** and apply a category filter or name search.
    3. In the navbar, note the identity label shows **System-assigned**.

4. Convert to User-assigned Managed Identity (instructor-led)
    1. In the Azure Portal, open the App Service (app-[token]).
    2. Go to **Identity**.
        - (Optional) Under **System assigned**, set **Status** to **Off** and **Save**.
        - Under **User assigned**, click **Add**, then select the deployed identity (id-[token]).

    3. (Optional failure demo) Do **not** set the `ManagedIdentity__UserAssignedClientId` application setting yet.
        1. Browse to `APP_ENDPOINT` and open **Products**.
        2. Expected result: the page shows an error (unable to query Azure SQL) because the app defaults to the System-assigned managed identity unless a User-assigned client id is configured.
        3. Show the code path that selects which identity to use:
            - Connection factory: [src/web/ManagedIdentityCatalog/Services/SqlConnectionFactory.cs](src/web/ManagedIdentityCatalog/Services/SqlConnectionFactory.cs)
            - Identity label (based on config): [src/web/ManagedIdentityCatalog/Services/IdentityModeProvider.cs](src/web/ManagedIdentityCatalog/Services/IdentityModeProvider.cs)

    4. Fix the configuration
        1. Go to **Configuration** and set the app setting:
            - `ManagedIdentity__UserAssignedClientId` = `USER_ASSIGNED_MI_CLIENT_ID`
        2. Save changes and restart the App Service if prompted.

    5. In the Azure Portal **Query editor**, create and authorize the user-assigned identity in the database (skip if the `postprovision` hook already created it):

        ```sql
        CREATE USER [<USER_ASSIGNED_MI_NAME>] FROM EXTERNAL PROVIDER;
        ALTER ROLE [catalog_reader] ADD MEMBER [<USER_ASSIGNED_MI_NAME>];
        ```

        6. (Optional) Confirm the user-assigned identity is a member of the role

                ```sql
                select
                        rolep.name as role_name,
                        memberp.name as member_name
                from sys.database_role_members drm
                join sys.database_principals rolep on rolep.principal_id = drm.role_principal_id
                join sys.database_principals memberp on memberp.principal_id = drm.member_principal_id
                where rolep.name = 'catalog_reader'
                    and memberp.name = '<USER_ASSIGNED_MI_NAME>';
                ```

5. Verify the app is now using User-assigned Managed Identity
    1. Refresh the **Products** page.
    2. Confirm the navbar identity label shows **User-assigned**.
    3. Confirm filtering still works.

Optional wrap-up discussion:
- Remove the System-assigned identity user from the database and/or disable the app’s System-assigned identity to demonstrate that only the User-assigned identity is now required.

Add screenshots where relevant. They should be stored in their own subfolder under the demoguide folder.



[comment]: <> (this is the closing section of the demo steps. Please do not change anything here to keep the layout consistant with the other demoguides.)
<br></br>
***
<div style="background: lightgray; 
            font-size: 14px; 
            color: black;
            padding: 5px; 
            border: 1px solid lightgray; 
            margin: 5px;">

**Note:** This is the end of the current demo guide instructions.
</div>




