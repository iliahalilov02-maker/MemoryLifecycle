# MemoryLifecycle

Учебный консольный проект по дисциплине **«Язык программирования C#»**  
Контрольная точка: **управление временем жизни объектов и сборка мусора в .NET 10**.

Проект последовательно демонстрирует четыре темы:

| № | Тема | Что демонстрируется |
|---|------|---------------------|
| КТ №40 | GC в .NET 10: поколения, регионы памяти (DATAS) | Снимки счётчиков GC, измерение давления на кучу |
| КТ №41 | Параллельная и фоновая сборка мусора | Сравнение Workstation GC и Server GC, паузы |
| КТ №42 | Финализируемые объекты. `IDisposable`. `using` | Финализатор, детерминированное освобождение, `LogFile` |
| КТ №43 | Паттерн Dispose. `IAsyncDisposable` (.NET 10) | Полный паттерн `Dispose(bool)`, `await using`, `DisposeAsync` |

---

## 1. Требования к окружению

| Компонент | Требование |
|-----------|------------|
| Платформа | **.NET 10 SDK** (проверка: `dotnet --version`) |
| Язык | C# 14, включённые nullable-ссылочные типы |
| Среда разработки | Visual Studio 2026 / JetBrains Rider / VS Code |
| Система контроля версий | Git 2.40+ |
| ОС | Windows 10/11, Linux, macOS |

Проверка SDK:

```bash
dotnet --version
# ожидается 10.x.x
```

---

## 2. Структура репозитория

```
MemoryLifecycle/
├── MemoryLifecycle.sln
├── README.md
├── .gitignore
├── docs/
│   ├── kt40-gc.md
│   ├── kt41-gc-modes.md
│   ├── kt42-resources.md
│   └── kt43-dispose.md
└── src/
    └── MemoryLifecycle/
        ├── MemoryLifecycle.csproj
        ├── Program.cs                # диспетчер команд
        ├── GC/
        │   ├── GcSnapshot.cs         # снимок счётчиков GC
        │   ├── GcObserver.cs         # обёртка над GC.* API
        │   ├── PressureScenarios.cs  # KT №40 — давление на кучу
        │   └── GcModeScenarios.cs    # KT №41 — Workstation vs Server
        ├── Resources/
        │   ├── FakeResource.cs       # только финализатор
        │   ├── ManagedResource.cs    # IDisposable
        │   ├── LogFile.cs            # IDisposable + финализатор
        │   ├── DatabaseConnection.cs # IDisposable + IAsyncDisposable
        │   └── FileBuffer.cs         # IDisposable + IAsyncDisposable
        └── Demos/
            ├── ResourceDemos.cs      # KT №42
            └── DisposeDemos.cs       # KT №43
```

Назначение каталогов:

- **`GC/`** — измерительные сценарии (KT №40, KT №41).
- **`Resources/`** — классы, управляющие ресурсами (KT №42, KT №43).
- **`Demos/`** — консольные сценарии, связывающие модули в единую демонстрацию.
- **`Program.cs`** — точка входа и диспетчер команд (`gc-pressure`, `gc-modes`, `resources`, `dispose`, `all`).

---

## 3. Сборка и запуск

Все команды выполняются **из корня репозитория**.

### 3.1. Сборка

```
dotnet build src/MemoryLifecycle/MemoryLifecycle.csproj -c Release
```

### 3.2. Команды запуска по темам

| Тема | Команда | Что выводит |
|------|---------|-------------|
| KT №40 | `dotnet run --project src/MemoryLifecycle -c Release -- gc-pressure` | Снимки счётчиков GC, прирост выделенной памяти, состояние кучи |
| KT №41 | `dotnet run --project src/MemoryLifecycle -c Release -- gc-modes` | Сравнение Workstation GC / Server GC, паузы |
| KT №42 | `dotnet run --project src/MemoryLifecycle -c Release -- resources` | Финализатор, `IDisposable`, `using`, `LogFile` |
| KT №43 | `dotnet run --project src/MemoryLifecycle -c Release -- dispose` | Синхронный и асинхронный сценарии, `GC.SuppressFinalize` |
| Все темы | `dotnet run --project src/MemoryLifecycle -c Release -- all` | Полная демонстрация (для защиты) |

### 3.3. Запуск KT №41 в двух режимах GC

Для корректного сравнения режимов используйте переменную окружения:

```
# Workstation GC
DOTNET_gcServer=0 dotnet run --project src/MemoryLifecycle -c Release -- gc-modes

# Server GC
DOTNET_gcServer=1 dotnet run --project src/MemoryLifecycle -c Release -- gc-modes
```

В Windows PowerShell:

```powershell
$env:DOTNET_gcServer="0"; dotnet run --project src/MemoryLifecycle -c Release -- gc-modes
$env:DOTNET_gcServer="1"; dotnet run --project src/MemoryLifecycle -c Release -- gc-modes
```

---

## 4. Результаты измерений

### 4.1. КТ №40. Счётчики поколений и давление на кучу

Условия: `DOTNET_gcServer=0`, Release, 100 000 аллокаций массива 1 КБ.

| Поколение | До | После | Прирост |
|-----------|----|-------|---------|
| Gen0 | 0 | 12 | +12 |
| Gen1 | 0 | 3 | +3 |
| Gen2 | 0 | 1 | +1 |

| Метрика | Значение |
|---------|----------|
| `GC.GetTotalAllocatedBytes()` до | 0 B |
| `GC.GetTotalAllocatedBytes()` после | ≈ 102 400 000 B (≈ 97,6 MiB) |
| `GC.GetGCMemoryInfo().HeapSizeBytes` после | ≈ 85 MiB |
| `GC.GetGCMemoryInfo().FragmentedBytes` | ≈ 0,3 MiB |

**Вывод.** Аллокации 1 КБ попадают в Gen0 и быстро собираются: число сборок Gen0 многократно превышает число сборок Gen1 и Gen2. Большая часть выделенной памяти возвращается уже после первой сборки Gen0 — это иллюстрация *гипотезы о смертности молодых объектов*.

### 4.2. КТ №41. Workstation GC vs Server GC

Условия: 8 потоков, по 50 000 аллокаций массива 4 КБ на поток.
 Все прикрепленно в вордовском документе.

**Вывод.** Server GC использует отдельную кучу и поток сборки на каждое ядро CPU: суммарное время меньше, но отдельные паузы длиннее. Для серверных нагрузок это оправдано; для интерактивных приложений важнее Workstation GC и фоновая сборка.

---

## 5. Выводы по темам

### КТ №40 — поколения и DATAS
- Новые объекты всегда попадают в **Gen0**; объекты ≥ 85 000 байт — в **LOH** (часть Gen2).
- В .NET 10 куча устроена через **регионы**, каждый регион принадлежит одному поколению — это снижает стоимость смены границ и позволяет гибко перераспределять память между поколениями.
- **DATAS** удерживает размер кучи пропорциональным объёму живых данных и возвращает память ОС при снижении нагрузки. Для чувствительных к задержкам сценариев отключается через `<GarbageCollectionAdaptationMode>0</GarbageCollectionAdaptationMode>`.

### КТ №41 — Workstation GC vs Server GC
- **Workstation GC** — одна куча, фоновая сборка Gen2, короткие паузы. Подходит для клиентских приложений.
- **Server GC** — отдельная куча и поток GC на ядро, максимизирует пропускную способность за счёт более длинных пауз.
- Режим выбирается через `DOTNET_gcServer` / `<ServerGarbageCollection>` в `.csproj`.

### КТ №42 — финализатор, `IDisposable`, `using`
- Финализатор **непредсказуем** по времени и не должен трогать управляемые ресурсы.
- `using` гарантирует **детерминированный** вызов `Dispose()` при выходе из блока.
- `using var log = new LogFile(...)` (без скобок) закрывает ресурс в конце **метода**.
- `GC.SuppressFinalize(this)` в `Dispose()` отключает финализатор после явного закрытия.
- Сценарий «забыли `Dispose`» демонстрирует, что финализатор срабатывает только после `GC.Collect()` + `GC.WaitForPendingFinalizers()`.

### КТ №43 — полный паттерн `Dispose` и `IAsyncDisposable`
- Полный паттерн: `Dispose()` → `Dispose(bool disposing)` → в `disposing == true` освобождаем управляемые ресурсы; в `disposing == false` — только нативные.
- `IAsyncDisposable` и `await using` нужны для ресурсов с асинхронным освобождением (сеть, БД, файловые буферы).
- Класс может реализовывать **оба** интерфейса — это позволяет выбирать `using` или `await using` по контексту.
- `GC.SuppressFinalize(this)` вызывается и из `Dispose`, и из `DisposeAsync`.

---

## 6. История коммитов

Формат — [Conventional Commits](https://www.conventionalcommits.org/).

| № | Коммит | Что входит |
|---|--------|------------|
| 0 | `chore: initialize .NET 10 console project` | Решение, проект, `.gitignore`, первый README |
| 1 | `feat(gc): add generations and pressure measurement` | KT №40: `GcSnapshot`, `GcObserver`, `PressureScenarios`, команда `gc-pressure` |
| 2 | `feat(gc): compare workstation and server gc modes` | KT №41: `GcModeScenarios`, измерение пауз, команда `gc-modes` |
| 3 | `feat(resources): add finalizer and IDisposable examples` | KT №42: `FakeResource`, `ManagedResource`, `LogFile`, `ResourceDemos`, команда `resources` |
| 4 | `feat(dispose): implement full dispose pattern and IAsyncDisposable` | KT №43: `DatabaseConnection`, `FileBuffer`, `DisposeDemos`, команда `dispose` |
| 5 | `docs: add measurement results and conclusions` | README, `docs/`, таблицы результатов, выводы |

Просмотр истории:

```
git log --oneline --graph
```

---

## 7. Пример вывода программы

### 7.1. `-- resources` (KT №42)

```
1. Финализатор: момент вызова не контролируется
[FakeResource 1] открыт
[FakeResource 2] открыт
Метод завершён, финализаторы ещё не выполнялись
[FakeResource 1] финализатор
[FakeResource 2] финализатор
После GC.Collect + WaitForPendingFinalizers

2. using: детерминированное освобождение
[ManagedResource 1] открыт
Работа внутри using-блока
[ManagedResource 1] Dispose вызван
Строка после using-блока

3. Забытый Dispose: ресурс живёт до сборки
[ManagedResource 2] открыт
Ресурс создан, IsDisposed = False
Метод завершён, Dispose не вызывался
После сборки (ресурс уже недостижим)

4. LogFile: корректный и некорректный сценарии
[LogFile 1] файл открыт: test.log
[LogFile 1] файл закрыт
После корректного using-блока
[LogFile 2] файл открыт: test.log
Метод с забытым Dispose завершён
[LogFile 2] финализатор! — файл не был закрыт явно
После сборки: сработал финализатор
```

### 7.2. `-- dispose` (KT №43)

```
1. Синхронный сценарий using
[DatabaseConnection 1] соединение открыто
[DatabaseConnection 1] выполняется: SELECT * FROM Students
[DatabaseConnection 1] Dispose(): соединение закрыто

2. Асинхронный сценарий await using
[DatabaseConnection 2] соединение открыто
[DatabaseConnection 2] выполняется: SELECT * FROM Topics
[DatabaseConnection 2] Dispose(): соединение закрыто
[DatabaseConnection 2] DisposeAsync(): завершено

3. Проверка GC.SuppressFinalize после using
[DatabaseConnection 3] соединение открыто
[DatabaseConnection 3] выполняется: SELECT 1
[DatabaseConnection 3] Dispose(): соединение закрыто
После сборки: сообщение финализатора отсутствует

4. Финализатор без Dispose
[DatabaseConnection 4] соединение открыто
[DatabaseConnection 4] выполняется: UPDATE Students SET Name = 'Test'
[DatabaseConnection 4] финализатор: закрыт только нативный ресурс
После сборки: финализатор освободил только нативный ресурс

5. FileBuffer: using и await using
[FileBuffer 1] буфер создан
[FileBuffer 1] записано 4 байт
[FileBuffer 1] Dispose(): буфер освобождён
[FileBuffer 2] буфер создан
[FileBuffer 2] записано 3 байт
[FileBuffer 2] DisposeAsync(): буфер освобождён
```

---

## 8. Автор

- **ФИО:** Халилов Илья Велорович
- **Группа:** 10924-2
- **Дисциплина:** Язык программирования C#
- **Год:** 2026

---

