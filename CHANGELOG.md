# Change Log

## [1.5.1]

- Processing pipeline rework: text-based verdict prefilter on worker threads, background file writing via temp + replace, per-frame clean slicing — a no-op pass now costs ~zero on the main thread
- Add `SRDuplicateMode.None` to disable duplicate cleaning; `Default` is now the active strategy everywhere
- Add "Import Chunk (KB)" processing setting for byte-based reimport chunking

## [1.5.0]

- New FormerlySerializeType processor

## [1.4.5]

- Fix SRDemo-Editor platforms

## [1.4.4]

- Add copy, paste, and cut actions for managed references

## [1.4.3]

- Fixes

## [1.4.2]

- Processing optimize

## [1.4.1]

- Fixes

## [1.4.0]

- Change editor namespace
- New tools: Missing types cleaner, Missing types logger, Type replace tool
- New asset processing on change: Double cleaner, Type replacer (FormerlySerializeType)

## [1.3.14]

- Change CreateAssetMenu path for SRMissingTypesValidatorConfig
- Update changelog

## [1.3.13]

- Bugfix
- Update IDE packages

## [1.3.12]

- Add SerializeReference field with SR attribute correct error

## [1.3.11]

- TypeInfoComparer fix

## [1.3.10]

- SRHidden fix

## [1.3.9]

- Fix error with editor namespace on build (thanks @Red-Cat-Fat)

## [1.3.8]

- DropdownButton fix

## [1.3.7]

- SRDrawer.GetButtonWidth added

## [1.3.6]

- Type upgrader fix
- Duplicate cleaner fix

## [1.3.5]

- Reselection fix

## [1.3.3]

- Fix namespace and assembly replace

## [1.3.2]

- Added SRHidden attribute, for hide type at select list
- Update Demo files

## [1.3.1]

- Added SRDrawerOptions to Draw method

## [1.3.0]

New Features:

1. Class Replacer
- Added new tool for replacing Serialize Reference classes
- Access via Tools -> SREditor -> Class Replacer

2. FormerlySerializedType attribute
- Added new attribute for handling class renaming/refactoring
- Works similar to FormerlySerializedAs but for Serialize Reference classes

3. Duplicate Cleaner
- Added new system for handling SerializeReference object duplicates
- Configurable settings: nullify, create with default values, or make deep copies
- Prevents issues with unwanted reference sharing in assets

4. SRDrawer improvements
- Added comprehensive Draw method with support for:
  - Dynamic type resolution
  - Type selection button
  - Array elements support
  - SearchWindow integration