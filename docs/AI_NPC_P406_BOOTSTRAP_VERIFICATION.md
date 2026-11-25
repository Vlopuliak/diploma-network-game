# P4-06 · Повторення train та контроль запуску

Дата: 2026-11-24. Продовження [coordinator/communicator](AI_NPC_P406_COORDINATOR_VERIFICATION.md). **P4-06 In Progress; цей зріз не запускає PPO.** Результат — відтворюваний розклад повторень frozen train, виконання через чинний training-Agent та передавання аварійного стану Python-клієнту API.

## Реалізований контракт

- [training_schedule.py](../ai_training/cases/training_schedule.py) читає тільки train-recipes.json, train-manifest.json і generation-audit.json. Перевіряє SHA-256, кількість 449, унікальність ID та покриття восьми родин. ID/OOD-файли не є входами цього маршруту, frozen рецепти не переписуються.
- `build_schedule` створює окремий запис на кожен епізод: case_id=`epoch/0000/train/...`, source_case_id — оригінальний train ID, epoch — номер проходу з 0. У кожному проході кожен обраний рецепт присутній рівно один раз. Python Random(shuffle_seed) перемішує копію списку для кожного проходу; збережений schedule.json є джерелом фактичного порядку, а не залежність від іншого PRNG у C#.
- Generation seed=1001 описує frozen дані; policy_seed передається в UnityEnvironment і Python Random для API-клієнта; shuffle_seed визначає порядок рецептів. Значення розділені в schedule/context/launch metadata. Ці технічні policy seeds ще не означають незалежні PPO-training runs.
- [CopyAttentionRecipeCoordinator](../Assets/Scripts/AI/TheCopy/CopyAttentionRecipeCoordinator.cs) приймає необов’язковий schedulePath. Повторно перевіряє контракт, train hashes, кількість, покриття/унікальність кожної epoch, вихідні ID та сценарії. Застосовує вихідні рецепти через реальні сенсори; вектор не генерується Python-скриптом. Без schedule старий single-pass API збережений.
- Маніфест лишається сумісним із evaluator-ом attention-case-manifest/1: тільки case_id/scenario. Source ID та epoch зберігаються в розкладі й кожному JSONL-записі, не розширюють strict manifest. Context містить hashes розкладу/джерел, epochs та обидва seeds.
- [P406CoordinatorVerification](../Assets/Tests/AI/P406CoordinatorVerification.cs) має окремий batch-прапор -p406TrainingBootstrap: Default-Agent, реальні NGO PlayerObject та ручне Academy stepping. Початковий Python reset обмінюється через terminal-only EpisodeInterrupted без запиту дії й без AddReward. Python вимагає рівно стільки rewarded terminal transitions, скільки записів у розкладі; зайві епізоди не приховуються.
- Running/Completed/Aborted передаються через файл у sandbox Logs; запис замінюється атомарно. RunAborted записує причину й counters до наступного обміну з Python. Файл є локальним керуванням disposable fixture, не мережевою системою гри.
- [verify_ai_npc_communicator.py](../tools/verify_ai_npc_communicator.py) з --bootstrap формує розклад до запуску, зберігає команду, версії Python/SDK/numpy та hashes семи C#-джерел, звірених із checkout. Після успіху копіює captures за межі disposable workspace, перевіряє schedule hash, порядок, coverage, seeds, кадри/маски/дії/rewards та сумісність із чинним evaluator-ом.
- При Aborted Python піднімає помилку, зберігає failure.json з valid_run=false і Unity reason/counters, закриває communicator та завершує тільки власний Unity-процес. Немає автоматичного restart/resume або навчання на підставленій нульовій нагороді.

## Відтворення без навчання

Команди для наявної підготовленої disposable copy; Editor не повинен уже працювати з нею. Новий output і вільний порт потрібні для кожного запуску. На іншому комп’ютері спочатку створити disposable copy через [launcher](../tools/Verify-P305.ps1) з KeepWorkspace й актуальними NPC-джерелами, використати її train-only P406CoordinatorPlan. Встановлення пакетів цей маршрут не виконує.

```powershell
& 'D:/PROGS/anaconda3/envs/project-s-ml/python.exe' -B tools/verify_ai_npc_communicator.py `
  --unity 'C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe' `
  --workspace Logs/AI-NPC/p305-workspace-445f2aa1 `
  --plan Logs/AI-NPC/p305-workspace-445f2aa1/Logs/AI-NPC/P405RecipePlan `
  --output Logs/AI-NPC/bootstrap-repeat --port 5029 `
  --bootstrap --diagnostic --epochs 2 --seed 11 --shuffle-seed 7011
```

Diagnostic означає 8 вихідних train-рецептів, по одному на родину; два проходи дають 16 переходів. Це окремий diagnostic split. Для двох повних train-проходів прибрати --diagnostic, задати новий output/порт і --timeout 600: буде 898 переходів. В обох режимах Python вибирає uniform allowed random, не створює trainer/optimizer. Наявність слова training у назві bootstrap не означає оптимізацію ваг.

Для ін’єкції technical failure додати --inject-technical-failure до окремого diagnostic-запуску. **Очікується ненульовий exit і failure.json**, а не status=pass: перевірка успішна лише якщо запуск відбракований із конкретною причиною, без прихованого переходу до наступного рецепту.

```powershell
python -B -m unittest discover -s tools/tests -v
& 'D:/PROGS/anaconda3/envs/project-s-ml/python.exe' -B -m unittest discover -s tools/tests -v
```

## Межі

Цей Python-клієнт є перевіркою API й ніколи не запускає PPO. Зупинка цього клієнта не доводить автоматичне відкидання EpisodeInterrupted experience справжнім trainer-ом. Для фактичного PPO потрібен supervisor, який контролює trainer та Unity, припиняє й відбраковує невалідний run, збирає checkpoints/ONNX та підтверджує відповідність бюджету.

NPC не рухається й не виконує дій механізмів; дії 5/6 закриті. Host/Client у цьому fixture — два NGO-менеджери в одному Editor через WebSocket, не ручний кооперативний сеанс. Production-сцени, shared-компоненти та пакети не змінюються. NN-якість ID/OOD і фізичні наслідки не оцінюються.

## Перевірки та докази

- **Повний bootstrap: 898/898 переходів, 8/8 C# fixture checks + 126/126 regression checks, Unity exit=0.** Два перемішані проходи по всіх 449 train-рецептах; policy seed=22, shuffle seed=7022. Кожен source ID зустрічається рівно один раз у кожній epoch; rewarded transitions поза маніфестом=0. [Результат із terminal transitions](../Logs/AI-NPC/P4-06-bootstrap-full-final-20261124/result.json), [виконана команда, SDK/версії та hashes семи C#-джерел](../Logs/AI-NPC/P4-06-bootstrap-full-final-20261124/launch.json), [offline audit](../Logs/AI-NPC/P4-06-bootstrap-full-final-20261124/audit.json). У coordinator-capture збережено маніфест, context, 898 records і summary; optimization steps=0, trainer created=false, policy quality=not_assessed.
- **Diagnostic bootstrap: 16/16 переходів, 8/8 C# fixture checks, Unity exit=0.** Два проходи по восьми родинах; policy seed=11, shuffle seed=7011. Python зіставив усі frames/masks/actions/rewards, rewarded transitions поза маніфестом=0. [Результат із terminal transitions](../Logs/AI-NPC/P4-06-bootstrap-final-fixture-20261124/result.json), [launch metadata та hashes виконаного коду](../Logs/AI-NPC/P4-06-bootstrap-final-fixture-20261124/launch.json), [offline-аудит](../Logs/AI-NPC/P4-06-bootstrap-final-fixture-20261124/audit.json). Captures автоматично збережені в coordinator-capture під цим каталогом, поза disposable workspace.
- **117/117 Python unit-тестів PASS** на Python 3.13.7 і 3.10.12. Нові 8 перевірок охоплюють 898 унікальних occurrence IDs, точне покриття кожної epoch, детермінізм і незалежність seeds, diagnostic-статус повторів, відсутність читання ID/OOD, frozen hashes, помилки бюджету/seed, abort-сигнал та порядок/покриття captures. [Python 3.13](../Logs/AI-NPC/P4-06-bootstrap-python-20261124.log), [Python 3.10](../Logs/AI-NPC/P4-06-bootstrap-python310-20261124.log).
- Позитивний offline CLI перевірив збережені 16 випадків із --schedule; негативний CLI без розкладу відмовив із exit=2 й не створив результату. [Негативний журнал](../Logs/AI-NPC/P4-06-bootstrap-missing-schedule-20261124.log). Для scheduled run hashes/epochs не можна мовчки замінити припущенням про 449 випадків.

Додаткові докази та повторний offline-аудит:

- **Явна technical failure підтверджена:** Python child exit=1, Aborted, valid_run=false, prepared=1 / recorded=0, rewarded terminal count=0. Причина — `technical_interruption:injected_bootstrap_failure`; наступний випадок не виконувався. Це успішна перевірка відбракування, а не успішний навчальний run. [Failure із сигналом Unity](../Logs/AI-NPC/P4-06-bootstrap-abort-final-20261124/failure.json), [незалежний аудит abort summary](../Logs/AI-NPC/P4-06-bootstrap-abort-final-20261124/abort-audit.json). Неповний маніфест/context/summary скопійовані в coordinator-capture після зупинки процесу.

```powershell
python -B tools/validate_ai_npc_coordinator.py `
  --capture Logs/AI-NPC/P4-06-bootstrap-final-fixture-20261124/coordinator-capture `
  --schedule Logs/AI-NPC/P4-06-bootstrap-final-fixture-20261124/schedule.json `
  --output Logs/AI-NPC/P4-06-bootstrap-repeat-audit.json
```

Перший diagnostic-обмін повернув 16 потрібних рішень, але наступний аудит виявив додатковий handshake reward і несумісне розширення strict manifest. Це **не остаточний PASS**: bootstrap змінено на terminal-only reset, маніфест повернено до чинного контракту, успішний diagnostic зібрано заново. Перший full-run зупинився на prepared=425 / recorded=424 через sharing violation від діагностичного читача активного JSONL. Coordinator і Python відбракували запуск; [failure.json](../Logs/AI-NPC/P4-06-bootstrap-full-20261124/failure.json), summary та неповний capture збережені. Цей full-run не є PASS. Для моніторингу не відкривати активний JSONL читачем, який забороняє конкурентний запис.

Перший запуск явної ін’єкції перервався з іншою причиною: тест очищав pending frame до власної immutable-copy перевірки. Це не зараховується як підтвердження очікуваного injected interruption. Обробник ін’єкції переставлено після перевірки копії **лише в fixture**; runtime coordinator/training-Agent не змінювалися. Повний успішний 898-run виконаний перед цією точковою зміною fixture; його фактичний hash збережено в launch.json.

## Наступний крок

**Оновлення після bootstrap-зрізу:** supervisor/preflight та керування Unity/API-worker уже реалізовані й перевірені без optimizer. [Новий звіт і актуальна пропозиція PPO-пілота для погодження](AI_NPC_P406_SUPERVISOR_VERIFICATION.md). Текст нижче описує наступний обсяг на момент завершення bootstrap; фактичного PPO ще немає.

Підготувати PPO-supervisor для справжніх trainer/Unity процесів: preflight YAML/контракту, явний бюджет і незалежні seeds, запуск/завершення, artifact paths, version metadata, abort/discard цілого невалідного run. Перевірити керування процесами без оптимізації, після цього погодити конкретні команди й бюджет фактичних PPO-запусків. Наявний --bootstrap ніколи не створює trainer і не є такою командою навчання. P4-06 лишається In Progress; до P4-07 не переходити.

Остаточний fixture після виправлення порядку ін’єкції також пройшов позитивні **16/16 переходів + 8 fixture + 126 regression checks**, Unity exit=0. [Зведена перевірка та SHA-256 доказів](../Logs/AI-NPC/P4-06-bootstrap-verification-20261124.json) підтверджує відповідність усіх семи C#-джерел фінального smoke поточному checkout та незмінність runtime-коду між повним 898-run і фінальним fixture. Optimization steps=0; ручних дій користувача для цього зрізу не потрібно.
