## Overview

The FortiManager Orchestrator Extension enables certificate inventory, addition, and removal on FortiGate devices managed by a FortiManager instance. Managed devices can be found in the FortiManager UI under Device Manager → Device & Groups → Managed FortiGate.
Each Keyfactor Command Certificate Store maps to a unique combination of FortiManager instance, ADOM, device, and a specific VDOM on the managed FortiGate device. Certificate changes (add/remove) can be pushed to the FortiGate device automatically or synced manually at a later time.

Supported use cases:

1. Inventory all certificates for a specified FortiGate device and VDOM, as managed through FortiManager.
2. Add new certificates to a specified FortiGate device and VDOM.
3. Replace existing certificates on a specified FortiGate device and VDOM.
4. Remove existing unbound certificates from a specified FortiGate device and VDOM.
5. Any of the above (2–4) can optionally be followed by syncing changes down to the managed FortiGate device.

Unsupported use cases:

1. Certificates enrolled through the FortiGate or FortiManager internal CA.
2. Factory-installed certificates.
3. Globally scoped certificates on the managed FortiGate device.
4. Removal of any certificate that is bound to a FortiGate object (e.g., an SSL VPN or HTTPS server certificate).