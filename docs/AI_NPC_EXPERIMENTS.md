# Project S · ML-експерименти The Copy

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
