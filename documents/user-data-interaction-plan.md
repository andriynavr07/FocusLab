# План взаємодії користувача з даними FocusLab

## 1. Загальна схема потоку даних

```
Користувач
   │  (кліки, drag & drop, ввід)
   ▼
View (WPF, XAML)
   │  Binding / Commands
   ▼
ViewModel (MVVM)
   │  async виклики
   ▼
Services (TaskService, ResourceService, FocusService, AnalyticsService)
   │
   ├── EF Core (IDbContextFactory) ──► CRUD: задачі, проєкти, теги, ресурси, сесії
                     │
                     ▼
              SQLite (.db файл, локально)
```

**Принципи:**
- View не знає про БД: усе через ViewModel і сервіси.
- Записи та CRUD — через EF Core; агрегатні звіти — через ADO.NET (`ExecuteReaderAsync`).
- Кожна операція з БД — `async`/`await`, UI не блокується.
- Один короткоживучий `DbContext` на операцію (`IDbContextFactory<AppDbContext>`).
- Дати зберігаються в UTC.

## 2. Модель даних

| Таблиця | Поля | Примітки |
|---------|------|----------|
| `Projects` | `Id`, `Name`, `Color` | «Без проєкту» = `ProjectId IS NULL` у задачі |
| `Tasks` | `Id`, `Title` (1–255), `Description`, `Deadline?`, `Priority` (0–3), `Status` (0–3), `ProjectId?`, `CreatedAt`, `CompletedAt?` | Пріоритет за замовчуванням = Середній |
| `Tags` | `Id`, `Name`, `Color` | |
| `TaskTags` | `TaskId`, `TagId` | Зв'язок many-to-many |
| `TaskResources` | `Id`, `TaskId`, `Type` (File/Folder/Url), `Path`, `DisplayName` | Кількість необмежена |


**Enum-и:**
- `Priority`: Low, Medium, High, Critical
- `TaskStatus`: Todo, InProgress, Review, Done
- `ResourceType`: File, Folder, Url

**Індекси:** `Tasks(Status)`, `Tasks(ProjectId)`, `FocusSessions(StartDateTime)`, `FocusSessions(TaskId)`, `TaskResources(TaskId)`.

**Зв'язки:** `Project 1—N Task`, `Task 1—N TaskResource`, `Task 1—N FocusSession`, `Task N—M Tag`.

## 3. Матриця взаємодій «дія користувача → дані»

| Дія користувача | Що читаємо | Що пишемо | Технологія | Sync/Async |
|-----------------|-----------|-----------|-----------|-----------|
| Запуск застосунку | Наявність файлу БД, застосовані міграції | Створення БД, міграції | EF Core `Migrate()` | Async до показу вікна |
| Відкриття дошки | Задачі, проєкти, теги, ресурси | — | EF Core (`Include`) | Async |
| Створення задачі | Список проєктів | `Tasks` (Status = Todo) | EF Core | Async |
| Drag & Drop картки | — | `Tasks.Status`, `CompletedAt` | EF Core | Оптимістичне оновлення UI + async запис |
| Пошук / фільтр за тегами | Вже завантажені задачі | — | LINQ / `ICollectionView` у пам'яті | Sync, миттєво |
| Підсвічування простроченого | `Deadline`, `Status` | — | Обчислення у ViewModel | Sync |
| Додавання ресурсу | — | `TaskResources` | EF Core | Async |
| Відкриття ресурсу | `TaskResources.Path` | — (файлова система) | `File.Exists`/`Directory.Exists` + `Process.Start` | Sync-перевірка |
| Старт/пауза/скидання таймера | — | Нічого в БД до завершення | Стан у пам'яті | — |
| Завершення сесії | — | `FocusSessions` (`IsCompleted = true`) | EF Core | Async |


## 4. Детальні потоки

### 4.1. Створення задачі
1. View → команда `SaveTaskCommand`.
2. ViewModel валідує `Title` (1–255) → якщо помилка, показує повідомлення.
3. `TaskService.CreateAsync(dto)` → `Task { Status = Todo, Priority = Medium за замовчуванням }`.
4. EF Core `SaveChangesAsync()`.
5. Успіх → картка додається в колонку «До виконання». Помилка → повідомлення, форма зберігає введені дані.

### 4.2. Зміна статусу (Drag & Drop)
1. `Drop` → ViewModel отримує `taskId` та нову колонку.
2. UI оновлюється одразу (оптимістично).
3. `TaskService.ChangeStatusAsync(taskId, newStatus)`:
   - `Done` → `CompletedAt = UtcNow`;
   - з `Done` в інший статус → `CompletedAt = null`.
4. Помилка → відкат картки у попередню колонку + повідомлення.

### 4.3. Відкриття ресурсу
1. Клік → `OpenResourceCommand(resource)`.
2. `Url` → перевірка http/https → `Process.Start` з `UseShellExecute = true`.
3. `File`/`Folder` → `File.Exists` / `Directory.Exists`:
   - існує → `Process.Start` (`UseShellExecute = true`);
   - не існує → повідомлення «Файл за шляхом ... не знайдено».
4. Весь виклик у `try/catch`: будь-який виняток стає повідомленням, а не крахом.

### 4.4. Фокус-сесія
1. Старт: у пам'яті зберігаються `startTime` (UTC), `taskId`, стан (Running/Paused).
2. `DispatcherTimer` раз на секунду оновлює залишок як `duration − (UtcNow − startTime − pausedTotal)`.
3. При 00:00: звук + Windows-сповіщення.
4. `FocusService.SaveSessionAsync(taskId, startTime, 25, true)` → запис у `FocusSessions`.
5. Лічильник завершених сесій у поточному циклі: `< 4` → перерва 5 хв, `= 4` → довга перерва 15–20 хв і скидання лічильника.


**Запити (приклад):**

```sql
-- Загальний час і кількість завершених сесій
SELECT COALESCE(SUM(DurationMinutes), 0) AS TotalMinutes,
       COUNT(*)                          AS CompletedSessions
FROM FocusSessions
WHERE IsCompleted = 1
  AND StartDateTime >= @from AND StartDateTime < @to;

-- Розподіл часу за проєктами
SELECT COALESCE(p.Name, 'Без проєкту') AS ProjectName,
       SUM(fs.DurationMinutes)         AS Minutes
FROM FocusSessions fs
JOIN Tasks t          ON t.Id = fs.TaskId
LEFT JOIN Projects p  ON p.Id = t.ProjectId
WHERE fs.IsCompleted = 1
  AND fs.StartDateTime >= @from AND fs.StartDateTime < @to
GROUP BY p.Id, p.Name
ORDER BY Minutes DESC;

-- Кількість закритих задач за період
SELECT COUNT(*)
FROM Tasks
WHERE Status = 3
  AND CompletedAt >= @from AND CompletedAt < @to;
```

Для розподілу за тегами — додатковий `JOIN TaskTags`/`Tags`; врахувати, що задача з кількома тегами дублюється в різних групах.

## 5. Правила цілісності та помилок

| Ситуація | Поведінка |
|----------|-----------|
| Видалення проєкту | `Tasks.ProjectId` → `NULL` (`ON DELETE SET NULL`) |
| Видалення задачі | Каскадно видаляються ресурси й зв'язки з тегами; сесії краще залишити або каскадно видалити (узгодити в команді, впливає на аналітику) |
| Видалення тегу | Видаляються записи `TaskTags` |
| Помилка запису в БД | Повідомлення користувачу, стан UI узгоджується з БД |
| Відсутній ресурс на диску | Повідомлення, запис у БД не змінюється |
| Закриття застосунку під час сесії | Попередження; незавершена сесія не записується |
| Одночасний доступ EF Core + ADO.NET | Увімкнути режим WAL, короткі з'єднання, `Cache=Shared` не потрібен |

## 6. Розташування даних

- Файл БД: `%AppData%\FocusLab\focuslab.db` .
- Рядок підключення береться з одного місця й використовується і EF Core, і ADO.NET.
- Резервне копіювання: копіювання одного файлу `.db` (у backlog: кнопка «Експорт/резервна копія»).

## 7. Що перевірити після реалізації

- Дані збереглися після перезапуску (задачі, статуси, ресурси, сесії).
- Застосунок стартує без мережі, БД створюється при першому запуску.
