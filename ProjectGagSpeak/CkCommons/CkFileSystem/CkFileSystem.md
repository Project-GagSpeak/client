# CK File System
This File System is an adaptation of OtterGui's FileSystem structure with some key differences.

- The File System no longer uses a Tree Node structure. This renders it slightly less optimal (roughly 10% additional total drawtime.
- This increase in drawtime is due to no longer using ImGui.StateStorage() to store path state references.
- State references are now stored in the path items themselves.
- New structure allows you to fully customize both folder and file nodes.
- Allows you to set custom sort orders and custom filter orders.
- Allows you to control the order of execution that the filter orders go off in when organizing.
