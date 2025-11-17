# AGENTS.md (optimized)

---

# Base

## Scripts Directory Structure

- **Scenes/** — сцено-зависимые MonoBehaviours и UI.
  - **Battle/** — менеджеры боя, payload, UI.
  - **Dungeon/** — HUD и загрузка.
  - **Preparation/** — меню подготовки.
  - **MainMenu/** — главный экран.
  - **Root/** — корневой bootstrap.
- **Gameplay/** — переиспользуемые геймплейные системы.
  - **Battle/** — механики боя, модели, эффекты, AI.
  - **Interactions/** — контексты и ScriptableObject-эффекты.
- **Entities/** — доменные данные и контроллеры сущностей.
  - **Units/** — юниты, squads, интерфейсы.
  - **Player/** — контроллеры игрока.
  - **Objects/** — объекты окружения.
  - **Resources/** — ресурсы игрока.
- **Services/** — глобальные сервисы (сессия, сцены, аудио, input).
- **UICommon/** — переиспользуемые UI-компоненты.
- **Utilities/** — утилиты и вспомогательные классы.

---

# Code Styles

## 1. Controller

**Описание:** Координатор сценария/экрана. Управляет последовательностью действий, вызывает модели и сервисы, обновляет UI.

**ЗО:** input/UI события, шаги сценария, работа с моделью/сервисами, управление объектами сцены.

**Ограничения:** без глобального стейта; не знает лишних подсистем; без тяжелой логики; без static/singleton; не работает с низким уровнем.

**Примеры:** BattleController, InventoryUIController, DialogueController.

---

## 2. Manager

**Описание:** Владелец подсистемы/ресурса. Даёт централизованное API.

**ЗО:** загрузка/кеширование/освобождение ресурсов; инкапсуляция низкого уровня; управление жизненным циклом.

**Ограничения:** без геймплейных формул; без UI; не становится God Object; не хранит модельные данные.

**Примеры:** AudioManager, PoolManager, SceneManager (обертка).

---

## 3. Service

**Описание:** Функциональный модуль через интерфейс. Не зависит от сцены/UI.

**ЗО:** доменная логика, работа с файлами/сетью, стабильное API.

**Ограничения:** без ссылок на сцену и UI; минимум Unity-зависимостей.

**Примеры:** ISaveService, IProgressionService, IAnalyticsService.

---

## 4. Model

**Описание:** Источник истины. Чистая логика + состояние, без Unity.

**ЗО:** данные и правила; события; сериализация.

**Ограничения:** без Unity-компонентов, UI и сервисов.

**Примеры:** UnitModel, InventoryModel, QuestModel.

---

# UI Toolkit

## Основные задачи

UXML — структура.  
USS — стиль.  
UIController — поведение без бизнес-логики.  
Widgets — переиспользуемые UI-элементы.

---

## Правила

- без inline-стилей;  
- классы — lowercase + BEM;  
- name — PascalCase;  
- 1 UXML = 1 сущность;  
- только классы в USS;  
- минимальная вложенность;  
- состояния — модификаторы (`--selected`).

---

## UIController

**Расположение:** `Scripts/Scenes/<Scene>/UI`  
**Имя:** `{UXML}UIController.cs`

**ЗО:** загрузка UIDocument, поиск элементов, события UI, обновление вида.  
**Ограничения:** без бизнес-логики; без динамического UI без необходимости.

---

## Widgets

**Расположение:** `UICommon/Widgets/`  
**Состав:** `Name.uxml`, `Name.uss`, `NameWidget.cs`

**Требования:** самостоятельность, переиспользуемость, API (`Init`, `Bind`, `UpdateView`), без зависимости от сцен.

---

# Null Checks

## Принципы
- максимум проверок — в редакторе;  
- важные ссылки — через Validate();  
- логические инварианты ≠ ошибки сцены.

## Подход

- обязательные компоненты:
  ```csharp
  [RequireComponent(typeof(Rigidbody))]
  ```
- ссылки из инспектора:
  ```csharp
  abstract class ValidatedMonoBehaviour : MonoBehaviour {
      protected abstract void Validate();
  }
  // Validate:
  RequireNotNull(_animator, nameof(_animator));
  ```
- запуск Validate в `OnValidate()` + `Awake()`  
- получение компонентов:
  ```csharp
  this.RequireComponent<Rigidbody>();
  ```
- Assertions — только для логики;  
- Null Object — если отсутствие поведения допустимо.

## Результат
Нет `if == null`, есть однозначные ошибки и чистый рантайм.
