# Project S · ML-експерименти The Copy

## 2026-11-24 · P4-06, перший погоджений PPO-пілот · завершено й перевірено

Після явного «та, продовжуй» виконано --execute-ppo для підготовленого spec. [Погодження та UTC-мітка](../Logs/AI-NPC/P4-06-ppo-pilot-review-20261124/user-approval.json). Run ID **TheCopy-10398d97ac25**, policy seed=11, shuffle seed=7011, 20 × 449 train-рецептів = 8980 одноетапних рішень, runtime limit=3600 с. PPO/MLP 2×128, buffer=8192, batch=256, та сама погоджена reward-схема; ID/OOD не використовуються. Параметри не змінювалися після старту.

**8980/8980 captured decisions**, усі 20 проходів; trainer та checkpoint global_step=8980, supervisor valid_run=true, exit=[0,0]. Adam має **96 optimizer steps у кожному з 12 parameter states**, скінченні й ненульові moments; policy tensors finite. ONNX checker і сигнатура 50/[7] PASS. Це фактична оптимізація нової sensory-intent-v1 NN, не untrained smoke. Runtime **2243,3 с**. [Artifact inspection](../Logs/AI-NPC/P4-06-ppo-pilot-review-20261124/artifact-inspection.json), [зведені докази](../Logs/AI-NPC/P4-06-ppo-pilot-review-20261124/pilot-evidence.json).

ONNX SHA-256 `02a522c98d60cc3dd8f34411e485d6eb8e1ac90cad623f52316717f65256bb12`; checkpoint SHA-256 `3cfec9ca1e88deb73b9589c949b71dd601a4a3306f5cd0a3b6d57462e99aa36b`. Артефакти лише в ignored Logs/trainer, у production модель не підключалася. 17 sealed source/config files архівовані. **136/136 Python PASS** на двох версіях.

SDK при плановому закритті Unity вивів restart/shutdown diagnostics. Логи збережені, фінальні steps/captures/модель перевірені; після завершення прямих SDK/Unity descendants не залишилося. Це обмеження для наступного shutdown-зрізу. [Worker log](../Logs/AI-NPC/P4-06-ppo-pilot-review-20261124/worker.log). Train mean reward=0.535 на step=5000 не є оцінкою фінального checkpoint на ID/OOD. ID/OOD quality та Unity-inference не виконувалися; P4-06 In Progress, один технічний пілот не замінює кілька незалежних експериментів. [Повний звіт](AI_NPC_P406_PILOT_VERIFICATION.md)

## 2026-11-24 · P4-06, supervisor та SDK preflight, без навчання

Перевірено керування окремими Unity/API-worker процесами: **16/16 diagnostic-переходів**, policy seed=11 / shuffle=7011, два проходи по восьми перших train-рецептах. **8/8 fixture + 126/126 regression checks**, Unity/worker exit=0, owned_processes_stopped=true; C# records та Python terminal frames/masks/actions/reward збігаються. [Result](../Logs/AI-NPC/P4-06-supervisor-positive-verified-20261124/result.json), [capture audit](../Logs/AI-NPC/P4-06-supervisor-positive-verified-20261124/capture-audit.json).

Окремі negative runs: injected_bootstrap_failure дав valid_run=false, prepared=1 / recorded=0 та зупинив обидва процеси; 10-секундний runtime timeout дав TimeoutError/exit=1 і зупинив обидва процеси. Перший fault-run мав Windows permission race сигналу й не зарахований як очікуваний injected failure; shared-reader і bounded retries виправлені, конкурентні atomic replacements перевірені. [Abort](../Logs/AI-NPC/P4-06-supervisor-abort-verified-20261124/result.json), [timeout](../Logs/AI-NPC/P4-06-supervisor-timeout-20261124/result.json).

**132/132 Python PASS** на 3.13.7 та 3.10.12. Справжній ML-Agents 1.1.0 прийняв proposal config через SDK parser, без TrainerController/optimizer; approval gate без --execute-ppo відмовив до run-процесів. **Запропонований, але не погоджений пілот:** policy seed=11, shuffle=7011, 20 train-проходів / 8980 рішень, runtime limit=3600 с; [готовий spec](../Logs/AI-NPC/P4-06-ppo-pilot-review-20261124/prepared.json), [SDK preflight](../Logs/AI-NPC/P4-06-ppo-pilot-review-20261124/sdk-preflight.json).

Нових optimization steps=0, trainer created=false, нових checkpoints/навчених ONNX і ID/OOD policy-метрик немає; OOD-вектори не читалися. Реальний PPO-worker path ще не перевірено оптимізацією/експортом. Пілот не замінює кілька незалежних експериментів. [Повний звіт і наступна дія для погодження](AI_NPC_P406_SUPERVISOR_VERIFICATION.md)

## 2026-11-24 · P4-06, repeat/shuffle bootstrap, без навчання

Через справжній ML-Agents communicator виконано **898/898 одноетапних переходів**, два проходи по всіх 449 frozen train-рецептах. API policy seed=22, shuffle seed=7022, generation seed=1001. Окремо **16/16 diagnostic** (два проходи по восьми перших рецептах родин), seed=11 / shuffle=7011. Це технічні uniform allowed random клієнти, не PPO-training seeds/експерименти та не вимір NN-якості.

В обох прогонах **8/8 C# bootstrap checks + 126/126 regression checks**, Unity exit 0; terminal frames/masks/actions/reward зіставлені з records, порядок epoch/покриття/seeds/schedule hash перевірено, off-manifest rewarded episodes=0. Початковий reset terminal-only; попередній warmup із додатковим rewarded episode не є фінальним bootstrap. [Повний результат із 898 terminal transitions](../Logs/AI-NPC/P4-06-bootstrap-full-final-20261124/result.json), [launch metadata і hashes](../Logs/AI-NPC/P4-06-bootstrap-full-final-20261124/launch.json), [offline audit](../Logs/AI-NPC/P4-06-bootstrap-full-final-20261124/audit.json).

**117/117 Python PASS** на 3.13.7 та 3.10.12; CLI відхиляє відсутній або змінений розклад. Перший full-run Aborted через sharing violation активного JSONL, prepared=425 / recorded=424, valid_run=false, Python exit=1; це не PASS. Після усунення діагностичного читання зібрано повні 898 заново. Optimization steps=0, trainer created=false, OOD vectors read=false; нових моделей/checkpoints чи ID/OOD policy-метрик немає. [Повний звіт, команди й наступна задача](AI_NPC_P406_BOOTSTRAP_VERIFICATION.md)

> Доповнення 2026-11-24 до bootstrap-зрізу вище: explicit technical failure з injected_bootstrap_failure відбракована з child exit=1, prepared=1 / recorded=0, valid_run=false, rewarded terminals=0. [Abort audit](../Logs/AI-NPC/P4-06-bootstrap-abort-final-20261124/abort-audit.json). Виправлено тільки порядок тестових callback-ів; runtime після повного 898-run не змінювався.

## 2026-11-23 · P4-06, coordinator та реальний communicator, без optimizer

**Це технічні API-тести, не навчання/порівняння політик.** Frozen train generation seed=1001; controlled policy seed=0, Python smoke seed=2026. CopyAttentionRecipeCoordinator виконав 449/449 train через реальні сенсори з одним recorded reward на decision. Окремі technical/capture failure зупинили run з valid_run=false, recorded=0. **12/12 coordinator + 4758/4758 adapter + 274/274 regression assertions PASS**, Unity exit 0. [449 captures, abort summaries та hashes](../Logs/AI-NPC/P4-06-coordinator-evidence-20261123/provenance.json).

Python 3.10.12 / mlagents_envs 1.1.0 обмінявся з Unity 6000.6.5f1 **8/8 diagnostic terminal transitions**: 50 observations, гілка [7], незмінні mask/action/reward із точністю 1e-6, Unity exit 0. Політика тесту — uniform allowed random, не NN. Початковий reset і фінальний terminal враховано; Python env закривається перед очікуванням Unity exit. [Python result/terminal records](../Logs/AI-NPC/P4-06-communicator-final-20261123/result.json), [окремо збережені C# captures](../Logs/AI-NPC/P4-06-communicator-evidence-20261123/provenance.json).

**109/109 Python unit-тестів PASS** на 3.13.7 та 3.10.12. Optimization steps=0, trainer created=false, OOD vectors read=false, policy quality=not_assessed. Нових checkpoints/навчених моделей чи ID/OOD policy-метрик немає. Перший full coordinator-run зупинився через client collider replicas у спільній PhysicsScene, перший communicator-run — через shutdown deadlock; обидва невдалі, не PASS. Після виправлень зібрано окремі повні успішні прогони. [Реалізація, журнали й наступний bootstrap](AI_NPC_P406_COORDINATOR_VERIFICATION.md)

## 2026-11-23 · P4-06, C#/Python parity і контрольований training API, без PPO

Підготовлено C# CopyAttentionReward та окремий CopyAttentionTrainingAgent для captured sensor input → scalar reward → EndEpisode/reset. На 674 audited train/ID кадрах для всіх 7 дій перевірено **4718** збігів C# із Python з похибкою ≤1e-6. [Python references та provenance](../Logs/AI-NPC/P4-06-adapter-verified-evidence-20261123/reward-parity.json). Ці числа перевіряють формулу, а не якість NN.

У controlled HeuristicOnly fixture перевірено одноразове призначення reward, reset реальної sound memory, маски, client/server guards, duplicate callback, технічні cancel/stale/authority loss. Повторні запити з terminal callback блокуються. Це ручні API-callback-и, не відповіді навченої моделі. **99/99 Python PASS** на 3.13.7 та 3.10.12: [системні тести](../Logs/AI-NPC/P4-06-python-tests-20261123.log), [ML Python](../Logs/AI-NPC/P4-06-python-tests-py310-20261123.log). [Повний звіт Unity і межі](AI_NPC_P406_ADAPTER_VERIFICATION.md)

Нових optimization steps **0**, connected trainer tested=false, OOD vectors read=false, policy quality=not_evaluated. Нових PPO-runs, checkpoint, навчених моделей чи фактичних ID/OOD policy-метрик немає. У цьому ж suite стара inference-регресія окремо виконує untrained smoke-NN; її не змішувати з контрольованими training actions. Потрібні recipe-coordinator, communicator smoke й окреме погодження фактичного навчання.

## 2026-11-23 · P4-05, frozen рецепти та sensor-only replay, без навчання

За погодженим дизайном створено 899 рецептів: 449 train / 225 ID / 225 OOD, generation seeds 1001/2001/3001, 8 родин. [Generation audit](../Logs/AI-NPC/P4-05-recipe-plan-20261123/generation-audit.json) містить hashes рецептів/маніфестів; active duplicates=0, повтори quiet явно задекларовані. [Фактичний sensor replay](../Logs/AI-NPC/P4-05-replay-evidence-20261123/sensor-audit.json) зібрав усі 674 train/ID: active vector duplicates=0 при округленні 6 знаків, OOD vectors read=false, model requests=0, training steps=0.

Unity exit 0: **3758 replay assertions + 274 diagnostic/regression PASS**; Python **97/97 PASS** на 3.13.7 та 3.10.12. [Фінальний Unity-журнал](../Logs/AI-NPC/P4-05-replay-locked-20261123.log), [системні тести](../Logs/AI-NPC/P4-05-python-tests-20261123.log), [ML Python](../Logs/AI-NPC/P4-05-python-tests-py310-20261123.log). Два попередні replay не завершилися через deferred domain reload й не є PASS; успішний збір відокремлений від неповних captures.

Окремі 8 diagnostic-forward виконані ненавченою smoke-моделлю з 0 training steps (hash у [provenance](../Logs/AI-NPC/P4-05-diagnostic-evidence-locked-20261123/provenance.json)); вони не входять у train/ID/OOD. [Їхній звіт](../Logs/AI-NPC/P4-05-diagnostic-evidence-locked-20261123/metrics.json) має diagnostic gate, не quality PASS. Ці числа перевіряють підключення реальних сенсорів/запиту/evaluator, не є результатом навченої політики. Нових навчальних запусків, checkpoints, моделей чи реальних ID/OOD policy-метрик немає. [Деталі та відтворення](AI_NPC_P405_VERIFICATION.md)

## 2026-11-23 · P4-04, протокол і тести evaluator-а, без ML-запуску

Користувач погодив epsilon=0.05, основну macro accepted-near-optimal ID≥90% / OOD≥80%, повноту маніфесту й accepted-invalid=0 для підготовки P4-05, без навчання. Додано versioned JSON і offline-evaluator із manifest/coverage, умовним reward/regret, paired baseline, micro/macro та gate на переданих даних. [Протокол](AI_NPC_P404_EVALUATION_PROTOCOL.md)

**25 нових + 61 попередній = 86/86 Python PASS** на 3.13.7 та 3.10.12: [журнал](../Logs/AI-NPC/P4-04-attention-metrics-tests-20261123.log), [ML Python](../Logs/AI-NPC/P4-04-attention-metrics-tests-py310-20261123.log). Unit-fixtures із позначками ID/OOD перевіряють код порогів, не є реальними ID/OOD даними політики. Окремий [синтетичний CLI-звіт](../Logs/AI-NPC/P4-04-attention-synthetic-report-20261123.json): 2 оцінювані вибори, stale і missing; умовний reward 0.625 проти accepted-near-optimal 0.25 від усіх 4 запланованих. Це контроль знаменників, не вимір NN.

Нових моделей, запусків навчання, checkpoint або NN-метрик немає; нових кроків **0**. Unity не запускали, код Agent/сцен/пакетів не змінено. Фактичні випадки й runner — P4-05. Нижче — історія попередніх зрізів.

## 2026-11-19 · Погоджений expert-attention v1, без ML-запуску

За явним погодженням користувача реалізовано [профіль](../ai_training/config/sensory_intent_v1_expert_attention_reward.json) `the-copy.expert-attention/1`, offline-обчислення reward і baseline uniform-allowed / first-allowed-sensory / argmax-expert. Це surrogate для однокрокового sensory-intent-v1, не reward фізичних наслідків. Argmax має reward=1 за побудовою; це математична властивість, не експериментальна перевага NN.

**20 нових + 41 попередній = 61/61 PASS** на Python 3.13.7 та 3.10.12; синтетичні unit-тести формул/масок, не train/ID/OOD набір. [Системний журнал](../Logs/AI-NPC/P4-04-expert-reward-tests-20261119.log), [ML Python](../Logs/AI-NPC/P4-04-expert-reward-tests-py310-20261119.log), [обсяг і відтворення](AI_NPC_P404_REWARD.md).

Фактичних нових кроків навчання **0**; нових моделей, run ID, NN-метрик чи оцінок ID/OOD немає. Agent/PPO й старі trace-метрики не змінено; компіляцію Unity повторно не запускали. Потрібні протокол і пороги до оцінювання NN, потім runner/дослідницькі випадки. Попередні записи нижче збережені як історія до погодження reward.

Оновлено 2026-11-19. Історичні результати отримано в окремому стенді. Шляхи під `D:/UnityML/` і в профілі Windows вказують на локальні артефакти поза командним репозиторієм.

## 2026-11-19 · P4-04, перевірка evaluator-а без експерименту політики

**Аналіз постановки, без ML-запуску:** математична діагностика неприйнятого reward «будь-який доступний сенсорний намір» на 16 структурних масках дає оптимальне просте правило (оцінка 1 у всіх масках) та очікування uniform-allowed 0,675 за однакової ваги масок. Це не датасет гри, не результат політики й не reward проєкту. Використано лише для пояснення, чому потрібно обговорити навчальну мету й baseline до навчання. [Діагностика](../Logs/AI-NPC/P4-04-objective-sanity-20261119.json), [неприйнята пропозиція](AI_NPC_P404_OBJECTIVE_PROPOSAL.md)

**Подальший технічний producer-зріз:** 186/186 Unity та 41/41 Python PASS. Три фактичні CPU-forward поточної ненавченої sensory-intent-v1 smoke-моделі записані в JSONL і прочитані evaluator-ом: 3 accepted, 3 latency samples, 0 прийнятих порушень, 0 еталонів, accuracy=null. Сценарій контрольований, доступний лише Continue; це не вимір якості/продуктивності політики. Штучні callback/cancel та NGO-despawn — окремі файли/policy_id. Нових моделей, reward, навчання або ID/OOD немає. [Докази](AI_NPC_P404_TRACE_VERIFICATION.md). Нижче збережено початковий стан до підключення recorder-а.

Користувач відклав reward до окремого обговорення. Підготовлено метрики й 25 нових unit-тестів; разом із попередніми 15 — **40/40 PASS** на Python 3.13.7 і 3.10.12. CLI перевірено на трьох синтетичних записах та некоректному JSONL. [Документ і докази](AI_NPC_P404_METRICS.md)

Синтетичні case_id/policy_id та їхні числа не є вимірами NN/гри. Реальні записи, producer, пороги якості, reward, еталони дослідницького набору й baseline ще не визначені/підключені. Навчання не запускалося, нових моделей або ID/OOD-результатів немає.

## 2026-11-19 · Підготовка draft-2, без ML-запуску

**Зафіксований перший сценарій:** після вибору користувача використовується sensory-intent-v1 — незалежний однокроковий вибір 0–4 за реальними сенсорами, без руху. Порядок 50 ознак не змінений, SchemaId/metadata оновлені. Новий smoke-експорт seed 11, 0 training steps, SHA-256 **3a81050958dd5becc3525ad780a26e9900ca659f0f05f8b0e4f95f6a1cf2295a**. Окрема негативна нейромережа на 27 входів використана лише для перевірки відмови Agent до ініціалізації. Unity 164/164, Python 15/15 PASS. Reward, навчальні запуски й метрики політики ще відсутні; попередні експериментальні пороги не перенесені автоматично. [Контракт і докази](AI_NPC_SENSORY_INTENT_V1.md)

**Погоджений технічний inference-зріз.** Після підключення ML-Agents 4.1.0 / Inference 2.6.1 експортовано окрему **ненавчену** TorchPolicy/SimpleActor: seed 11, CPU, два шари 128, 50 входів, гілка [7], training_steps=0. SHA-256 **cdcd1d64c3bf05717394b7804bda70e3e4010585512c3a7da2b0a085d53b07e5**. Метадані purpose=UNTRAINED_SMOKE_TEST_ONLY; файл лише Logs і Assets тимчасових копій. Unity CPU-forward через InferenceOnly пройшов; Agent 23/23, загальний мережевий fixture 149/149, код 0. Це перевірка з’єднання, без оптимізатора, reward, train/ID/OOD або оцінки політики. [Докази та відтворення](AI_NPC_AGENT_INFERENCE_VERIFICATION.md), [метадані моделі](../Logs/AI-NPC/draft2-untrained-smoke.json). Записи нижче описують попередні кроки підготовки, до цього smoke-експорту.

**Подальша read-only перевірка:** RunOptions із встановленого mlagents 1.1.0 прийняв YAML після register_trainer_plugins. ONNX checker прочитав історичний Stage4: obs_0 [batch, 27], action_masks [batch, 7], SHA-256 збігається. 13/13 тестів preflight пройшли. Це не навчання та не inference; нових кроків/моделей/метрик немає. [JSON-доказ](../Logs/AI-NPC/P4-03-ml-preflight-20261119.json), [деталі](AI_NPC_ML_PREFLIGHT.md). Нижче збережено стан початкової підготовки шаблону.

В ai-branch від ec7935d із незакоміченими NPC-змінами реалізовано `the-copy.decision-input/draft-2`: 50 ознак, сім дій, 5/6 закриті до правил пазла. C#-перевірки входів/масок — 33/33 PASS, без оцінювання нейромережі. [Докази та обмеження](AI_NPC_DECISION_INPUT_DRAFT2.md)

Додано [PPO YAML](../ai_training/config/the_copy_draft2_ppo.yaml) і [парний контракт](../ai_training/config/the_copy_draft2_contract.json) для нового Behavior TheCopyDraft2. Гіперпараметри взяті з локального `D:/UnityML/Project_S_Stage4_ML_DecisionMaker/config/ppo_seed11.yaml` лише як шаблон: два шари по 128, max_steps 150000. Фактичних кроків нового навчання — **0**, run ID, seed запуску, checkpoint, нової моделі, reward-метрик та ID/OOD результатів — **немає**. Синтаксис YAML/JSON перевірено; trainer ML-Agents та Unity Agent не підключені, конфігурацію запуском не перевірено. Старий ONNX на 27 входів несумісний; його inference заново не запускався.

## Середовище історичних запусків

У `ai-test` зафіксовано Unity 6000.6.5f1, ML-Agents 4.1.0 та Inference Engine 2.6.1. [Контекст](AI_NPC_CONTEXT.md) фіксує Python 3.10.12, `mlagents==1.1.0` і PyTorch 2.2.1+cu121. Є експорт 3D Ball на кроці 195 831 у `D:/UnityML/ml-agents/results/projectS-3DBall-01/`. Inference 3D Ball зазначений у контексті як історична перевірка. У [manifest командної гри](../Packages/manifest.json) ML-Agents відсутній.

## Stage 2 · PPO проти random

- **Контракт.** Три входи `Sound`, `Light`, `Security` і три дії `Continue`, `Investigate`, `Observe`.
- **Конфігурація й модель.** `D:/UnityML/Project_S_Stage2_POC/config/stage2_ppo.yaml`. Експорт `ProjectSStage2.onnx` на кроці **40 001** у `D:/UnityML/ml-agents/results/projectS-stage2-v1/`.
- **Виміряно.** 500 контрольних випадків на політику. Середня винагорода **0,51628** для ML і **0,19630** для random, різниця **+0,31998**. Частка оптимальних рішень **79,8%** проти **33,4%**. У `QuietNormal` частка оптимальних рішень **21%** проти **26%**.
- **Докази.** `D:/UnityML/Project_S_Stage2_POC/stage2_results/report.md` та `stage2_eval_model.csv` і `stage2_eval_random.csv` у `%USERPROFILE%/AppData/LocalLow/Unity Technologies/UnityEnvironment/`. Результат стосується числової POC-симуляції.

## Stage 3 · Контроль сенсорів

**10/10 PASS**, 17 ознак, Unity 6000.6.5f1. Доказ — `stage3_test_report.json` у тій самій папці `UnityEnvironment`. Це перевірка Perception і Memory в прототипі. Результати поточних мережевих тестів слуху записані в [прогресі P3-05](AI_NPC_PROGRESS.md).

## Stage 4 · PPO та експериментальний ONNX

- **Контракт і конфігурація.** 27 ознак, 7 дискретних рішень із масками, два прихованих шари по 128. Заплановано по 150 000 кроків для seed 11/22/33. YAML лежать у `D:/UnityML/Project_S_Stage4_ML_DecisionMaker/config/`. Фактична конфігурація seed 11 збережена як `configuration.yaml` у каталозі запуску нижче.
- **Підтверджений запуск.** `D:/UnityML/ml-agents/results/projectS-stage4-s11-retry/`. Один `run_logs/training_status.json` містить чекпойнти **3 016** і **64 981**. Експорт `ProjectSStage4.onnx` відповідає **64 981** кроку. SHA-256 `287259e973cf5216c5c29f284e65cc7dcb497a393f81e81cfb2ea5659eec02eb`. Ранній чекпойнт сам по собі не встановлює причину переривання.
- **Статус.** У перевірених артефактах відсутні завершені запуски seed 22/33 та результати ID/OOD Stage 4. Історичний CSV плану запусків має `NOT_STARTED`, тому факт експорту підтверджує журнал запуску. У збереженій сцені `ai-test` модель не призначена. Inference і якість цієї політики в командній грі ще потребують перевірки.

## Що записувати для наступного запуску

Запиши дату й run ID, гілку та коміт або стан робочого дерева, версії Unity і пакетів, версію контракту, фактичний YAML і seed. Додай кількість кроків, шлях і хеш моделі, сценарії та розбиття, пороги, результати й шляхи до логів та CSV. Фіксуй критерії до оцінювання, підбирай модель за ID і використовуй відкладений OOD для підсумкової оцінки. Тривалі запуски потребують попереднього погодження.
