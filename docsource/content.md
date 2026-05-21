## Overview

The FortiManager Orchestrator Extension supports the Inventory, Add, and Removal of certificates against a specified Fortigate deviced managed by the targeted FortiManager instance.  These devices can be found in the FortiManager UI
by going to Device Manager => Device & Groups => Managed FortiGate.  Each Keyfactor Command Certificate Store represents a single unique FortiManager instance + FortiManager ADOM + FortiManager device mapped to a managed Fortigate device, 
and the Fortigate VDOM of the managed Fortigate device.  The user has the option to automatically push/sync FortiManager device modifications (add/removal of certificate) to the Fortigate device or sync manually later.  Use cases supported:

1. Inventory of all certificates for the specified FortiManager device mapped to a specified Fortigate device and specified Fortigate VDOM.
2. Ability to add new certificates to a specified FortiManager device mapped to a specified Fortigate device and specified Fortigate VDOM.
3. Ability to replace existing certificates on a specified FortiManager device mapped to a specified Fortigate device and specified Fortigate VDOM.
4. Ability to remove existing **unbound** certificates from a specified FortiManager device mapped to a specified Fortigate device and specified Fortigate VDOM.
5. 2-4 can optionally be followed by syncing locally to the Fortigate device being managed.

Use cases not supported:

1. The renewal or removal of certificates enrolled throught the internal Fortigate or FortiManager CA
2. The renewal or removal of factory certificates
3. Management of globally scoped certificates on the Fortigate device the FortiManager device is mapped to.
4. Removal of ANY certificate bound to a Fortigate object for the Fortigate device being managed by FortiManager.