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
  - Обработка: `ProcessingBatchSize` (макс. файлов в чанке реимпорта), `ProcessingFrameBudgetMs` (кадровый бюджет Classify/Apply), `ProcessingMaxThreads` (воркеры скана), `ProcessingImportChunkKb` (макс. суммарный размер файлов в одном чанке реимпорта, дефолт 4096).
  - Очистка/дубликаты: `ClearMissingReferencesIfNoReplacement`, `DuplicateMode`.
  - Источники детектора изменений: `DoubleCleanOnEditorUpdate/OnUndoRedo/OnAssetSave`, `ChangeDetectorPollIntervalMs` (троттлинг опроса на update).
  - Инструменты: `MissingTypesAssetFilter`.
  - `[InitializeOnLoadMethod] Initialize()` пробрасывает флаги в `AssetChangeDetector.Initialize(...)`.
- **`SREditorSettingsProvider`** : `SettingsProvider` — UI на странице `Project/Serialize Reference Editor`, на изменение зовёт `SaveSettings()`.
- Enum-ы: **`ShowNameType`** (FullName/OnlyNameSpace/OnlyCurrentType), **`SRDuplicateMode`** (Default/Copy/Null/None). `Default` — активная стратегия (замена на default-значение), `None` — очистка дублей выключена. `None` добавлен в конец (int 3), чтобы не сломать сериализованные настройки.
- Пункт меню `Tools/SREditor/Settings` открывает страницу.

### Авто-обработка (`Processing/`)

- **`ProcessingCoordinator`** : `AssetPostprocessor` — оркестратор, машина состояний `Idle → Scanning → Classifying → Writing → Applying` (`SRProcessingState`), которую крутит единый насос на `EditorApplication.update` (подписка только когда есть работа). `[InitializeOnLoadMethod]` подписывается на: выбор объекта, сохранение/открытие сцены, `AssetChangeDetector.ChangeEvent`, `AssemblyReloadEvents.beforeAssemblyReload`, `playModeStateChanged`. Триггеры (`OnPostprocessAllAssets`, `OnSelectionChanged`, `OnSceneOpened`, `OnAssetChanged`) кладут пути/объекты в очереди `PendingAssetPaths`/`PendingSceneObjects` и поднимают насос. Защита от рекурсии реимпорта — `InFlightImports`.
  - **Ключевой принцип** — no-op проход стоит ~ноль на главном потоке: вся проверка «есть ли что делать» выполняется текстом на воркерах (вердикт), скан запускается **всегда** (даже при пустом списке паттернов), реальная работа режется на мелкие шаги под кадровый бюджет, запись файлов уходит в фон.
  - **Scanning** — `SnapshotPatterns` сворачивает замены в `SRReplacementPattern` на главном потоке, дальше `Task.Run` + `Parallel.ForEach` (`ProcessingMaxThreads`) гоняет `SRFileScanner.Scan` по путям: файл читается один раз, применяются все паттерны (`TypeReplaceHelper.ApplyReplacement`), по финальному контенту считается вердикт (`SRVerdictScanner`). Воркеры не трогают Unity API.
  - **Classifying** — `StepClassify` по кадрам разбирает вердикты: изменённые файлы → `PendingWrites` (и в `NeedsCleanAfterImport`, если вердикт требует чистки); неизменённые, но требующие чистки (`NeedsObjectClean`: дубли rid, нераспарсенные/missing-триплеты через `SRTypeResolveCache`) → `PendingObjBranch`; остальные — отброс. Только словарные лукапы в бюджете, никаких `LoadAssetAtPath`.
  - **Writing** — если есть записи, `StartWrites` на воркере пишет через временный `.srtmp` + `File.Replace`/`Move`: полузаписанных ассетов не остаётся ни при отмене, ни при domain reload. Результаты (путь + размер) стекаются в `SRImportItem`.
  - **Applying** — `StepApply` по кадрам в пределах `ProcessingFrameBudgetMs`: `LoadAssetAtPath` для obj-веток (один за проход), развёртка в `SRCleanStep`/`SRCleanGroup` (по компоненту за шаг, dup-шаги делят один `seenObjects` на GameObject), выполнение шагов, реимпорт чанками по суммарным байтам (`ProcessingImportChunkKb`) и числу (`ProcessingBatchSize`) под `StartAssetEditing`/`StopAssetEditing`, в конце — поштучный `SaveAssetIfDirty` (`DirtyAssets`). `Idle` с одними `PendingSceneObjects` переходит сразу в Applying.
  - **Отмена** — `CancellationTokenSource` (`CancelProcessing` гасит и скан, и запись), срабатывает в `beforeAssemblyReload` и при выходе в play-mode; насос простаивает в play-mode.
  - `OnSceneSaving` остаётся синхронным (`SerializedObject`/`SerializationUtility`), но через `SRSceneDirtyTracker` обрабатывает только точечно изменённые объекты, сгруппированные по GameObject с общим `seenObjects` (первый сейв сцены в сессии — полный проход).
  - **TDD**: `Docs/tdd/260626-1511-TDD-processing_coordinator_async_pipeline.md` (async-пайплайн) и `Docs/tdd/260711-1734-TDD-processing_invisible_pipeline.md` (незаметный пайплайн: префильтр вердиктов, фоновая запись, слайсинг клинеров) — оба «Выполнено».
- **`SRProcessingState`** (enum) — `Idle`/`Scanning`/`Classifying`/`Writing`/`Applying`.
- **`SRSceneDirtyTracker`** (static) — точечный трекинг грязных объектов сцены (instanceID компонента/GameObject, не корней): `MarkDirty(Object)`/`GetDirtyObjectIds`/`ShouldProcessAll`/`OnProcessed`/`Reset`. Источник пометок — `OnAssetChanged` (изменения выделенных объектов/undo).
- **`SRCleanStep`/`SRCleanGroup`/`SRCleanStepKind`** — единица отложенной очистки: `SRCleanStep` (цель + вид `Duplicate`/`Missing` + shared `seenObjects` + группа), `SRCleanGroup` (корень/сцена, счётчик `Remaining`, флаг `AnyChanged`; при завершении с изменениями дертит ассет или сцену). `SRImportItem` (readonly struct) — путь + размер файла для чанкования реимпорта.
- `TypeReplace/`:
  - **`TypeReplacer`** (static) — `TryClearMissingReferences(obj, out cleared)` (чистит missing managed references на `GameObject`/`ScriptableObject`, гейт `ClearMissingReferencesIfNoReplacement`) и `ClearMissingOn(target)` (прямая очистка одного объекта без проверки настроек — гейт на вызывающей стороне).
  - **`TypeReplaceHelper`** (static) — `ApplyReplacement(content, pattern, out wasModified)`: чистая строковая трансформация (ни `File.*`, ни `AssetDatabase.*`), регексами заменяет тип в трёх местах (`references: RefIds`, `type: {class,ns,asm}`, `managedReferences[...]`) по разобранному `SRReplacementPattern`.
  - **`SRReplacementPattern`** (readonly struct) — иммутабельный снимок одного маппинга с уже разобранными частями типа (`OldClassName`/`OldNamespace`/`OldAssembly` + new-аналоги). Фабрика `Parse(oldTypePattern, newTypePattern)` разбирает строки один раз на главном потоке.
  - **`SRFileScanner`** (static) — потокобезопасный `Scan(path, patterns)`: читает файл один раз, прогоняет все паттерны, считает вердикт по финальному контенту, возвращает `SRFileScanResult`. Исключения трактуются как «не изменён».
  - **`SRFileScanResult`** (readonly struct) — `Path`/`Modified`/`NewContent` (контент только при `Modified`) + `Verdict`.
  - **`SRVerdictScanner`** (static) — потокобезопасный однопроходный построчный анализ YAML без Unity API (`AsSpan`/`Slice`, без аллокаций на строку): считает `SRFileVerdict` (`HasManagedReferences`/`HasDuplicateRids`/`HasUnparsableTypes` + список `SRTypeTriple`). Счётчики использований rid — на документ; нераспарсенный триплет/generic-имя — консервативный кандидат.
  - **`SRFileVerdict`** (readonly struct), **`SRTypeTriple`** (readonly struct, ключ кэша: класс/ns/asm, нормализованы к пустой строке).
  - **`SRTypeResolveCache`** (static, **только главный поток**) — `IsMissing(triple)`: резолвит тип через `AppDomain` и кэширует результат. Статика сбрасывается при domain reload (новые типы требуют рекомпиляции → кэш инвалидируется автоматически).
- `DoubleClean/`:
  - **`SRDuplicateCleaner`** (static) — `TryCleanupObject(asset, mode)` (для `GameObject` — по всем компонентам с общим `seenObjects`) и `TryCleanupTarget(target, mode, seenObjects)` (один объект с внешним shared-набором). При `SRDuplicateMode.None` — ранний выход. По `SRDuplicateMode` повторно встреченную managed-ссылку зануляет / ставит default / делает deep copy (`CreateDeepCopy` рекурсивно копирует поля с `[SerializeReference]`).

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
изменение/импорт ассета → `AssetChangeDetector.ChangeEvent` или `OnPostprocessAllAssets` → `ProcessingCoordinator` (очередь, насос на `update`) → Scanning (`SnapshotPatterns` → `SRReplacementPattern` → `Parallel.ForEach` `SRFileScanner.Scan` + `TypeReplaceHelper.ApplyReplacement` + `SRVerdictScanner` на воркер-нитях) → Classifying (вердикты → `PendingWrites`/`PendingObjBranch`/отброс) → Writing (фоновая запись через temp+`Replace`) → Applying (реимпорт чанками по байтам, затем поштучный `SaveAssetIfDirty` по кадровому бюджету).

**Очистка дубликатов/missing:**
- сейв сцены → `ProcessingCoordinator.OnSceneSaving` → полный проход (первый сейв в сессии) либо точечно по `SRSceneDirtyTracker.GetDirtyObjectIds` → `SRDuplicateCleaner.TryCleanupTarget` / `TypeReplacer.ClearMissingOn`.
- сейв/импорт ассета → пайплайн: вердикт на воркере → obj-ветка в Applying → развёртка в `SRCleanStep` → `SRDuplicateCleaner.TryCleanupTarget` (по `SRDuplicateMode`) и/или `TypeReplacer.ClearMissingOn` → `SaveAssetIfDirty`.

## Демо (`Samples/Demo`)

- **`DataHolder`** : `MonoBehaviour` — три варианта поля с `[SerializeReference][SRDemo(...)]` (ограниченный список типов / все наследники / без аргументов).
- **`SRDemoAttribute`** : `SRAttribute` — переопределяет `OnCreate`, проставляя `DataName` для `AbstractNamedData`.
- Иерархия данных: `AbstractData` → `AbstractNamedData` → конкретные (`StringData`, `IntegerData`, `FloatData`, `ComplexData`, `ContainerData`, named-варианты, `HiddenData` с `[SRHidden]`, `StringInterfaceData` через `IData`). Пути в меню заданы `SRName`.
- **`CustomData`** + **`CustomDataDrawer`** — пример переиспользования `SRDrawer` внутри своего `PropertyDrawer` (рисует вложенное поле `Data` с кастомным заголовком).
- Прочее: `DataList`, `TestData`, `ScriptableObjectTest`/`ScriptableObjectTestData`, `PrefabTest` (+`.prefab`), `SOTest.asset`, сцена `SRDemo.unity`, `NewTests/` (отдельный ассембли для замены типов между сборками).

## Документация

- `Docs/PROJECT_MAP.md` — этот файл.
- `Docs/tdd/` — технические задания (текущее: незаметный пайплайн `ProcessingCoordinator` — префильтр вердиктов, фоновая запись, слайсинг клинеров).
- `Docs/notes/` — заметки.
- `CHANGELOG.md`, `Assets/SREditor/README.pdf`.
