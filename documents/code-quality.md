# Перевірки якості коду

Автоматичні перевірки стилю й аналізу коду, які виконуються локально та в CI (задача #100).

## Що налаштовано

| Що | Де | Для чого |
|---|---|---|
| `.editorconfig` | корінь репозиторію | Базові правила стилю C#: відступи, фігурні дужки, порядок `using`, іменування. Їх підхоплюють Visual Studio, Rider, VS Code і `dotnet format`. |
| `Directory.Build.props` | корінь репозиторію | Вмикає .NET-аналізатори (`EnableNETAnalyzers`, `AnalysisLevel=latest`, `AnalysisMode=Default`) і IDE-правила під час збирання (`EnforceCodeStyleInBuild`) для **всіх** проєктів. |
| Крок `Check formatting (dotnet format)` | `.github/workflows/ci.yml` | Падає, якщо `dotnet format` змінив би хоч один файл. Виконується до збирання, тож помилка форматування видна одразу. |
| Крок `Build` | `.github/workflows/ci.yml` | Попередження аналізаторів (`CA*`, `IDE*`) виводяться в лог, наприкінці — підсумок `Warning(s)`. |

Усе це відбувається в job `build`, тому required status check у Branch protection (#97) лишається тим самим.

## Як це працює для розробника

Перед пушем перевір і за потреби виправ форматування:

```powershell
# з кореня репозиторію — перевірка (нічого не змінює)
dotnet format Backend/src/Application/FocusLab.Application.csproj --verify-no-changes
# виправлення
dotnet format Backend/src/Application/FocusLab.Application.csproj
```

Проєкти, які перевіряє CI, перелічені в `Backend/FocusLab.slnx`:
`FocusLab.Application`, `FocusLab.Domain`, `FocusLab.Infrastructure`, три тестові проєкти та `frontend`.
Щоб виправити все одразу, виконай `dotnet format <проєкт>` для кожного з них.

Visual Studio застосовує ті самі правила автоматично: **Edit → Advanced → Format Document** (`Ctrl+K, Ctrl+D`).

### Рівні правил

* `warning` (зараз це **IDE0055** — форматування) — порушення ламає CI;
* `suggestion` — підказка в IDE (стиль `using`, іменування, `file_scoped` namespace), CI не падає.

Нові правила вмикай із `suggestion`, а на `warning` переводь лише після того, як існуючий код їм відповідає.

## Рішення: TreatWarningsAsErrors

**Поки що `TreatWarningsAsErrors` вимкнено** (`false` у `Directory.Build.props`).

Чому:

1. Аналізатори вмикаються вперше, тож у проєктах уже є попередження, які ніхто не бачив. Якщо вважати їх помилками, збірка одразу стане червоною для всіх відкритих гілок.
2. Деякі попередження приходять із згенерованого коду (WPF/XAML, міграції EF Core), який ми не редагуємо.
3. Форматування, яке найчастіше ламається, і так уже блокує CI окремим кроком `dotnet format`.

План переходу:

1. Подивитись підсумок `Warning(s)` у логу кроку `Build` в останньому запуску CI.
2. Виправити попередження в коді, а для згенерованого коду або свідомо прийнятих винятків виставити `dotnet_diagnostic.<ID>.severity = none` в `.editorconfig` із коментарем чому.
3. Коли підсумок стане `0 Warning(s)`, змінити в `Directory.Build.props` значення на `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` окремим PR.

До цього моменту нові попередження не вважаються приводом блокувати PR, але ревʼюер звертає увагу на ті, що з'явилися через конкретні зміни.
