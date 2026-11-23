# P4-04 · Записи рішень Agent для evaluator-а

**Подальша перевірка перед комітами:** повний launcher `-UseWebSockets -VerifyDecisionTrace` створив нову копію й завершив 186/186 перевірок; postprocess зберіг три окремі JSONL/metrics-файли. [Журнал](../Logs/AI-NPC/P4-04-trace-445f2aa1.log), [smoke-метрики](../Logs/AI-NPC/p404-records-445f2aa1/p404-smoke-records-metrics.json). Початковий sandbox-запуск із проблемами Licensing/Burst не зараховується. Після окремого погодження реалізовано [offline-reward](AI_NPC_P404_REWARD.md), але цей recorder/evaluator його не обчислює. Нижче збережено історію producer-зрізу до погодження reward.

## Обсяг і критерії · 2026-11-19

Продовжено доступну частину P4-04 після метрик: автоматичний producer JSONL у підсистемі NPC та перевірка всього ланцюга **Agent → запис → evaluator**. Рішення користувача відкласти reward чинне. Формула reward, еталони й пороги якості не додаються, навчання не запускається.

Критерії: запис лише на сервері й за явного ввімкнення, початкова маска того самого запиту, одна завершальна подія на запит, nullable action/latency для скасування, причини відмов, відсутність впливу помилки запису на прийняття нейромережевого рішення, окремі файли справжніх forward та штучних контрольних callback-ів.

## Реалізація

- [CopyDecisionTrace](../Assets/Scripts/AI/TheCopy/CopyDecisionTrace.cs) — копія request ID, захоплених кандидатів, вибраного індексу й цілі, status/reason та nullable latency.
- [CopyDecisionAgent](../Assets/Scripts/AI/TheCopy/CopyDecisionAgent.cs) публікує DecisionCompleted для accepted/rejected, а CancelPendingDecision — no_response, лише якщо був pending-запит. Повторне скасування не створює дубль. Перед подією pending-стан і буфер очищаються. Чинна DecisionAccepted збережена. Рішення та маски нейромережі не змінені.
- Latency вимірюється Time.realtimeSinceStartupAsDouble від прийнятого запиту, після захоплення входів і до RequestDecision, до входу в OnActionReceived, **до повторної перевірки та файлового I/O**. Це response latency із плануванням Academy, не чистий час worker-а й не час виконання ігрової дії. Строк придатності відповіді у 0,5 scaled секунди лишається окремою чинною перевіркою.
- Причини: authority_denied, configuration_invalid, malformed, stale, perception_unavailable, masked_or_unavailable, target_changed; для no_response — явна причина скасування/reset/despawn. None-response не видається за відповідь із нульовим часом.
- [CopyDecisionTraceRecorder](../Assets/Scripts/AI/TheCopy/CopyDecisionTraceRecorder.cs) підписується тільки на spawned-сервері. Потрібні задані runId/policyId, scenario, split і seed. На клієнті файла й підписки немає. Запис reference_outcomes завжди null: producer не винаходить еталон.
- Маска отримується із кандидатів захопленого запиту, а не з нового стану світу. Ціль — копія позиції й ID із цих кандидатів. JSON містить коректні null і bool; використано вже наявний Newtonsoft JSON, пакети не змінено.
- Підписка серверного recorder-а дозволяє записати термінальне скасування під час NGO despawn, навіть коли мережеві прапори вже очищуються. Потім recorder відписується. Вимкнення самого recorder-а зупиняє запис і не скасовує запит Agent.
- Помилка файлового I/O/серіалізації зберігається в LastWriteError, дає Warning та вимикає recorder. Рішення Agent продовжує оброблятися. Invalid configuration також вимикає recorder. WrittenRecords — лічильник записів поточного компонента, OutputPath — фактичний файл.

## Використання

Компонент recorder додають до NPC **до NGO spawn**. Він необов’язковий: без нього файли не створюються. У production-сцени чи prefab його не додавали.

Перед Play задають runId, policyId, scenario, split (train/id/ood/diagnostic), seed і filename. Якщо outputDirectory порожній, використовується Application.persistentDataPath/AI-NPC; автоматичний тест явно задає Logs/AI-NPC у тимчасовому проєкті. Для різних runs/policies/seeds/splits потрібно вибирати різні файли. Recorder дописує рядки, не перезаписує старі дані; evaluator відхиляє змішані контексти. case_id містить унікальний ID сесії recorder-а та номер запиту.

Запис синхронний і може додавати I/O-навантаження; він вимкнений у наявних сценах. Вимір response latency не включає вартість запису. Цей механізм ще не є оптимізованою системою масового збирання навчальних даних.

Recorder записує лише завершення запитів, зроблених під час активної підписки. Зміна підписки посеред запиту може втратити його запис. Незаплановані запити, відмова захоплення входів і відкриті на момент завершення запису pending-запити не породжують штучного успіху чи маски; їх потрібно окремо обліковувати у майбутньому експериментальному runner-і.

Автоматичного watchdog-таймауту не додано: no_response з’являється при явному скасуванні/reset/despawn/вимкненні Agent. Якщо Academy не крокує й запит не скасовано, він лишається pending. Це не доказ здатності виявляти всі можливі зависання. Метрики тепер показують no_response_reasons окремо, щоб контрольоване скасування не змішувалось із невідомим timeout.

## Перевірки

[P404TraceVerification](../Assets/Tests/AI/P404TraceVerification.cs) додає вимкнений recorder лише в генерований NPC тестового fixture. Попередні штучні Agent-callback-и не записуються. Потім:

1. Вимкнено сенсорні цілі, залишено лише Continue у масці. Виконано **три реальні CPU-forward** ненавченої ONNX. Власний файл p404-smoke-records.jsonl, policy_id=untrained-neural-smoke, scenario=quiet-controlled-smoke. Це технічний контроль, не дослідницька вибірка якості.
2. Перевірено вимкнений recorder і штучну помилку диска: outputDirectory є звичайним файлом. Recorder вимикається з помилкою, прийняття NN-рішення зберігається. Цей окремий fault-injection не додається до файлу NN-відповідей.
3. В **інший** файл і policy_id=controlled-callback-fixture записано штучну відповідь 5 та явне скасування; перевірено nullable поля й відсутність дубля після повторного cancel.
4. В третій файл і policy_id=controlled-lifecycle-fixture записано справжнє скасування pending-запиту при NGO despawn.

**22/22** нові інтеграційні перевірки та попередні **38 + 33 + 28 + 23 + 42 = 164/164**, разом **186/186**, Unity 6000.6.5f1, код **0**. [Фінальний журнал](../Logs/AI-NPC/P4-04-trace-final-20261119.log). Використано наявну тимчасову копію p305-workspace-da75f8b5 зі синхронізованими скриптами та раніше експортованими сумісними smoke-моделями. Два NGO-менеджери в одному процесі, UTP WebSocket; не два standalone-процеси й не Relay.

Evaluator перевірений після додавання причин no_response: **26 нових + 15 попередніх = 41/41 PASS** на Python 3.13.7 та 3.10.12. [Системний журнал](../Logs/AI-NPC/P4-04-trace-python-tests-20261119.log), [ML Python](../Logs/AI-NPC/P4-04-trace-python-tests-py310-20261119.log).

## Фактичний ланцюг JSONL → метрики

Окремий [Export-AINpcTraceEvidence.ps1](../tools/Export-AINpcTraceEvidence.ps1) перевіряє, що директорія з маркера Unity-логу лежить у Logs тестового workspace, копіює три файли та викликає той самий evaluator окремо для кожного контексту. Цей postprocess виконано на успішному прогоні; assertions підтвердили:

- **Реальні NN-forward:** 3 accepted, 3 виміри latency, 0 прийнятих некоректних рішень, 0 еталонів; accuracy залишається null. [Записи](../Logs/AI-NPC/p404-records-final-20261119/p404-smoke-records.jsonl), [метрики](../Logs/AI-NPC/p404-records-final-20261119/p404-smoke-records-metrics.json)
- **Контрольовані callback-и:** 1 rejected, 1 no_response, 0 прийнятих порушень. Вони не домішані до NN-вимірів. [Записи](../Logs/AI-NPC/p404-records-final-20261119/p404-controlled-records.jsonl), [метрики](../Logs/AI-NPC/p404-records-final-20261119/p404-controlled-records-metrics.json)
- **NGO despawn:** 1 no_response, null action/target/latency та явна причина. [Записи](../Logs/AI-NPC/p404-records-final-20261119/p404-pending-despawn.jsonl), [метрики](../Logs/AI-NPC/p404-records-final-20261119/p404-pending-despawn-metrics.json)

Числа не є оцінкою навченої політики чи обчислювальної продуктивності: модель ненавчена, ситуація контрольована, доступний лише Continue, вибірка три відповіді. Reward не обчислюється, еталони й пороги якості не визначені. У звітах reward_status=not_defined та policy_quality_conclusion=not_assessed.

## Відтворення

```powershell
.\tools\Verify-P305.ps1 -UseWebSockets -VerifyDecisionTrace -MlPython 'D:/PROGS/anaconda3/envs/project-s-ml/python.exe' -KeepWorkspace
```

Новий прапор підключає всі залежні тести Agent/контракту/сенсорів і постобробку JSONL. Моделі й записи лишаються в Logs або тимчасових Assets. Каталог доказів копіюється за межі disposable-workspace перед його можливим видаленням. У поточній перевірці Unity-частину запущено безпосередньо в підготовленій копії, а той самий postprocess — окремим helper-ом; нову повну холодну копію цим прапором не створювали. Синтаксис обох PowerShell-скриптів перевірено.

Для повторного аналізу вже успішного прогону без запуску Unity можна викликати helper з UnityLog, Workspace, OutputDirectory та MlPython. Параметр target-tolerance=0.05 у цьому діагностичному postprocess не впливає на accuracy: еталони null; це не поріг майбутнього експерименту.

## Статус

**P4-04 · In Progress.** Метрики й producer технічних записів перевірені. Reward залишається відкладеним за рішенням користувача. Потрібні навчальна мета, джерело еталонів/наслідків, baseline та критерії якості; потім контрольовані дослідницькі випадки й їхній runner у P4-05. Цей recorder не є таким runner-ом і не створює train/ID/OOD набори. Фізичний Action Controller й наслідки головоломки не реалізовувалися цим кроком.
