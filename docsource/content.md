## Overview

The FortiManager Orchestrator Extension supports the Inventory, Add, and Removal of certificates against a specified Fortigate managed device.  Each Keyfactor Command Certificate Store will represent a single unique FortiManager instance + FortiManager ADOM + FortiManager device name mapping to a managed Fortigate device, and the Fortigate VDOM of the managed Fortigate device.  User has the option to automatically push modifications (add/remove of certificate) from FortiManager to the Fortigate device or sync manually later.  Use cases supported:

1. Inventory of all certificates in the VDOM scope of the managed Fortigate device.
2. Ability to add new certificates to a FortiManager device.
3. Ability to replace existing certificates to a FortiManager device.
4. Ability to remove existing **unbound** certificates from a FortiManager device.
5. 2-4 can optionally be followed with a sync locally to the Fortigate device being managed.

Use cases not supported:

1. The renewal or removal of certificates enrolled throught the internal Fortigate or FortiManager CA
2. The renewal or removal of factory certificates
3. Management of globally scoped certificates.
4. Removal of ANY certificate bound to a Fortigate object for the Fortigate device being managed by FortiManager.