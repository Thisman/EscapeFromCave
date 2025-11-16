# AGENTS.md

---
Хранит общую структуру папки Scripts
## Scripts Directory Structure

- `Scenes/`: Scene-specific MonoBehaviours and UI assets grouped by screen; use it for anything tightly coupled to a particular scene setup.
  - `Battle/`: Battle scene managers, lifetime scope, payload, and battle-only UI documents/controllers.
  - `Dungeon/`: Dungeon scene wiring, lifetime scope, and the dungeon HUD layout/controllers.
  - `Preparation/`: Preparation menu scene bootstrap and its UI documents/controllers.
  - `MainMenu/`: Main menu scene wiring plus its visual layout and controller scripts.
  - `Root/`: Root scene bootstrap components that host global composition before switching to gameplay scenes.
- `Gameplay/`: Reusable gameplay systems decoupled from individual scenes.
  - `Battle/`: Core battle mechanics, data models, controllers, effects, abilities, and AI utilities.
  - `Interactions/`: Interaction contexts, resolvers, and ScriptableObject effects shared across exploration.
- `Entities/`: Domain models and controllers for persistent game entities.
  - `Units/`: Unit, squad, and army definitions plus their read-only interfaces.
  - `Player/`: Player-specific controllers for movement, interactions, combat input, and resource management.
  - `Objects/`: Environment object and resource models/controllers with their access interfaces.
  - `Resources`: Player resources: food, energy, etc.
- `Services/`: Global services such as session, scene loading, input, audio, dialogs, and shared interfaces.
- `UICommon/`: Shared UI Toolkit assets (e.g., global styles) reused across scenes.
- `Utilities/`: Cross-cutting helper classes like cooldown/state utilities and scene helpers.

---
Описывает стандарты нейминга и зон ответственности основных архитектурных сущностей в игровом проекте.

## 1. Controller

### 1. Краткое описание сущности
Контроллер — координатор поведения в рамках конкретного сценария, экрана или игровой ситуации. Управляет порядком действий, дергает модель и сервисы, обновляет представление.

### 2. ЗО сущности
- Обработка входных событий (input, UI, триггеры).
- Управление сценарием: выбор действия, переключение состояний, последовательность шагов.
- Коммуникация с моделью (чтение/запись) и вызовы сервисов/менеджеров.
- Управление жизненным циклом объектов в пределах сцены/контекста.

### 3. Ограничения
- Не хранить глобальный стейт игры.
- Не знать устройство других подсистем, кроме тех, что строго нужны.
- Не реализовывать тяжелую бизнес-логику и формулы.
- Не использовать `static` или singleton-подход для глобального доступа.
- Не работать напрямую с низкоуровневой ресурсной логикой (пулы, файлы, ассеты).

### 4. Примеры
- `BattleController` — управляет ходами боя, дергает модель юнитов, вызывает эффекты.
- `InventoryUIController` — контролирует открытие/закрытие окна инвентаря, обновляет отображение.
- `DialogueController` — оркестрирует диалоговую сцену, переключает реплики, взаимодействует с UI.

---

## 2. Manager

### 1. Краткое описание сущности
Менеджер — владелец подсистемы или ресурса. Предоставляет API для работы с тяжелыми или глобальными объектами: аудио, объектами, сценами, пулами.

### 2. ЗО сущности
- Управление ресурсами: загрузка, кеширование, выдача, освобождение.
- Централизованный доступ к подсистеме.
- Инкапсуляция низкоуровневых деталей (пулы, Addressables, загрузчики).
- Контроль жизненного цикла ресурсов на уровне проекта.

### 3. Ограничения
- Не реализовывать геймплейных формул и бизнес-логики.
- Не зависеть от конкретных UI-экранов или контроллеров.
- Не становиться "God Object" (`GameManager`, который делает всё).
- Не хранить данные модели (статы, прогресс, параметры геймплея).

### 4. Примеры
- `AudioManager` — хранит звуки, микширует, управляет воспроизведением.
- `PoolManager` — выдает объекты из пулов, возвращает их, оптимизирует аллокации.
- `SceneManager` (обертка) — регулирует переходы между сценами.

---

## 3. Service

### 1. Краткое описание сущности
Сервис — абстрактный функциональный модуль (обычно через интерфейс), предоставляющий доменные операции: сохранение, загрузка, расчет, сетевую работу. Не зависит от сцены и UI.

### 2. ЗО сущности
- Реализация доменной логики: работа с прогрессом, конфигами, формулами.
- Взаимодействие с внешними источниками/платформами (файлы, сеть, облако).
- Предоставление стабильного и легко заменяемого API (`I*Service`).
- Выполнение операций, не принадлежащих конкретной сцене/контроллеру.

### 3. Ограничения
- Не знать об объектах сцены и UI.
- Не иметь прямых ссылок на контроллеры.
- Не хранить состояние сцены.
- Не зависеть от Unity-компонентов там, где это возможно.

### 4. Примеры
- `ISaveService` — сохранение и загрузка прогресса.
- `IProgressionService` — расчет опыта, уровней, стоимости улучшений.
- `IAnalyticsService` — отправка событий в аналитику.

---

## 4. Model

### 1. Краткое описание сущности
Модель — источник истины данных и правил. Хранит состояние и реализует чистую логику без привязки к Unity, сценам и UI.

### 2. ЗО сущности
- Хранение и модификация данных (здоровье, инвентарь, параметры).
- Реализация базовых правил и формул (урон, бонусы, ограничения).
- Генерация событий изменения состояния.
- Подготовка данных, которые могут быть сериализованы/сохранены.

### 3. Ограничения
- Не содержать ссылок на Unity-компоненты (`MonoBehaviour`, `Transform`, `GameObject`).
- Не взаимодействовать с UI, сценами или контроллерами.
- Не хранить низкоуровневые ресурсы.
- Не вызывать сервисы/менеджеры напрямую.

### 4. Примеры
- `UnitModel` — здоровье, атака, защита юнита + методы нанесения урона.
- `InventoryModel` — список предметов и операции добавления/удаления.
- `QuestModel` — состояние задач, прогресс, флаги выполнения.

---
