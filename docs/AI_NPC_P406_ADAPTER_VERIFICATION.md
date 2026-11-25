# P4-06 · Підготовка навчального адаптера, без PPO

2026-11-23. **Перевірений частковий результат P4-06; задача In Progress.** Реалізовано однокроковий ML-Agents adapter із погодженим reward та reset. Оптимізатор, Python-тренер і навчальні запуски не запускалися. Чинний production `CopyDecisionAgent` залишається InferenceOnly; новий компонент не додано до production-сцен або prefab-ів.

## Результат і критерії цього зрізу

- [CopyAttentionReward](../Assets/Scripts/AI/TheCopy/CopyAttentionReward.cs) — C#-реалізація `the-copy.expert-attention/1`, а не вибір дії для NPC. Invalid snapshot/mask повертає false; invalid/unavailable action на валідному кадрі дає −1. Пам’ять має пріоритет над live, freshness не множиться двічі, player floor=0.01. Порядок 50 ознак, сім дій і маски 5/6 незмінні.
- [CopyAttentionTrainingAgent](../Assets/Scripts/AI/TheCopy/CopyAttentionTrainingAgent.cs) — **окремий** opt-in actor, який використовує реальний `CopyDecisionInputV2`. Співіснування з production CopyDecisionAgent та автоматичним DecisionRequester на тому самому actor відхиляється. Відхиляються Model, зайві Sensor/Actuator components, неправильний Behavior/shape/MaxStep. Для реального навчального режиму потрібні spawned server-owned non-player actor, BehaviorType.Default і вже активний ML-Agents communicator; за відсутності Python немає heuristic fallback.
- [P406TrainingVerification](../Assets/Tests/AI/P406TrainingVerification.cs) — generated training-prefab, зареєстрований у Host/Client до їх запуску. Окрема клієнтська репліка не ініціалізує training-Agent і не активується. Перевіряються unspawned/client-owned/configuration guards, 50 входів, маски, AddReward/EndEpisode, скидання пам’яті, duplicate callback, cancellation, stale та втрата authority.
- [Експортер Python-еталонів](../tools/export_ai_npc_reward_parity.py) спершу перевіряє frozen P4-05 replay, потім формує scalar references для всіх 7 дій на 674 фактичних train/ID-знімках. Це тест арифметики C#/Python, не оцінювання політики й не новий синтетичний observation dataset. OOD-вектори не читаються.

Критерії цього кроку: збіг C#/Python reward з похибкою ≤1e-6, один reward на один callback/епізод, відсутність перенесення reward і звукової пам’яті в наступний випадок, незмінна маска/вектор підготовленого запиту та відсутність AddReward при технічному interruption. Тести не повинні вимагати PPO чи встановлення пакетів.

## Життєвий цикл API

1. Координатор створює окремий training-prefab із CopyDecisionInputV2, потрібними сенсорами та BehaviorParameters: TheCopyDraft2, 50 inputs, stacking=1, discrete [7], continuous=0, MaxStep=0. Реальний режим — Default, без Model; сам training-Agent початково не активується.
2. Після серверного spawn викликається `TryActivateTraining()`. Без активного communicator він повертає false. Активація не готує світ і не запускає PPO сама.
3. `OnEpisodeBegin` очищає pending-вектор/маску та власну CopyHearing memory через її lifecycle. Координатор після цього застосовує наступний frozen рецепт: позиції, світло, звук, затримку пам’яті. Геометрію й інші механізми training-Agent сам не скидає.
4. `TryBeginPreparedEpisode()` захоплює 50 входів і кандидатів один раз, перевіряє reward-frame і викликає RequestDecision. Другий pending-запит відхиляється. CollectObservations і WriteDiscreteActionMask використовують цей кадр.
5. На коректному callback: AddReward → збереження cumulative reward для діагностики → EndEpisode → новий OnEpisodeBegin. Invalid/unavailable вибір отримує −1; виконання руху або взаємодії немає. RewardedEpisodes рахує лише такі завершення; повторний callback після завершення ігнорується.
6. Cancel, втрата authority/configuration чи вік >0.5 scaled с: `InterruptPending`, EpisodeInterrupted, nullable LastAssignedReward=null; AddReward не викликається. Повторне скасування не створює другого interruption. DeactivateTraining припиняє запити.

`EpisodeFinished` викликається під час завершення; повторна підготовка всередині цього callback блокується захистом від reentrancy. Координатор має підготувати наступний випадок на наступній ітерації, після завершення reset. Якщо Academy reset надійшов посеред запиту, він обліковується окремо, без вигаданого reward.

**Важлива межа:** «AddReward не викликано» не означає, що Python-тренер автоматично відкинув transition. ML-Agents EpisodeInterrupted має власну bootstrap-семантику. Поки справжній communicator/coordinator не перевірені, не вважати облік технічних відмов у PPO завершеним; майбутній coordinator має обліковувати такі випадки й припиняти/позначати невалідним дослідницький run, а не мовчки навчатися на зіпсованих епізодах. Це одна з причин, чому P4-06 ще не Done.

## Як саме перевірено без навчання

Тільки в тестовому actor використано **явний** `TryActivateTraining(controlledHeuristicTest: true)` з HeuristicOnly. У цьому режимі контрольні ActionBuffers передаються тестом, а дозволений fixture heuristic — Continue. Це API/lifecycle-тест, не NN-рішення, не baseline політики та не production fallback. Default без Python окремо перевірено на відмову. Попередні runtime-inference тести в тому самому suite все ще виконують справжній forward ненавченої smoke-NN; не змішувати їх із контрольними training-callback-ами.

**4758/4758 assertions P4-06 PASS** = **4718** порівнянь (674 × 7) + **40** negative/lifecycle/network checks. Додатково **274/274** попередніх diagnostic/regression PASS, Unity exit 0. C# скомпілювався без помилок; відомі package/API warnings і scratch Bee cache warning не є результатами навчання.

- [Успішний Unity-журнал](../Logs/AI-NPC/P4-06-adapter-final-20261123.log).
- [Збережений знімок журналу](../Logs/AI-NPC/P4-06-adapter-verified-evidence-20261123/unity-log.txt).
- [Версії, counts і хеші фактично виконаного коду](../Logs/AI-NPC/P4-06-adapter-verified-evidence-20261123/verification.json).
- [Python references і hashes джерел](../Logs/AI-NPC/P4-06-adapter-verified-evidence-20261123/reward-parity.json).
- **99/99 Python PASS** на Python 3.13.7 та 3.10.12: [системні тести](../Logs/AI-NPC/P4-06-python-tests-20261123.log), [ML Python](../Logs/AI-NPC/P4-06-python-tests-py310-20261123.log). Дві нові перевірки exporter-а відхиляють unsafe output та відсутні audited sources без запису вигаданих references.

Перші ітерації fixture падали через runtime-generated NetworkObject без prefab hash, реєстрацію після старту ForceSamePrefabs, ownership transfer невидимому client та припущення про активність inactive-prefab репліки. Стенд виправлено: generated prefab до Play, реєстрація обох NetworkManager до Start, observers і явна активація client replica для перевірки guard. Невдалі журнали з префіксами P4-06-adapter / prefab / registered / networked не зараховуються в PASS; production-мережевий код для цього не змінювався.

## Відтворення

```powershell
python -B -m unittest discover -s tools/tests -v
./tools/Verify-P305.ps1 -UseWebSockets -VerifyTrainingAdapter -RewardPlan Logs/AI-NPC/P4-05-recipe-plan-20261123 -RewardCapture Logs/AI-NPC/P4-05-replay-evidence-20261123 -MlPython 'D:/PROGS/anaconda3/envs/project-s-ml/python.exe' -KeepWorkspace
```

На іншому комп’ютері потрібні його audited P4-05 план/capture й шлях до готового ML Python. Launcher не встановлює пакети, не запускає trainer і не оптимізує ваги. Новий прапор додає тільки parity export та controlled tests; модель для попередньої inference-регресії лишається untrained smoke із 0 training steps.

У цій сесії Unity запущено безпосередньо у наявній `p305-workspace-445f2aa1` після копіювання змінених скриптів і Python fixture; [helper](../tools/Export-AINpcTrainingEvidence.ps1) перевірено окремо. Cold-copy маршрут нового прапора ще не запускався; PowerShell syntax перевірено. Дані та моделі зберігаються лише в Logs/тимчасових Assets.

## Залишок P4-06

**Оновлення після цього adapter-зрізу:** coordinator завершив 449 train-випадків і перевірки зупинки після capture/technical failure; Python communicator зіставив 8 diagnostic terminal transitions без optimizer. Потім 2026-11-24 додано repeat/shuffle bootstrap: 898 train та 16 diagnostic переходів, metadata й abort-сигнал Python. [Актуальні докази та залишок](AI_NPC_P406_BOOTSTRAP_VERIFICATION.md). Числа вище описують окремий попередній adapter-run. Для фактичного PPO ще потрібен supervisor trainer/Unity з preflight, бюджетом/незалежними seeds та відбракуванням невалідного run, а потім окреме погодження запусків і бюджету. Optimization steps=0, trainer created=false, OOD vectors read=false, policy quality=not_evaluated. Нових навчених моделей і результатів ID/OOD немає.
