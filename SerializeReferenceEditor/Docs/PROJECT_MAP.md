# Карта проекта SerializeReferenceEditor

Навигация по `Assets/SREditor/`. Обновляй при изменении структуры (новые папки, подсистемы, точки входа).

## Назначение

Unity-инструмент для редактирования полей `[SerializeReference]`: выбор конкретного типа из выпадающего меню в инспекторе, переименование типов при рефакторинге через `FormerlySerializedType`, очистка дубликатов managed-ссылок и missing-типов.

## Сборки (asmdef) и зависимости

| Asmdef | Папка | Платформы | Назначение | Ссылается на |
|---|---|---|---|---|
| `SREditor` | `Package/Scripts` | Все | Рантайм-атрибуты, доступные пользовательскому коду | — |
| `SREditor-Editor` | `Package/Editor` | Editor | Вся редакторная логика | `SREditor` |
| `SRDemo` | `Samples/Demo` | Все | Демо-данные | `SREditor` |
| `SRDemo-Editor` | `Samples/Demo/Editor` | Editor | Демо drawer | `SREditor`, `SRDemo`, `SREditor-Editor` |
| `NewTest` | `Samples/Demo/NewTests` | — | Тестовый ассембли для проверки замены типов между сборками | — |

Корневой namespace рантайма — `SerializeReferenceEditor`, редактора — `SerializeReferenceEditor.Editor`.
Пакет: `com.elmortem.serializereferenceeditor`, версия в `Package/package.json`.

## Структура папок

```
Assets/SREditor/
├── Package/
│   ├── Scripts/                    # Рантайм (asmdef SREditor) — атрибуты
│   │   ├── SRAttribute.cs          # Главный атрибут поля [SR]
│   │   ├── SRNameAttribute.cs      # Кастомное имя/путь типа в меню
│   │   ├── SRHiddenAttribute.cs    # Скрыть тип из меню
│   │   ├── FormerlySerializedTypeAttribute.cs  # Старое имя типа для авто-замены
│   │   └── Services/
│   │       └── SRFormerlyTypeCache.cs          # Реестр замен [старый тип → новый Type]
│   └── Editor/                     # Редактор (asmdef SREditor-Editor)
│       ├── SRDrawer.cs             # PropertyDrawer для SRAttribute — рисует кнопку выбора
│       ├── SRDrawerOptions.cs      # Опции отрисовки (WithChild/ButtonTitle/DisableExpand)
│       ├── NameService.cs          # Форматирование имени типа по настройкам
│       ├── AssetChangeDetector.cs  # Источник событий об изменении ассетов
│       ├── SearchTree/             # Выпадающее меню выбора типа (GraphView SearchWindow)
│       ├── SRActions/              # Команды над managed-ссылкой (Create/Erase/Copy/Paste/Cut)
│       ├── Services/               # Кэш типов и TypeInfo
│       ├── Comparers/              # Сравнение/хэш TypeInfo для кэшей
│       ├── Settings/               # Project Settings + enum-ы режимов
│       ├── Processing/             # Авто-обработка ассетов (замена/очистка)
│       ├── Tools/                  # Пункты меню Tools/SREditor
│       └── MissingTypesValidator/  # [Deprecated] старый валидатор missing-типов
├── Samples/Demo/                   # Демо-сцена, данные, кастомный drawer
└── README.pdf
```

## Рантайм-слой (`Package/Scripts`)

- **`SRAttribute`** (`[SR]`) — `PropertyAttribute` на поле `[SerializeReference]`. Конструкторы: без типов (тип берётся из `managedReferenceFieldTypename`), один базовый тип, массив типов. Виртуальный `OnCreate(object)` — хук кастомизации создаваемого экземпляра (см. `SRDemoAttribute`).
- **`SRNameAttribute`** (`[SRName("Group/Sub/Name")]`) — задаёт путь типа в меню (`FullName`) и короткое имя (`Name`, часть после последнего `/`).
- **`SRHiddenAttribute`** (`[SRHidden]`) — исключает тип из меню и из результатов кэша типов.
- **`FormerlySerializedTypeAttribute`** (`[FormerlySerializedType("Asm, Ns.OldName")]`, `AllowMultiple`) — фиксирует старое полное имя типа. Только в `UNITY_EDITOR`; разбирает строку на класс/namespace/assembly, доинициализируется лениво через `SRFormerlyTypeCache`.
- **`SRFormerlyTypeCache`** — статический реестр, в статическом конструкторе сканирует все сборки и собирает словарь `[атрибут → Type]`. Отдаёт замены: `GetAllReplacements()`, `GetReplacementType(asm, type)`, `GetTypeForAttribute(attr)`.

## Editor-слой (`Package/Editor`)

### Отрисовка инспектора

- **`SRDrawer`** : `PropertyDrawer` для `SRAttribute` — ключевой класс UI.
  - `OnGUI` → `Draw` → `DrawCore`. Весь draw обёрнут в `try/catch(ObjectDisposedException)` — обход бага Unity 2022.3 с устаревшим `SerializedProperty` при удалении элемента списка.
  - Резолвит список доступных типов через `SRTypeCache`, рисует зелёную `DropdownButton` с именем текущего типа (+ индекс в массиве), по клику открывает меню выбора (`ShowTypeSelectionMenu`).
  - Публичные `Draw`/`GetPropertyHeight`/`GetButtonWidth` переиспользуются кастомными drawer-ами (пример — `CustomDataDrawer`).
- **`SRDrawerOptions`** — флаги `WithChild`, `ButtonTitle`, `DisableExpand`.
- **`NameService`** — форматирует `managedReferenceFullTypename` в подпись согласно `ShowNameType` (FullName/OnlyNameSpace/OnlyCurrentType), учитывает `SRNameAttribute`. `GetSplitPathType` режет путь по `NameSeparators` из настроек.

### Меню выбора типа (`SearchTree/`)

Использует `UnityEditor.Experimental.GraphView.SearchWindow`.

- **`SRTypesSearchWindowProvider`** : `ISearchWindowProvider` — строит дерево: фиксированные пункты `Erase / Copy / Paste / Cut` (через `SRActionFactory`) + дерево типов. `OnSelectEntry` вызывает `BaseSRAction.Apply()` у выбранного пункта.
- **`SRTypeTreeFactory`** — превращает массив `TypeInfo` в `List<SearchTreeEntry>`, группируя по путям (`SRName`/namespace). Сортирует через `TypeInfoComparer`.
- **`SRCashTypeSearchTree`** — кэш фабрик деревьев по хэшу набора типов (`TypeInfoArrayComparer`). Один статический экземпляр живёт в `SRDrawer`.

### Команды (`SRActions/`)

- **`BaseSRAction`** (файл `SRAction.cs`) — база: хранит `SerializedProperty`, `Apply()` делает `UpdateIfRequiredOrScript()` + абстрактный `DoApply()`.
- **`SRActionFactory`** — фабрика команд для текущего property и набора `TypeInfo`.
- Реализации:
  - **`InstanceClassSRAction`** — создаёт экземпляр выбранного типа (`Activator.CreateInstance`), пишет в `managedReferenceValue`, регистрирует Undo.
  - **`ErasePropertySRAction`** — обнуляет ссылку.
  - **`CopyPropertySRAction`** / **`CutPropertySRAction`** / **`PastePropertySRAction`** — буфер обмена через **`SRClipboard`** (статический `ManagedReferenceValue`). Cut/Paste/Copy регистрируют Undo.

### Кэш типов (`Services/`)

- **`SRTypeCache`** (static) — центральный кэш. Три словаря: имя→`Type`, базовый тип→массив наследников, `Type`→`TypeInfo`.
  - `GetSupportTypes(baseType)` — сканирует все сборки `AppDomain`, отбирает неабстрактные не-`SRHidden` наследники/реализации (`IsCorrectChildTypeForSearchTree`).
  - `GetTypeInfos(...)` — перегрузки по `Type` / `Type[]` / строке; учитывает `SRName.FullName` для пути, отсекает `SRHidden`.
- **`TypeInfo`** — пара `Type Type` + `string Path` (путь в меню). Лежит в namespace `SerializeReferenceEditor.Editor`.

### Сравнители (`Comparers/`)

- **`TypeInfoComparer`** : `IEqualityComparer<TypeInfo>` + `IComparer<TypeInfo>` — сортировка по сегментам пути (`/`), equals/hash по `Type`+`Path`.
- **`TypeInfoArrayComparer`** : `IEqualityComparer<TypeInfo[]>` — поэлементное сравнение и хэш массива (для ключа кэша деревьев).

### Настройки (`Settings/`)

- **`SREditorSettings`** : `ScriptableObject` — синглтон, сериализуется в `ProjectSettings/SREditorSettings.json` (не в Assets). Поля `internal` с публичными свойствами-геттерами:
  - Отображение: `ShowNameType`, `NameSeparators`.
  - Триггеры авто-обработки: `FormerlySerializedTypeOnAssetImport/OnAssetSelect/OnSceneSave`, `ProcessScenesOnOpen`.
  - Обработка: `ProcessingBatchSize`, `ProcessingFrameBudgetMs` (кадровый бюджет Apply), `ProcessingMaxThreads` (воркеры скана).
  - Очистка/дубликаты: `ClearMissingReferencesIfNoReplacement`, `DuplicateMode`.
  - Источники детектора изменений: `DoubleCleanOnEditorUpdate/OnUndoRedo/OnAssetSave`, `ChangeDetectorPollIntervalMs` (троттлинг опроса на update).
  - Инструменты: `MissingTypesAssetFilter`.
  - `[InitializeOnLoadMethod] Initialize()` пробрасывает флаги в `AssetChangeDetector.Initialize(...)`.
- **`SREditorSettingsProvider`** : `SettingsProvider` — UI на странице `Project/Serialize Reference Editor`, на изменение зовёт `SaveSettings()`.
- Enum-ы: **`ShowNameType`** (FullName/OnlyNameSpace/OnlyCurrentType), **`SRDuplicateMode`** (Default/Copy/Null).
- Пункт меню `Tools/SREditor/Settings` открывает страницу.

### Авто-обработка (`Processing/`)

- **`ProcessingCoordinator`** : `AssetPostprocessor` — оркестратор, машина состояний `Idle → Scanning → Applying` (`SRProcessingState`), которую крутит единый насос на `EditorApplication.update` (подписка только когда есть работа). `[InitializeOnLoadMethod]` подписывается на: выбор объекта, сохранение/открытие сцены, `AssetChangeDetector.ChangeEvent`, `AssemblyReloadEvents.beforeAssemblyReload`, `playModeStateChanged`. Триггеры (`OnPostprocessAllAssets`, `OnSelectionChanged`, `OnSceneOpened`, `OnAssetChanged`) кладут пути/объекты в очереди `PendingAssetPaths`/`PendingSceneObjects` и поднимают насос. Защита от рекурсии реимпорта — `InFlightImports`.
  - **Scanning** — `SnapshotPatterns` сворачивает замены в `SRReplacementPattern` на главном потоке, дальше `Task.Run` + `Parallel.ForEach` (`ProcessingMaxThreads`) гоняет `SRFileScanner.Scan` по путям: каждый файл читается один раз, все паттерны применяются в памяти (`TypeReplaceHelper.ApplyReplacement`). Воркеры не трогают Unity API.
  - **Applying** — `StepApply` по кадрам в пределах `ProcessingFrameBudgetMs`: пишет изменённые файлы, грузит ассеты для obj-веток (дубликаты/missing), обрабатывает объекты сцены, реимпортирует чанками (`ProcessingBatchSize`) под `StartAssetEditing`/`StopAssetEditing` без `ForceSynchronousImport`, в конце `SaveAssets`.
  - **Отмена** — `CancellationTokenSource`, гасится в `beforeAssemblyReload` и при выходе в play-mode; насос простаивает в play-mode.
  - `OnSceneSaving` остаётся синхронным (`SerializedObject`/`SerializationUtility`), но через `SRSceneDirtyTracker` обрабатывает только грязные корни (первый сейв сцены в сессии — полный проход).
  - **TDD рефакторинга в async-пайплайн**: `Docs/tdd/260626-1511-TDD-processing_coordinator_async_pipeline.md` (статус «Выполнено»).
- **`SRProcessingState`** (enum) — `Idle`/`Scanning`/`Applying`.
- **`SRSceneDirtyTracker`** (static) — трекинг грязных корней сцены: `MarkDirty`/`IsRootDirty`/`ShouldProcessAll`/`OnProcessed`/`Reset`. Источник пометок — `OnAssetChanged` (изменения выделенных объектов/undo).
- `TypeReplace/`:
  - **`TypeReplacer`** (static) — `TryClearMissingReferences(obj, out cleared)`: на загруженном `obj` чистит missing managed references (главный поток), если `ClearMissingReferencesIfNoReplacement`. Текстовая ветка вынесена в пайплайн.
  - **`TypeReplaceHelper`** (static) — `ApplyReplacement(content, pattern, out wasModified)`: чистая строковая трансформация (ни `File.*`, ни `AssetDatabase.*`), регексами заменяет тип в трёх местах (`references: RefIds`, `type: {class,ns,asm}`, `managedReferences[...]`) по разобранному `SRReplacementPattern`.
  - **`SRReplacementPattern`** (readonly struct) — иммутабельный снимок одного маппинга с уже разобранными частями типа (`OldClassName`/`OldNamespace`/`OldAssembly` + new-аналоги). Фабрика `Parse(oldTypePattern, newTypePattern)` разбирает строки один раз на главном потоке.
  - **`SRFileScanner`** (static) — потокобезопасный `Scan(path, patterns)`: читает файл один раз, прогоняет все паттерны, возвращает `SRFileScanResult`. Исключения трактуются как «не изменён».
  - **`SRFileScanResult`** (readonly struct) — `Path`/`Modified`/`NewContent` (контент только при `Modified`).
- `DoubleClean/`:
  - **`SRDuplicateCleaner`** (static) — `TryCleanupObject(asset, mode)`: обходит `SerializedObject` (для `GameObject` — по всем компонентам), ищет повторно встреченные managed-ссылки (один и тот же объект в нескольких полях) и по `SRDuplicateMode` зануляет / ставит default / делает deep copy (`CreateDeepCopy` рекурсивно копирует поля с `[SerializeReference]`).

### Детектор изменений

- **`AssetChangeDetector`** : `AssetModificationProcessor` — единый источник события `ChangeEvent(Object)`. Три источника (включаются из настроек через `Initialize`): `EditorApplication.update`, `Undo.postprocessModifications`, `OnWillSaveAssets`. Подписчик — `ProcessingCoordinator`. Опрос на `update` троттлится по `ChangeDetectorPollIntervalMs` и переиспользует кэш `SerializedObject` (`UpdateIfRequiredOrScript`), пересобираемый только на смену выделения (`Selection.selectionChanged`), а не каждый кадр.

### Инструменты меню (`Tools/`)

| Пункт меню | Класс | Действие |
|---|---|---|
| `Tools/SREditor/Log MissingTypes` | `MissingTypesLogger` | Логирует все missing managed-типы в проекте (фильтр `MissingTypesAssetFilter`) |
| `Tools/SREditor/Clean MissingTypes` | `MissingTypesCleaner` | Чистит missing-типы в prefab/asset/scene |
| `Tools/SREditor/Clean MissingTypes (Force Reserialize)` | `MissingTypesCleaner` | То же + `ForceReserializeAssets` |
| `Tools/SREditor/Class Replacer Window` | `TypeReplacerWindow` | Окно ручной замены типа во всех ассетах (выбор старого/нового типа, `TypeReplaceHelper`) |
| `Tools/SREditor/Settings` | `SREditorSettings` | Открывает Project Settings |

### `MissingTypesValidator/` — Deprecated

Полностью помечен `[Obsolete]`, заменён на `Tools/SREditor/Log MissingTypes`. Старая конфигурируемая схема: `SRMissingTypesValidatorConfig` (ScriptableObject с массивом `AssetChecker`) + загрузчики `IAssetsLoader` (`LoadAllScriptableObjects`) + форматтеры `IAssetMissingTypeReport` (`UnityLogAssetMissingTypeReport`). Использует `[SR]` на интерфейсных полях — заодно служит примером.

## Ключевые потоки данных

**Выбор типа в инспекторе:**
`SRDrawer.OnGUI` → `SRTypeCache.GetTypeInfos` (сканирование+кэш наследников) → `DropdownButton` → `ShowTypeSelectionMenu` → `SRCashTypeSearchTree.GetTypeTreeFactory` → `SRTypesSearchWindowProvider` (дерево + действия) → выбор пункта → `BaseSRAction.Apply` (`InstanceClassSRAction` создаёт экземпляр / `Erase` / clipboard-команды).

**Авто-замена переименованного типа:**
изменение/импорт ассета → `AssetChangeDetector.ChangeEvent` или `OnPostprocessAllAssets` → `ProcessingCoordinator` (очередь, насос на `update`) → Scanning (`SnapshotPatterns` → `SRReplacementPattern` → `Parallel.ForEach` `SRFileScanner.Scan` + `TypeReplaceHelper.ApplyReplacement` на воркер-нитях) → Applying (запись файлов и реимпорт чанками по кадровому бюджету).

**Очистка дубликатов/missing:**
сохранение сцены/ассета → `ProcessingCoordinator` → `SRDuplicateCleaner.TryCleanupObject` (по `SRDuplicateMode`) и/или `SerializationUtility.ClearAllManagedReferencesWithMissingTypes`.

## Демо (`Samples/Demo`)

- **`DataHolder`** : `MonoBehaviour` — три варианта поля с `[SerializeReference][SRDemo(...)]` (ограниченный список типов / все наследники / без аргументов).
- **`SRDemoAttribute`** : `SRAttribute` — переопределяет `OnCreate`, проставляя `DataName` для `AbstractNamedData`.
- Иерархия данных: `AbstractData` → `AbstractNamedData` → конкретные (`StringData`, `IntegerData`, `FloatData`, `ComplexData`, `ContainerData`, named-варианты, `HiddenData` с `[SRHidden]`, `StringInterfaceData` через `IData`). Пути в меню заданы `SRName`.
- **`CustomData`** + **`CustomDataDrawer`** — пример переиспользования `SRDrawer` внутри своего `PropertyDrawer` (рисует вложенное поле `Data` с кастомным заголовком).
- Прочее: `DataList`, `TestData`, `ScriptableObjectTest`/`ScriptableObjectTestData`, `PrefabTest` (+`.prefab`), `SOTest.asset`, сцена `SRDemo.unity`, `NewTests/` (отдельный ассембли для замены типов между сборками).

## Документация

- `Docs/PROJECT_MAP.md` — этот файл.
- `Docs/tdd/` — технические задания (текущее: async-пайплайн `ProcessingCoordinator`).
- `Docs/notes/` — заметки.
- `CHANGELOG.md`, `Assets/SREditor/README.pdf`.
