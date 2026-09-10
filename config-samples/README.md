# Configuration samples

Copies of configuration files the Foundation libraries read. They are not
deployed from here; copy them to the location described below and edit them
there.

## material-library-rules.json

Lists which material libraries hold sheet metal materials. Materials from these
libraries can be assigned only to sheet metal bodies, and sheet metal bodies can
take materials only from these libraries.

**Where it goes:** the material library folder, beside the library `.xml`
files. That is `%UGII_CUSTOMER_DIR%\MATERIALS` when it exists, otherwise
`%UGII_BASE_DIR%\MATERIALS`. To keep it somewhere else — for example because
the library folder is under Program Files — set
`BANXOPEN_MATERIAL_LIBRARY_RULES` to the file's full path.

**Names** are library ids: the `.xml` file name without its extension. They are
matched exactly, ignoring case.

**Without the file**, a library counts as sheet metal when its name contains
"sheetmetal", ignoring spaces and case. A file that exists but is invalid, or
lists no libraries, stops both dialogs from starting rather than being ignored.
