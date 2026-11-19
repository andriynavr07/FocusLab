# FocusLab — план тестування по спринтах

19 лист. 2026 р. · @Someone

## Звідки план і як рахувались спринти

Автотести потрібні лише спринтам 3 і 4: зараз у репозиторії 29 тестів, план додає ще 100. Для кожної задачі з дошки FocusLab GoalRoad нижче вказано тестовий файл і назви тест-методів.

На дошці 56 задач. Поля Sprint/Iteration там немає, milestones порожні. Тому спринти визначено за датами створення задач і етапами з документа «Вимоги та правила командного проєкту».

| Спринт | Дати | Задачі | Етап |
| --- | --- | --- | --- |
| 1 | 11.09–24.09 | #5–#21 | 1–3: команда, вимоги, моделювання |
| 2 | 25.09–02.10 | #29–#35 | 4: архітектура та дані |
| 3 | 15.10–29.10 | #47–#66 | 5: початкова структура |
| 4 | 03.11–19.11 | #72–#90 | 6: ітеративна реалізація |

Зараз усі тести лежать у `Backend/tests/FocusLab.Application.Tests` (xUnit, NSubstitute, FluentAssertions). План додає ще два тестові проєкти:

- **FocusLab.Infrastructure.Tests** (net8.0): репозиторії, CatalogService, міграції, хешування пароля. Використовує SQLite in-memory через Microsoft.Data.Sqlite.
- **FocusLab.Frontend.Tests** (net8.0-windows): ViewModel-і та TaskBoardStore.

Позначка ✅ означає, що тест уже є в репозиторії.

## Спринти 1–2: автотести не потрібні

У задачах #5–#35 немає коду: це README, презентації, вимоги, user stories, UML, дизайн і рішення щодо архітектури. Їх перевіряють через рев'ю PR іншим учасником команди.

## Спринт 3 (15.10–29.10): 4 тести вже є, 28 нових

| Задача | Файл | Тести | К-сть |
| --- | --- | --- | --- |
| #54 Solution і проєкти, #55 Посилання між проєктами | Infrastructure.Tests/ArchitectureTests.cs | Domain_DoesNotReferenceApplicationOrInfrastructure, Application_DoesNotReferenceInfrastructure (через Assembly.GetReferencedAssemblies) | 2 |
| #56 Сутності та enum-и в Domain | Application.Tests/DomainDefaultsTests.cs | TaskItem_New_HasTodoMediumAndEmptyChecklist, [Theory] Priority_PersistedValues_AreStable, [Theory] TaskStatus_PersistedValues_AreStable (числа enum-ів лежать у БД) | 3 |
| #57 AppDbContext і конфігурації | Infrastructure.Tests/UtcDateTimeConverterTests.cs, AppDbContextTests.cs | LocalTime_IsStoredAsUtc, ReadValue_HasUtcKind, Nullable_NullStaysNull, DeleteTask_CascadesTaskTagsAndFocusSessions | 4 |
| #58 Міграція InitialCreate | Infrastructure.Tests/DatabaseInitializerTests.cs | ✅ DatabaseMigrationsAndTaskCatalog_PersistProjectsTagsAndChecklist; нові: InitializeAsync_EmptyDb_NoPendingMigrations, InitializeAsync_CalledTwice_DoesNotFail, InitializeAsync_FileDb_SetsWalJournalMode | 1 + 3 |
| #59 IClock і SystemClock | Infrastructure.Tests/SystemClockTests.cs | UtcNow_HasUtcKind (FakeClock уже є) | 1 |
| #60 TaskService (CRUD), #61 тести для TaskService | Application.Tests/TaskServiceTests.cs | ✅ 3 наявні; нові: CreateAsync_IgnoresDtoStatus_SetsTodoAndClockTime, CreateAsync_TrimsTitle, CreateAsync_TitleOver255_Throws, CreateAsync_ProjectOver100_Throws, [Theory] CreateAsync_NonPositiveEstimate_StoredAsNull (0, -5), CreateAsync_DuplicateTagIds_AreDistinct, UpdateAsync_UnknownId_ThrowsKeyNotFound, UpdateAsync_ToDone_SetsCompletedAt, UpdateAsync_DoneToTodo_ClearsCompletedAt, UpdateAsync_SameStatus_KeepsCompletedAt, GetAll_ChecklistJsonNull_ThrowsInvalidOperation | 3 + 11 |
| #65 Розібрати MVVM | Frontend.Tests/RelayCommandTests.cs, ViewModelBaseTests.cs | Execute_InvokesAction, CanExecute_UsesPredicate, SetField_SameValue_NoPropertyChanged, SetField_NewValue_RaisesOnce | 4 |
| #66 Оновити модель даних | — | Покривається тестами для #58 | — |
| #47–#52, #63, #64 (верстка, стилі, Figma, UML) | — | Автотестів не треба: ручна звірка з Figma і рев'ю | — |

## Спринт 4 (03.11–19.11): 25 тестів вже є, 72 нові

| Задача | Файл | Тести | К-сть |
| --- | --- | --- | --- |
| #72 Модель даних і міграції | Infrastructure.Tests/MigrationsTests.cs | Migrate_FromAddProjects_ExistingTasksGetEmptyChecklist (накатити міграції до AddProjects, вставити задачу, накатити до кінця) | 1 |
| #73 Сервіси: AuthService | Application.Tests/AuthServiceTests.cs | Register_Valid_HashesPasswordAndSignsIn, [Theory] Register_InvalidName_Fails ("", "   ", 101 символ), [Theory] Register_InvalidEmail_Fails, Register_PasswordUnder8_Fails, Register_AlreadyRegistered_Fails, Register_NormalizesEmail, Login_Success_SignsIn, Login_WrongPassword_Fails, Login_UnknownEmail_SameMessageAsWrongPassword, UpdateProfile_EmailTakenByOther_Fails, UpdateProfile_NotSignedIn_Fails, IsRegistered_NoPasswordHash_False | 12 |
| #73 Pbkdf2PasswordHasher | Infrastructure.Tests/Pbkdf2PasswordHasherTests.cs | HashThenVerify_True, Verify_WrongPassword_False, Hash_SamePassword_DifferentSalt, [Theory] Verify_MalformedHash_ReturnsFalseNotThrows | 4 |
| #73 CatalogService | Infrastructure.Tests/CatalogServiceTests.cs | SaveProject_DuplicateName_Throws, SaveProject_Rename_UpdatesTasksProject, DeleteProject_ClearsTasksProject, [Theory] SaveProject_InvalidColor_Throws ("red", "#12345", "#GGGGGG"), SaveProject_LowercaseColor_StoredUppercase, SaveTag_DuplicateName_Throws, DeleteTag_RemovesTaskTags | 7 |
| #73 ActivityService | Application.Tests/ActivityServiceTests.cs | Summary_SumsOnlyTodaySessions, InterruptedSession_ShownToday_ButNotInStreak, DoneTaskWithoutCompletedAt_Ignored, Summary_UsesLocalDayBoundary (часова зона Києва, сесія о 22:30 UTC) | 4 |
| #73 JsonFocusSettingsProvider | Infrastructure.Tests/JsonFocusSettingsProviderTests.cs | MissingFile_ReturnsDefaults, CorruptedJson_ReturnsDefaults, Save_KeepsOtherSections, Save_ClampsOutOfRange (500 → 180, 1 → 2), Save_ThenNewInstance_LoadsSaved | 5 |
| #73 Репозиторії | Infrastructure.Tests/RepositoryTests.cs | SetTags_UnknownTag_Throws, SetTags_ReplacesExisting, Update_UnknownId_Throws, Account_DuplicateEmail_ViolatesUniqueIndex | 4 |
| #74 Екран входу | Frontend.Tests/AuthViewModelTests.cs | Register_PasswordsMismatch_DoesNotCallAuth, Register_Success_RaisesRegistered, Register_AuthFail_ShowsError, Login_Success_InvokesCallback, Login_Fail_ShowsError | 5 |
| #75 Каркас і навігація | Frontend.Tests/MainViewModelTests.cs | Start_Registered_ShowsLogin, Start_NotRegistered_ShowsRegister, Logout_ReturnsToLogin | 3 |
| #76 Канбан, #77 Робота із задачами, #79 Екран додавання, #86 Фікс появи на дошці | Frontend.Tests/TaskBoardStoreTests.cs | Load_DoesNotTriggerUpdate, Add_CallsCreate_AndSetsId, Add_NonTodoStatus_CallsUpdateAfterCreate (регресійний тест для #86), EditTitle_CallsUpdateOnce, EditBeforeCreateFinished_UpdateRunsAfterCreate, Mapping_NullDeadline_DueDateIsCreatedAt, [Theory] Mapping_Priority_RoundTrip | 7 |
| те саме | Frontend.Tests/TaskRowTests.cs, TasksViewModelTests.cs | Status_Done_SetsCompletedAt, Status_BackToTodo_ClearsCompletedAt, IsOverdue_DoneTask_False, CanAddSubtask_EmptyOrOver255_False, Filter_ByStatus_ShowsOnlyMatching, AllDone_EmptyList_False | 6 |
| #78 Екран фокус-сесій, #80 Логіка таймера | Application.Tests/FocusTimerServiceTests.cs | ✅ 16 у FocusTimerServiceTests + ✅ 6 у StreakCalculatorTests + ✅ 3 у FocusServiceTests; нові: Resume_AfterPause_ContinuesFromRemaining, SkipBreak_DuringBreak_SwitchesToFocus | 25 + 2 |
| #81 Логіка на ресурси | Frontend.Tests/TaskAttachmentStoreTests.cs | GetFor_ReturnsOnlyThisTask, RemoveForTask_RemovesAll, CorruptedFile_ReturnsEmpty, Save_ThenNewInstance_Persists | 4 |
| #88 Деталі завдання | Frontend.Tests/TasksViewModelTests.cs | OpenEdit_FillsFormFromRow, SubmitEdit_UpdatesRow_NotAddsNew, FormTitle_InEditMode_IsEdit | 3 |
| #90 Логіка календаря | Frontend.Tests/CalendarViewModelTests.cs | Days_StartOnMonday_CountMultipleOf7, DonePercent_CountsOnlyCurrentMonth, DonePercent_NoTasks_Zero, Search_FiltersSelectedEvents, FifteenTasksOneDay_TimeRangeDoesNotWrap | 5 |
| #87 Темна тема | — | Ручна перевірка | — |

## Знайдений баг і потрібний рефакторинг

**Баг** (його ловить тест для #90). У `CalendarViewModel.CreateEvent` година старту рахується як `9 + index`. Якщо на один день припадає 15 задач, година стає 24, і `TimeSpan` з форматом `hh` показує «00:00 - 00:25».

**Рефакторинг, без якого frontend-тести не запустяться.** Ці зміни також можна показати як «приклади виконаного рефакторингу» для етапу 7:

1. `TaskBoardStore` викликає `MessageBox.Show` у `Remove` і `ShowError`. Рішення: винести це в інтерфейс `IDialogService`.
2. `TaskAttachmentStore` є синглтоном із жорстко заданим шляхом `AppPaths.Root`. Рішення: передавати шлях до файлу через конструктор.
3. `TaskRow` і `CalendarViewModel` беруть час напряму з `DateTime.Now` і `DateTime.Today`. Рішення: передавати `IClock`, як це вже зроблено в backend.
4. `UiPreferences.Instance` є глобальним станом, тому тести на текст вимагають фіксованої мови. Класи з такими тестами мають позначатися однією `[Collection]`, щоб не виконуватися паралельно.

Ще одне зауваження: в репозиторій закомічено папки `obj/`. Їх треба додати в `.gitignore`, інакше CI на етапі 8 може збирати проєкт із застарілим кешем NuGet.
