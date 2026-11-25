# P4-06 · Coordinator та communicator без навчання

Дата: 2026-11-23. **Перевірений частковий результат P4-06, статус In Progress.** Продовження [reward/reset adapter](AI_NPC_P406_ADAPTER_VERIFICATION.md). Критерії цього зрізу: відтворити всі 449 frozen train-рецептів через реальні сенсори, зберегти один запит/вибір/reward на випадок, зупинити пошкоджений run та перевірити Python terminal/reset через справжній communicator без optimizer.

## Реалізація

- [CopyAttentionRecipeCoordinator](../Assets/Scripts/AI/TheCopy/CopyAttentionRecipeCoordinator.cs) — окремий disposable coordinator для batch-sandbox. Його IEnumerator виконує caller; при завершенні/помилці caller має викликати Dispose. Потрібні активний server training-Agent і два реальні PlayerObject того самого NGO-менеджера зі стабільними A/B за OwnerClientId. Production-сцени не підключають його автоматично.
- До першого запиту перевіряються SHA-256 train-рецептів/маніфесту за generation-audit, версії контрактів, 449 унікальних case_id та кількість випадків кожної родини. Координатор читає тільки train; ID/OOD-рецепти та їхні вектори для нього не потрібні. Записуються run-context і маніфест до збору результатів. Наявний output не перезаписується.
- Після reset coordinator очищає слух і задає геометрію, наявність сенсорів, ліхтарик та звукову подію через чинні NPC API. Для sound-memory очікує погоджений вік пам’яті. Вектор не генерується з рецепту: CopyDecisionInputV2 захоплює фактичні 50 ознак. CopyHearing додатково відкриває read-only MemoryLifetime/LiveWindow без зміни алгоритму слуху.
- [CopyAttentionTrainingAgent](../Assets/Scripts/AI/TheCopy/CopyAttentionTrainingAgent.cs) надає копії pending frame/mask та зберігає обрану дію/ціль перед reset. Coordinator копіює кадр до callback, перевіряє сенсорну наявність за родиною, очікує terminal і звіряє фактичний reward. Наступний рецепт застосовується після завершення callback/reset. Вивід — observations, mask, action, target, assigned/cumulative reward, case_id, policy seed окремо від generation seed=1001. Latency=null: її тут не вимірювали.
- Capture failure, exception, timeout або technical interruption припиняють run та деактивують training-Agent; summary має valid_run=false, причину й counts. Немає переходу до наступного рецепту, підставленого нульового вектора чи запису reward=0 замість технічної відмови. Геометрія, сенсори та стани вимкнених sandbox-компонентів відновлюються.
- [P406CoordinatorVerification](../Assets/Tests/AI/P406CoordinatorVerification.cs) створює generated NPC тільки в тимчасовій копії. Контрольний режим виконує всі 449 train-випадків через явний HeuristicOnly для API-перевірки. Це не нейромережеві рішення та не навчання. Default використовується в окремому communicator smoke.

## Фактично виконані перевірки

1. **Unity 6000.6.5f1: 449/449 train-випадків завершені**, по одному JSONL-запису та reward на prepared decision; повний manifest. **12/12 coordinator checks**, включно з immutable frame/mask та двома ін’єкціями відмов. Technical interruption: prepared=1, recorded=0, run Aborted, Agent вимкнений. Capture failure: prepared=0, recorded=0, run Aborted. У цьому ж прогоні **4758/4758 adapter assertions + 274/274 regression/diagnostic PASS**, Unity exit=0. [Журнал](../Logs/AI-NPC/P4-06-coordinator-isolated-20261123.log).
2. **Справжній Python mlagents_envs 1.1.0 ↔ Unity communicator: 8/8 diagnostic-випадків**, по одному першому рецепту кожної родини з train. Python надсилає uniform allowed random із seed=2026, перевіряє 50 inputs / discrete [7], маски 5/6, одноетапні terminal transitions і reward за Python-формулою. Реальні C# кадри/маски/дії зіставлені з Python terminal-записами в порядку запитів, точність reward ≤1e-6. **8/8 C# smoke checks, Unity exit=0.** Початковий Academy reset обмінюється до маніфесту; фінальний terminal примусово відправляється до teardown. Це diagnostic split, не дослідницька train/ID/OOD оцінка. [Результат із terminal transitions](../Logs/AI-NPC/P4-06-communicator-final-20261123/result.json), [Unity-журнал](../Logs/AI-NPC/P4-06-communicator-final-20261123/unity.log).
3. **109/109 Python unit-тестів PASS** на Python 3.13.7 і ML Python 3.10.12. Нові тести перевіряють disabled-mask семантику SDK, interrupted terminal, reward, counts, seeds, missing cases, заборону ID/OOD та NaN/Infinity/нечислові нагороди. [Python 3.13](../Logs/AI-NPC/P4-06-coordinator-python-20261123.log), [Python 3.10](../Logs/AI-NPC/P4-06-coordinator-python310-20261123.log).

[Збережені 449 captures, відмови, audit і provenance](../Logs/AI-NPC/P4-06-coordinator-evidence-20261123/provenance.json) лежать поза disposable workspace. [Окремі communicator captures і audit](../Logs/AI-NPC/P4-06-communicator-evidence-20261123/provenance.json) також скопійовано. SHA-256 семи виконаних C#-джерел звірено з checkout; snapshot журналу та файли доказів мають hashes. Logs локальні й ignored, не нові Git-артефакти навчання.

## Виявлені та виправлені проблеми

- Перший повний coordinator-run зупинився на player-a/000 після 193 випадків: у fixture два NetworkManager мають одну PhysicsScene, тому клієнтські репліки колайдерів перекривали серверний sight після реплікації позицій. У fixture тимчасово вимкнено тільки колайдери spawned-об’єктів клієнтського менеджера з відновленням у finally. Production-оклюзію не змінено. Перший Aborted run не є PASS; повні 449 зібрано заново.
- Перший communicator smoke обмінявся випадками, але Python чекав завершення Unity до закриття свого gRPC-сервера, а Unity чекала з’єднання. Запуск позначено failed і не зараховано. Python тепер закриває env до очікування завершення Editor; повторний запуск отримав exit=0.
- Offline-audit явно відхиляє нечислові та нескінченні rewards: самого порівняння abs(error)>tolerance недостатньо для NaN.

## Відтворення

Контрольний повний прогін, без optimizer, з кореня репозиторію:

```powershell
.\tools\Verify-P305.ps1 -UseWebSockets -VerifyTrainingCoordinator -KeepWorkspace `
  -MlPython 'D:/PROGS/anaconda3/envs/project-s-ml/python.exe' `
  -RewardPlan 'Logs/AI-NPC/P4-05-recipe-plan-20261123' `
  -RewardCapture 'Logs/AI-NPC/P4-05-replay-evidence-20261123'
```

CoordinatorPlan початково дорівнює RewardPlan; у disposable copy копіюються тільки train-recipes.json, train-manifest.json і generation-audit.json. Старий reward parity exporter окремо читає audited train/ID для тесту арифметики, не для навчання. Launcher додає -p406Coordinator до adapter/regression suite й викликає [evidence helper](../tools/Export-AINpcCoordinatorEvidence.ps1). **Нову cold-copy комбінацію launcher-а перевірено синтаксично, але не запускали повністю:** наведені результати отримано безпосереднім запуском того самого C# suite у warm disposable copy; helper фактично викликано для обох збережених наборів.

Окремий communicator smoke для підготовленої disposable copy (не поєднувати з -p406TrainingAdapter, який тестує відмову без communicator):

```powershell
& 'D:/PROGS/anaconda3/envs/project-s-ml/python.exe' -B tools/verify_ai_npc_communicator.py `
  --unity 'C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe' `
  --workspace 'Logs/AI-NPC/p305-workspace-445f2aa1' `
  --plan 'Logs/AI-NPC/p305-workspace-445f2aa1/Logs/AI-NPC/P405RecipePlan' `
  --output 'Logs/AI-NPC/P4-06-communicator-repeat' --port 5027
```

Для нового workspace використовуй збережений launcher-ом P406CoordinatorPlan. Каталог output має бути новим, порт — вільним; Unity не має вже працювати з цією disposable copy. Python запускає прихований Editor, задає bounded timeout, завершує тільки власний процес після помилки. SDK-версії вже встановлені; tool не встановлює залежностей, не імпортує trainer/PPO й не створює optimizer.

Повторний аудит збережених файлів:

```powershell
python -B tools/validate_ai_npc_coordinator.py `
  --capture Logs/AI-NPC/P4-06-coordinator-evidence-20261123 `
  --output Logs/AI-NPC/P4-06-coordinator-repeat-audit.json
python -B -m unittest discover -s tools/tests -v
```

## Межі та наступна задача

Optimization steps=0, trainer created=false, нових checkpoints/навчених ONNX немає. Random тут перевіряє API, не замінює нейромережевий Decision Maker. ID/OOD policy quality не оцінювали, OOD-вектори не читали. Host/Client — різні NGO-менеджери в одному Editor через loopback WebSocket, не два процеси кооперативної гри.

Coordinator обриває невалідний run, але це **не доказ**, що PPO автоматично відкидає EpisodeInterrupted transition: optimizer тут відсутній. Для реального PPO потрібна явна політика завершення/відбракування такого запуску.

**Оновлення 2026-11-24:** наступний bootstrap-зріз реалізовано: optional schedulePath додає повторення/перемішування train до старого single-pass API. Перевірено 898 train та 16 diagnostic transitions без optimizer, terminal-only reset і abort-сигнал Python. [Новий звіт та актуальний наступний крок](AI_NPC_P406_BOOTSTRAP_VERIFICATION.md). Докази вище стосуються попереднього coordinator-зрізу. P4-06 не Done; PPO-supervisor, окреме погодження й фактичні незалежні навчальні запуски ще потрібні; до P4-07 не переходити.
