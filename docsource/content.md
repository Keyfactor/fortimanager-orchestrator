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