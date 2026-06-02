<h1 align="center" style="border-bottom: none">
    Integration Template Universal Orchestrator Extension
</h1>

<p align="center">
  <!-- Badges -->
<img src="https://img.shields.io/badge/integration_status-prototype-3D1973?style=flat-square" alt="Integration Status: prototype" />
<a href="https://github.com/Keyfactor/fortimanager-orchestrator/releases"><img src="https://img.shields.io/github/v/release/Keyfactor/fortimanager-orchestrator?style=flat-square" alt="Release" /></a>
<img src="https://img.shields.io/github/issues/Keyfactor/fortimanager-orchestrator?style=flat-square" alt="Issues" />
<img src="https://img.shields.io/github/downloads/Keyfactor/fortimanager-orchestrator/total?style=flat-square&label=downloads&color=28B905" alt="GitHub Downloads (all assets, all releases)" />
</p>

<p align="center">
  <!-- TOC -->
  <a href="#support">
    <b>Support</b>
  </a>
  ·
  <a href="#installation">
    <b>Installation</b>
  </a>
  ·
  <a href="#license">
    <b>License</b>
  </a>
  ·
  <a href="https://github.com/orgs/Keyfactor/repositories?q=orchestrator">
    <b>Related Integrations</b>
  </a>
</p>

## Overview

The FortiManager Orchestrator Extension supports the Inventory, Add, and Removal of certificates against a specified Fortigate device managed by the targeted FortiManager instance.  These devices can be found in the FortiManager UI
by going to Device Manager => Device & Groups => Managed FortiGate.  Each Keyfactor Command Certificate Store represents a single unique FortiManager instance + FortiManager ADOM + FortiManager device mapped to a managed Fortigate device, 
as well as a specified VDOM of the managed Fortigate device.  The user also has the option to automatically push/sync FortiManager device modifications (add/removal of certificate) to the Fortigate device or sync manually later.  Use cases supported:

1. Inventory of all certificates for the specified FortiManager device mapped to a specified Fortigate device and specified Fortigate VDOM.
2. Ability to add new certificates to a specified FortiManager device mapped to a specified Fortigate device and specified Fortigate VDOM.
3. Ability to replace existing certificates on a specified FortiManager device mapped to a specified Fortigate device and specified Fortigate VDOM.
4. Ability to remove existing **unbound** certificates from a specified FortiManager device mapped to a specified Fortigate device and specified Fortigate VDOM.
5. 2-4 can optionally be followed by syncing locally to the Fortigate device being managed.

Use cases not supported:

1. Management of certificates enrolled throught the internal Fortigate or FortiManager CA
2. Management of factory certificates
3. Management of globally scoped certificates on the Fortigate device the FortiManager device is mapped to.
4. Removal of ANY certificate bound to a managed Fortigate object (i.e. SSL VPN or HTTPS server certificate).

## Compatibility

This integration is compatible with Keyfactor Universal Orchestrator version 10.4 and later.

## Support

The Integration Template Universal Orchestrator extension is supported by Keyfactor. If you require support for any issues or have feature request, please open a support ticket by either contacting your Keyfactor representative or via the Keyfactor Support Portal at https://support.keyfactor.com.

> If you want to contribute bug fixes or additional enhancements, use the **[Pull requests](../../pulls)** tab.

## Requirements & Prerequisites

Before installing the Integration Template Universal Orchestrator extension, we recommend that you install [kfutil](https://github.com/Keyfactor/kfutil). Kfutil is a command-line tool that simplifies the process of creating store types, installing extensions, and instantiating certificate stores in Keyfactor Command.

## FortiManager Certificate Store Type

To use the Integration Template Universal Orchestrator extension, you **must** create the FortiManager Certificate Store Type. This only needs to happen _once_ per Keyfactor Command instance.



#### Supported Operations

| Operation    | Is Supported |
|--------------|--------------|
| Add          | ✅ Checked |
| Remove       | ✅ Checked |
| Discovery    | 🔲 Unchecked |
| Reenrollment | 🔲 Unchecked |
| Create       | 🔲 Unchecked |

#### Store Type Creation

##### Using kfutil:
`kfutil` is a custom CLI for the Keyfactor Command API and can be used to create certificate store types.
For more information on [kfutil](https://github.com/Keyfactor/kfutil) check out the [docs](https://github.com/Keyfactor/kfutil?tab=readme-ov-file#quickstart)

   <details><summary>Click to expand FortiManager kfutil details</summary>

   ##### Using online definition from GitHub:
   This will reach out to GitHub and pull the latest store-type definition
   ```shell
   # FortiManager
   kfutil store-types create FortiManager
   ```

   ##### Offline creation using integration-manifest file:
   If required, it is possible to create store types from the [integration-manifest.json](./integration-manifest.json) included in this repo.
   You would first download the [integration-manifest.json](./integration-manifest.json) and then run the following command
   in your offline environment.
   ```shell
   kfutil store-types create --from-file integration-manifest.json
   ```
   </details>

#### Manual Creation
Below are instructions on how to create the FortiManager store type manually in
the Keyfactor Command Portal

   <details><summary>Click to expand manual FortiManager details</summary>

   Create a store type called `FortiManager` with the attributes in the tables below:

   ##### Basic Tab
   | Attribute | Value | Description |
   | --------- | ----- | ----- |
   | Name | FortiManager | Display name for the store type (may be customized) |
   | Short Name | FortiManager | Short display name for the store type |
   | Capability | FortiManager | Store type name orchestrator will register with. Check the box to allow entry of value |
   | Supports Add | ✅ Checked | Indicates that the Store Type supports Management Add |
   | Supports Remove | ✅ Checked | Indicates that the Store Type supports Management Remove |
   | Supports Discovery | 🔲 Unchecked | Indicates that the Store Type supports Discovery |
   | Supports Reenrollment | 🔲 Unchecked | Indicates that the Store Type supports Reenrollment |
   | Supports Create | 🔲 Unchecked | Indicates that the Store Type supports store creation |
   | Needs Server | ✅ Checked | Determines if a target server name is required when creating store |
   | Blueprint Allowed | 🔲 Unchecked | Determines if store type may be included in an Orchestrator blueprint |
   | Uses PowerShell | 🔲 Unchecked | Determines if underlying implementation is PowerShell |
   | Requires Store Password | 🔲 Unchecked | Enables users to optionally specify a store password when defining a Certificate Store. |
   | Supports Entry Password | 🔲 Unchecked | Determines if an individual entry within a store can have a password. |

   The Basic tab should look like this:

   ![FortiManager Basic Tab](docsource/images/FortiManager-basic-store-type-dialog.svg)

   ##### Advanced Tab
   | Attribute | Value | Description |
   | --------- | ----- | ----- |
   | Supports Custom Alias | Required | Determines if an individual entry within a store can have a custom Alias. |
   | Private Key Handling | Required | This determines if Keyfactor can send the private key associated with a certificate to the store. |
   | PFX Password Style | Default | 'Default' - PFX password is randomly generated, 'Custom' - PFX password may be specified when the enrollment job is created (Requires the Allow Custom Password application setting to be enabled.) |

   The Advanced tab should look like this:

   ![FortiManager Advanced Tab](docsource/images/FortiManager-advanced-store-type-dialog.svg)

   > For Keyfactor **Command versions 24.4 and later**, a Certificate Format dropdown is available with PFX and PEM options. Ensure that **PFX** is selected, as this determines the format of new and renewed certificates sent to the Orchestrator during a Management job. Currently, all Keyfactor-supported Orchestrator extensions support only PFX.

   ##### Custom Fields Tab
   Custom fields operate at the certificate store level and are used to control how the orchestrator connects to the remote target server containing the certificate store to be managed. The following custom fields should be added to the store type:

   | Name | Display Name | Description | Type | Default Value/Options | Required |
   | ---- | ------------ | ---- | --------------------- | -------- | ----------- |
   | SyncFortigateDevice | Sync Fortigate Device After Update | Set to true if you want to push any modifications automatically to the managed Fortigate device.  Set to false if you only want to make modifications on the FortiManager device and sync later. | Bool | false | ✅ Checked |
   | ServerUsername | Server Username | User ID for the FortiManager instance you are managing. | Secret |  | 🔲 Unchecked |
   | ServerPassword | Server Password | Password for the FortiManager instance you are managing | Secret |  | 🔲 Unchecked |
   | ServerUseSsl | Use SSL | Not Used | Bool | true | ✅ Checked |

   The Custom Fields tab should look like this:

   ![FortiManager Custom Fields Tab](docsource/images/FortiManager-custom-fields-store-type-dialog.svg)

   ###### Sync Fortigate Device After Update
   Set to true if you want to push any modifications automatically to the managed Fortigate device.  Set to false if you only want to make modifications on the FortiManager device and sync later.

   ![FortiManager Custom Field - SyncFortigateDevice](docsource/images/FortiManager-custom-field-SyncFortigateDevice-dialog.svg)


   ###### Server Username
   User ID for the FortiManager instance you are managing.


   > [!IMPORTANT]
   > This field is created by the `Needs Server` on the Basic tab, do not create this field manually.


   ###### Server Password
   Password for the FortiManager instance you are managing


   > [!IMPORTANT]
   > This field is created by the `Needs Server` on the Basic tab, do not create this field manually.


   ###### Use SSL
   Not Used

   ![FortiManager Custom Field - ServerUseSsl](docsource/images/FortiManager-custom-field-ServerUseSsl-dialog.svg)


   </details>

## Installation

1. **Download the latest Integration Template Universal Orchestrator extension from GitHub.**

    Navigate to the [Integration Template Universal Orchestrator extension GitHub version page](https://github.com/Keyfactor/fortimanager-orchestrator/releases/latest). Refer to the compatibility matrix below to determine which asset should be downloaded. Then, click the corresponding asset to download the zip archive.

   | Universal Orchestrator Version | Latest .NET version installed on the Universal Orchestrator server | `rollForward` condition in `Orchestrator.runtimeconfig.json` | `fortimanager-orchestrator` .NET version to download |
   | --------- | ----------- | ----------- | ----------- |
   | Between `11.0.0` and `11.5.1` (inclusive) | `net8.0` | `LatestMajor` | `net8.0` |
   | `11.6` _and_ newer | `net8.0` | | `net8.0` |

    Unzip the archive containing extension assemblies to a known location.

    > **Note** If you don't see an asset with a corresponding .NET version, you should always assume that it was compiled for `net8.0`.

2. **Locate the Universal Orchestrator extensions directory.**

    * **Default on Windows** - `C:\Program Files\Keyfactor\Keyfactor Orchestrator\extensions`
    * **Default on Linux** - `/opt/keyfactor/orchestrator/extensions`

3. **Create a new directory for the Integration Template Universal Orchestrator extension inside the extensions directory.**

    Create a new directory called `fortimanager-orchestrator`.
    > The directory name does not need to match any names used elsewhere; it just has to be unique within the extensions directory.

4. **Copy the contents of the downloaded and unzipped assemblies from __step 2__ to the `fortimanager-orchestrator` directory.**

5. **Restart the Universal Orchestrator service.**

    Refer to [Starting/Restarting the Universal Orchestrator service](https://software.keyfactor.com/Core-OnPrem/Current/Content/InstallingAgents/NetCoreOrchestrator/StarttheService.htm).

6. **(optional) PAM Integration**

    The Integration Template Universal Orchestrator extension is compatible with all supported Keyfactor PAM extensions to resolve PAM-eligible secrets. PAM extensions running on Universal Orchestrators enable secure retrieval of secrets from a connected PAM provider.

    To configure a PAM provider, [reference the Keyfactor Integration Catalog](https://keyfactor.github.io/integrations-catalog/content/pam) to select an extension and follow the associated instructions to install it on the Universal Orchestrator (remote).

> The above installation steps can be supplemented by the [official Command documentation](https://software.keyfactor.com/Core-OnPrem/Current/Content/InstallingAgents/NetCoreOrchestrator/CustomExtensions.htm?Highlight=extensions).

## Defining Certificate Stores

### Store Creation

#### Manually with the Command UI

<details><summary>Click to expand details</summary>

1. **Navigate to the _Certificate Stores_ page in Keyfactor Command.**

    Log into Keyfactor Command, toggle the _Locations_ dropdown, and click _Certificate Stores_.

2. **Add a Certificate Store.**

    Click the Add button to add a new Certificate Store. Use the table below to populate the **Attributes** in the **Add** form.

   | Attribute | Description |
   | --------- | ----------- |
   | Category | Select "FortiManager" or the customized certificate store name from the previous step. |
   | Container | Optional container to associate certificate store with. |
   | Client Machine | The IP address or DNS of the FortiManager server |
   | Store Path | Value must contain the the FortiManager ADOM, FortiManager device name, and Fortigate VDOM this certificate store will be managing using the pipe character as a delimiter.  For example - root&#124;Device1&#124;root |
   | Orchestrator | Select an approved orchestrator capable of managing `FortiManager` certificates. Specifically, one with the `FortiManager` capability. |
   | SyncFortigateDevice | Set to true if you want to push any modifications automatically to the managed Fortigate device.  Set to false if you only want to make modifications on the FortiManager device and sync later. |
   | ServerUsername | User ID for the FortiManager instance you are managing. |
   | ServerPassword | Password for the FortiManager instance you are managing |
   | ServerUseSsl | Not Used |

</details>

#### Using kfutil CLI

<details><summary>Click to expand details</summary>

1. **Generate a CSV template for the FortiManager certificate store**

    ```shell
    kfutil stores import generate-template --store-type-name FortiManager --outpath FortiManager.csv
    ```
2. **Populate the generated CSV file**

    Open the CSV file, and reference the table below to populate parameters for each **Attribute**.

   | Attribute | Description |
   | --------- | ----------- |
   | Category | Select "FortiManager" or the customized certificate store name from the previous step. |
   | Container | Optional container to associate certificate store with. |
   | Client Machine | The IP address or DNS of the FortiManager server |
   | Store Path | Value must contain the the FortiManager ADOM, FortiManager device name, and Fortigate VDOM this certificate store will be managing using the pipe character as a delimiter.  For example - root&#124;Device1&#124;root |
   | Orchestrator | Select an approved orchestrator capable of managing `FortiManager` certificates. Specifically, one with the `FortiManager` capability. |
   | Properties.SyncFortigateDevice | Set to true if you want to push any modifications automatically to the managed Fortigate device.  Set to false if you only want to make modifications on the FortiManager device and sync later. |
   | Properties.ServerUsername | User ID for the FortiManager instance you are managing. |
   | Properties.ServerPassword | Password for the FortiManager instance you are managing |
   | Properties.ServerUseSsl | Not Used |

3. **Import the CSV file to create the certificate stores**

    ```shell
    kfutil stores import csv --store-type-name FortiManager --file FortiManager.csv
    ```

</details>

#### PAM Provider Eligible Fields
<details><summary>Attributes eligible for retrieval by a PAM Provider on the Universal Orchestrator</summary>

If a PAM provider was installed _on the Universal Orchestrator_ in the [Installation](#Installation) section, the following parameters can be configured for retrieval _on the Universal Orchestrator_.

   | Attribute | Description |
   | --------- | ----------- |
   | ServerUsername | User ID for the FortiManager instance you are managing. |
   | ServerPassword | Password for the FortiManager instance you are managing |

Please refer to the **Universal Orchestrator (remote)** usage section ([PAM providers on the Keyfactor Integration Catalog](https://keyfactor.github.io/integrations-catalog/content/pam)) for your selected PAM provider for instructions on how to load attributes orchestrator-side.
> Any secret can be rendered by a PAM provider _installed on the Keyfactor Command server_. The above parameters are specific to attributes that can be fetched by an installed PAM provider running on the Universal Orchestrator server itself.

</details>

> The content in this section can be supplemented by the [official Command documentation](https://software.keyfactor.com/Core-OnPrem/Current/Content/ReferenceGuide/Certificate%20Stores.htm?Highlight=certificate%20store).


## License

Apache License 2.0, see [LICENSE](LICENSE).

## Related Integrations

See all [Keyfactor Universal Orchestrator extensions](https://github.com/orgs/Keyfactor/repositories?q=orchestrator).
