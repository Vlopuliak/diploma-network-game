# P4-06 · Supervisor trainer/Unity та пропозиція PPO-пілота

**Оновлення після підготовчого зрізу:** користувач погодив цей пілот; --execute-ppo запущено 2026-11-24. [Фактичний запуск і стан його перевірки](AI_NPC_P406_PILOT_VERIFICATION.md). Твердження нижче про відсутність погодження/навчання описують момент підготовки supervisor-а, не актуальний runtime стан.

Дата: 2026-11-24. **Перевірений частковий результат, P4-06 In Progress.** Продовження [bootstrap](AI_NPC_P406_BOOTSTRAP_VERIFICATION.md). Цей зріз готує керування процесами й конфігурацією; фактичне PPO-навчання ще не погоджене й не запускалося.

## Реалізація та критерії

- [ai_npc_training_supervisor.py](../tools/ai_npc_training_supervisor.py) має окремі prepare / preflight / run. Prepare створює новий каталог у repository Logs/AI-NPC, frozen schedule, ppo.yaml, prepared.json і checksum. Уже наявний каталог не перезаписується. Worker/Unity запускаються окремими Popen без shell; власні PID записуються до owned-processes.json.
- Spec містить явні epochs, policy/shuffle seeds, кількість рішень, wall-clock timeout, port, output та artifact paths. SHA-256 фіксують schedule/config, вихідний PPO-шаблон, JSON-контракт, чотири Python-джерела й одинадцять Unity/package/version файлів. Перед виконанням звіряються workspace і checkout. Змінений spec/config/source/tool вимагає нового prepare. Checksum захищає від випадкової зміни, не є підписом або дозволом користувача.
- Preflight запускає лише парсер справжнього ML-Agents 1.1.0 у готовому ML Python, перевіряє TheCopyDraft2 / PPO, бюджет, server input contract 50 / [7], один Editor environment, відсутність resume/force/initialize-from та SDK restart. Trainer, нейромережа й optimizer у preflight не створюються. Версії SDK/PyTorch/numpy/protobuf записуються до sdk-preflight.json.
- [ai_npc_training_worker.py](../tools/ai_npc_training_worker.py) у режимі api-smoke обмінюється з Unity через mlagents_envs: uniform allowed random, реальні 50 ознак, маски 5/6, одноетапні terminal rewards. Цей клієнт не замінює ігровий Decision Maker. Worker записує terminal transitions; supervisor зіставляє їх із C# records та проводить manifest/schedule/reward audit після завершення процесів.
- У режимі ppo worker підготовлений для офіційних parse_command_line / run_cli з установленого SDK. Після завершення вимагає final training status із точною кількістю кроків, ONNX і PyTorch checkpoint у власному trainer/run_id. **Цей шлях реальної оптимізації ще не виконувався.** Його наявність у коді не є доказом успішного PPO чи якості NN.
- Supervisor перевіряє Aborted раніше, ніж exit codes. Потрібні Completed, два exit=0, повний capture та успішний worker result. Timeout, child failure чи Aborted роблять весь run valid_run=false, без автоматичного restart/resume. Навіть створені trainer-артефакти такого run не можна вибирати/відновлювати; вони лишаються для розбору, автоматично не видаляються.
- У finally завершуються тільки власні Popen PID; Windows taskkill /PID /T /F прибирає їхні дочірні процеси. Це важливо для SDK environment subprocess. Часткові captures копіюються після зупинки в partial-capture, якщо вони встигли утворитися. Новий запуск потребує нового каталогу.
- [P406CoordinatorVerification](../Assets/Tests/AI/P406CoordinatorVerification.cs) отримав лише окрему назву completion для external trainer та короткий bounded retry атомарної заміни signal-файлу. Runtime NPC, production-сцени, shared-компоненти й пакети цього зрізу не змінено.

Критерії цього кроку: справжній SDK приймає frozen config; API-сеанс проходить 16 реальних випадків; Aborted і timeout припиняють обидва процеси; відсутність дозволу не запускає PPO; метадані й докази збережені. Критерії якості ID/OOD цим кроком не оцінюються.

## Windows signal-файл

Перший supervisor fault-run був невалідний через Permission denied при конкурентній заміні signal.json. Звичайний Python open не надає FILE_SHARE_DELETE, тому конфліктує з Unity File.Replace. [read_run_signal](../ai_training/cases/training_schedule.py) на Windows відкриває файл через CreateFileW із share read/write/delete; тимчасова відмова повторюється до 25 разів із паузою 2 мс. Writer fixture також має bounded retry; постійна помилка не перетворюється на успіх.

Додано тести конкурентних 100 atomic replacements, повтору тимчасової відмови й відхилення постійної. Перший невдалий run збережено окремо, не зараховано як підтвердження очікуваної injected failure. Ігрові події NPC та мережеві RPC не змінювалися.

## Перевірки та докази

- **132/132 Python PASS** на Python 3.13.7 та ML Python 3.10.12. Нові перевірки охоплюють immutable spec/config/source, PPO gate, одноразовість run, відповідність worker frame/action/reward, negative child exits/completion, timeout, завершення реальних дочірніх процесів та Windows grandchild. [Python 3.13](../Logs/AI-NPC/P4-06-supervisor-python-20261124.log), [Python 3.10](../Logs/AI-NPC/P4-06-supervisor-python310-20261124.log).
- **Позитивний supervisor API-сеанс: 16/16 переходів**, два diagnostic-проходи, seed=11 / shuffle=7011. Worker та Unity exit=0; captures/reward/порядок збігаються, owned_processes_stopped=true. [Result](../Logs/AI-NPC/P4-06-supervisor-positive-verified-20261124/result.json), [manifest/reward audit](../Logs/AI-NPC/P4-06-supervisor-positive-verified-20261124/capture-audit.json), [spec і source hashes](../Logs/AI-NPC/P4-06-supervisor-positive-verified-20261124/prepared.json).
- **Injected failure відбракована:** supervisor exit=1, prepared=1 / recorded=0, valid_run=false, причина technical_interruption:injected_bootstrap_failure, обидва власні процеси зупинені. [Result](../Logs/AI-NPC/P4-06-supervisor-abort-verified-20261124/result.json). Цей run підтвердив Python shared-reader після виправлення; фінальний writer retry додано пізніше й скомпільовано/перевірено у позитивному сеансі. Не називати аварійний run навчальним PASS.
- **PPO gate:** команда run без --execute-ppo відхиляється до preflight/створення run-процесів; started.json / owned-processes.json / trainer не з’являються. CLI-прапор є захистом від випадкового запуску, а не заміною погодження користувача за AGENTS.md. Фінальна пропозиція нижче залишається не запущеною.
- **Жорсткий timeout підтверджено на реальних Unity/API-worker:** окремий run із 10-секундним runtime budget завершився очікуваним exit=1 / TimeoutError, valid_run=false, owned_processes_stopped=true. [Result](../Logs/AI-NPC/P4-06-supervisor-timeout-20261124/result.json). Це перевірка зупинки, не незавершений успішний сценарій.
- Позитивний сеанс включав **8/8 C# fixture + 126/126 regression checks**, Unity exit=0; компіляція зміненого fixture без C# errors. Попередні повні 898 переходів/4758 parity assertions цього разу не повторювалися й залишаються окремими історичними доказами.

Логи й captures лежать у ignored Logs за межами disposable workspace. Вихідні frozen train-рецепти не переписані; ID/OOD-вектори не читалися. Нових optimization steps=0, trainer created=false, нових checkpoints/навчених ONNX та ID/OOD policy-результатів немає. Окремий попередній 898-run є доказом bootstrap повторень, а не результатом цього supervisor-сеансу чи навчання.

[Зведена перевірка й SHA-256 доказів](../Logs/AI-NPC/P4-06-supervisor-verification-20261124.json) підтвердила positive/abort/timeout, відповідність фінального позитивного spec і PPO-пропозиції поточним джерелам, відсутність started/trainer у PPO-каталозі. [Фінальний негативний CLI gate](../Logs/AI-NPC/P4-06-supervisor-ppo-review-gate-20261124.log) відмовив без --execute-ppo. Git: ai-branch, HEAD 10a5688; попередні незакомічені зміни збережено, нових комітів/push/merge немає.

## Відтворення API-перевірки

Наявна disposable copy має містити актуальні NPC-джерела; Unity не повинна вже працювати з нею. Для нового workspace спочатку використати наявний launcher із KeepWorkspace та актуальними джерелами, потім вказати його train-only P406CoordinatorPlan. Output має бути новим.

```powershell
& 'D:/PROGS/anaconda3/envs/project-s-ml/python.exe' -B tools/ai_npc_training_supervisor.py prepare `
  --workspace Logs/AI-NPC/p305-workspace-445f2aa1 `
  --recipe-plan Logs/AI-NPC/p305-workspace-445f2aa1/Logs/AI-NPC/P405RecipePlan `
  --output Logs/AI-NPC/supervisor-repeat `
  --unity 'C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe' `
  --python 'D:/PROGS/anaconda3/envs/project-s-ml/python.exe' `
  --diagnostic --epochs 2 --seed 11 --shuffle-seed 7011 --port 5035 --timeout 240
& 'D:/PROGS/anaconda3/envs/project-s-ml/python.exe' -B tools/ai_npc_training_supervisor.py run `
  --prepared Logs/AI-NPC/supervisor-repeat
```

Для negative test підготувати окремий output із --inject-technical-failure, очікувати exit=1, Aborted та owned_processes_stopped=true. Для timeout — окремий output із --timeout 10; це очікувана невалідна зупинка, не успіх ігрового сценарію. На іншому ПК шляхи Editor/Python/workspace слід замінити. Інструменти не встановлюють пакетів.

## Конкретна пропозиція першого PPO-пілота — потребує погодження

Підготовлено [prepared.json](../Logs/AI-NPC/P4-06-ppo-pilot-review-20261124/prepared.json), [фактичний ppo.yaml](../Logs/AI-NPC/P4-06-ppo-pilot-review-20261124/ppo.yaml), [SDK preflight](../Logs/AI-NPC/P4-06-ppo-pilot-review-20261124/sdk-preflight.json) та [schedule](../Logs/AI-NPC/P4-06-ppo-pilot-review-20261124/schedule.json). **Started/trainer directory відсутні, реальний запуск ще не виконано.**

- Один технічний пілот, **policy seed=11**, shuffle seed=7011, generation seed=1001.
- **20 повних проходів train = 8980 одноетапних рішень**, без руху й дій механізмів; ID/OOD не використовуються.
- PPO/MLP із чинного шаблону: 2 × 128, batch=256, buffer=8192, learning_rate=0.0003, beta=0.005, epsilon=0.2, num_epoch=3, extrinsic gamma=0.99. 8980 перевищує один buffer, щоб пілот міг перевірити оптимізацію, а не лише збір досвіду; результат якості не гарантується.
- Engine time_scale=1, target_frame_rate=60; 50 inputs, [7], 5/6 masked, той самий погоджений expert-attention reward.
- **Жорсткий runtime budget — 3600 с** для запущених Unity/worker. Preflight має окремий ліміт 120 с. При перевищенні — stop/discard, без resume.
- Unity визначає точну кількість 8980 епізодів. Trainer max_steps=8981 є запобіжним cap, щоб він не закрився до останнього terminal; worker після завершення вимагає фактичні trainer steps=8980 та final ONNX/PT.
- Для Editor використовується **port 5004**. У встановленому ML-Agents 1.1.0 learn.run_training для env_path=None передає port=None, а UnityEnvironment обирає DEFAULT_EDITOR_PORT=5004; довільний --base-port у цьому режимі не є підтвердженим маршрутом. Supervisor явно відхиляє інший PPO-port. API-smoke використовує явний port самостійного UnityEnvironment.
- Артефакти — лише власний Logs-каталог, новий run_id, без відновлення старого Stage4. Пілот не закриває P4-06 і не замінює кілька незалежних дослідницьких запусків.

**Лише після окремого погодження користувачем** команда для підготовленої незміненої пропозиції:

```powershell
& 'D:/PROGS/anaconda3/envs/project-s-ml/python.exe' -B tools/ai_npc_training_supervisor.py run `
  --prepared Logs/AI-NPC/P4-06-ppo-pilot-review-20261124 --execute-ppo
```

Якщо джерела/конфігурація змінилися, команда відмовить: спочатку новий prepare і перевірка обсягу. До погодження дозволені лише prepare/preflight/API-smoke. Наступний крок після погодження — виконати цей пілот, перевірити фактичні trainer steps/checkpoints/ONNX, зафіксувати всі помилки й результати; quality gate ID/OOD та production inference залишаються подальшими задачами.
