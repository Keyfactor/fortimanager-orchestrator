v1.0.1
- Modify log/return messaging to indicate "possible" issue on sync after a Management job when real time syncing to Fortigate is enabled.  Return status from FortiManager
  that indicates an error does not definitively mean that the sync failed, but rather that the sync MAY have failed.  The sync may have succeeded, but the return status from FortiManager was not definitive.

v1.0
- Initial Version
