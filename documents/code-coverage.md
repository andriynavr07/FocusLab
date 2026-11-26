# Покриття коду тестами

Збір покриття під час `dotnet test` і публікація звіту в CI (задача #101).

## Що налаштовано

| Що | Де |
|---|---|
| `coverlet.collector` 6.0.2 | `FocusLab.Application.Tests`, `FocusLab.Infrastructure.Tests`, `FocusLab.Frontend.Tests` |
| Налаштування збору (формат Cobertura, винятки) | `coverlet.runsettings` у корені |
| Збір + об'єднання звітів + summary + артефакт | `.github/workflows/ci.yml`, кроки `Test`, `Generate coverage report`, `Coverage summary`, `Upload coverage report` |

Що не рахується: тестові збірки, згенерований код (`GeneratedCode`, `CompilerGenerated`, `[ExcludeFromCodeCoverage]`), міграції EF Core, автовластивості.

## Де дивитися результат у CI

1. Actions → потрібний запуск → **Summary**: відсоток рядків для `FocusLab.Application`, загальний відсоток і таблиця по збірках.
2. Внизу сторінки запуску в **Artifacts**: `coverage-report` (HTML — відкрий `index.html`, Cobertura — `Cobertura.xml`) та `test-results` (trx).

## Локально

```powershell
dotnet test Backend/FocusLab.slnx -c Release --collect:"XPlat Code Coverage" -s coverlet.runsettings --results-directory TestResults
dotnet tool install --global dotnet-reportgenerator-globaltool --version 5.4.1   # один раз
reportgenerator "-reports:TestResults/**/coverage.cobertura.xml" "-targetdir:CoverageReport" "-reporttypes:Html" "-assemblyfilters:+FocusLab.*;+frontend"
start CoverageReport/index.html
```

`TestResults/` і `CoverageReport/` в `.gitignore`, у репозиторій вони не потрапляють.

## Мінімальний поріг

Поріг **необов'язковий і зараз вимкнений**, щоб не ламати CI на старті. Він задається змінною `COVERAGE_MIN_APPLICATION` у кроці `Coverage summary` (`ci.yml`): `0` означає «не перевіряти». Коли побачите стабільний відсоток, виставте поріг трохи нижче поточного значення (наприклад, поточні 62 % → поріг 60), і CI падатиме лише при помітному погіршенні. Піднімайте поріг поступово.

## Відомі обмеження

* Покриття `FocusLab.Application` рахується по рядках (`line-rate`) з об'єднаного звіту всіх тестових проєктів: його покривають і `Application.Tests`, і `Frontend.Tests` (через транзитивну залежність).
* Для WPF-коду (`frontend`) відсоток нижчий, бо XAML і code-behind тестами не покриваються. Це нормально, ціль — логіка у ViewModel і сервісах.
