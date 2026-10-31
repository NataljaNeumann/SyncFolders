---

applyTo: "SyncFolders/**/*.cs"
applyTo: "SyncFoldersApi/**/*.cs"
applyTo: "SyncFoldersTests/**/*.cs"

---

<!-- This project is licensed under GPL-2.0-or-later. See ../../LICENSE.TXT for the complete license text. -->

# General Instructions

These instructions apply only to source files matching `applyTo`. Project-specific `.editorconfig`, `CONTRIBUTING.md`, and build configuration take precedence if they define a conflicting rule.

Use Hungarian notation. The prefix for bool is "b", the prefix for date and time is "dtm", the prefix for float is "f", the prefix for double is "dbl", the prefix for byte is "by", prefix for string is "str", the prefix for ints is "n", the prefix for longs is "l", the prefix for chars is "c", the prefix for arrays is "a", the prefix of arrays of strings is "astr", the prefix for text boxes is "tbx", the prefix for labels is "lbl", the prefix for check boxes is "chk", the prefix for buttons is "btn", the prefix for other controls is "ctl", the prefix for other objects is "o". If variables represent physical values, (e.g. pixels, meters, inches, circular degrees, etc.) then they shall have an additional prefix that represents the units of the variable. Member variables have an additional prefix "m_". Static member variables have an additional prefix "s_", instead of "m_".

Document the source code, functions, and their parameters. The documentation must be in English. Be generous in commenting the code, explain the situation and what shall be done. XML documentation may use `<b>`, `</b>`, `<i>`, `</i>`, and `<br/>` to format text in IntelliSense. Use `<br/><br/>` to separate paragraphs.

If a string is compared to null, then use == or != comparison. If a string is compared to another string that is not null, then use functions CompareTo or Equals.

When a string shall be composed of several parts, please put the result to a string variable, before using it. Explicitly declare that the variable is of type string.

When a function with more than two parameters shall be called, please use multiline syntax for calling, put different parameters into different lines, so the line doesn't become too long.

Put every parameter into a separate line in function definitions.

Explicitly declare the types of variables, don't use "var".

If an object is created that implements IDisposable interface, then please take care of its disposal. Either it must be stored in another object that will dispose it, or you need to dispose it explicitly after usage.

If a COM object is created then please take care of its release. Please take care about reference count of COM objects.

Please take care of localization/internationalization of source code. The used strings need to be taken from resources, so they are localizable for different cultures.

Avoid known security issues.

## Reference Implementations

Use these files as examples when working with file-system abstractions:

- [`IDirectoryInfo`](../../SyncFoldersApi/FileSystem/IDirectoryInfo.cs)
- [`IFileInfo`](../../SyncFoldersApi/FileSystem/IFileInfo.cs)
- [`IFileOperations`](../../SyncFoldersApi/FileSystem/IFileOperations.cs)
- [`IFile`](../../SyncFoldersApi/FileSystem/IFile.cs)

When modifying related code, inspect these files and their public contracts, nullability, naming conventions, and disposal behavior. Use these abstractions, instead of direct access of the files. If needed, then expand these interfaces. There is an in-memory and a file system implementation of these interfaces.


See these files for the available settings, when processing a file pair:

- [`IFilePairStepsSetting`](../../SyncFoldersApi/IFilePairStepsSetting.cs)
- [`ICancelable`](../../SyncFoldersApi/ICancelable.cs)


There are many missing unit tests, the most important examples for creation of new unit tests are in following files:

- [`T040_FilePairStepsTest.cs`](../../SyncFoldersApi/SyncFoldersTests/T040_FilePairStepsTest.cs)
- [`T090_MostCommonSituations.cs`](../../SyncFoldersApi/SyncFoldersTests/T090_MostCommonSituations.cs)
- [`T100_UncommonSituations.cs`](../../SyncFoldersApi/SyncFoldersTests/T100_UncommonSituations.cs)

If a method must throw an exception inside a test, then instead of expecting an exception outside of a unit test catch the exception inside the test and verify that it was there. This is faster and more convenient.

## Licensing

This project is licensed under GNU GPL version 2 or later (`GPL-2.0-or-later`).

Generated or modified source code must remain compatible with the project's license. 
Preserve existing copyright and license notices. See [`LICENSE.TXT`](../../LICENSE.TXT) for the complete license text.
