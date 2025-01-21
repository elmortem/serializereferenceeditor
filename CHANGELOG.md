# 1.3.2

- Added SRHidden attribute, for hide type at select list
- Update Demo files

# 1.3.1

- Added SRDrawerOptions to Draw method

# 1.3.0

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