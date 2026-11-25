# P4-06 · Перший погоджений PPO-пілот

Дата: 2026-11-24. **Погоджений технічний пілот завершено, артефакти перевірено.** P4-06 загалом In Progress. Це виконання [погодженої пропозиції](AI_NPC_P406_SUPERVISOR_VERIFICATION.md), без зміни frozen training sources/config після старту.

## Фактичний результат

- **8980/8980 captured decisions**, усі 20 train-проходів; trainer steps і checkpoint global_step — **8980**. Supervisor valid_run=true, worker/Unity exit=[0,0]. [Result](../Logs/AI-NPC/P4-06-ppo-pilot-review-20261124/result.json), [capture audit](../Logs/AI-NPC/P4-06-ppo-pilot-review-20261124/capture-audit.json).
- У цьому ж Unity-запуску завершено **126/126 regression checks і 8/8 coordinator fixture checks**; це окремо від PPO environment/optimizer steps.
- **96 Adam optimizer steps** у кожному з 12 parameter states; first moments скінченні й мають 42758 ненульових елементів. Це фактичний доказ виконання оптимізації. 8980 environment steps не називаються 8980 optimizer updates.
- Policy state: 12 скінченних tensors, 23949 state elements, включно з buffers. ONNX checker PASS; obs_0=[batch,50], action_masks=[batch,7], discrete_actions=[batch,1]. Це перевірка артефакту, не Unity-inference або якість політики.
- Runtime supervisor-а **2243,3 с ≈ 37 хв 23 с**, менше погоджених 3600 с. Один training run, seed=11; додаткових PPO-запусків не було.
- [Artifact inspection](../Logs/AI-NPC/P4-06-ppo-pilot-review-20261124/artifact-inspection.json), [зведені докази й SHA-256](../Logs/AI-NPC/P4-06-ppo-pilot-review-20261124/pilot-evidence.json). Відповідність поточним sealed sources перевірено; 17 перелічених у spec файлів скопійовані в executed-sources й звірені за hash. Це snapshot файлів spec, не повний архів усього проєкту.

Фінальна модель: [TheCopyDraft2.onnx](../Logs/AI-NPC/P4-06-ppo-pilot-review-20261124/trainer/TheCopy-10398d97ac25/TheCopyDraft2.onnx), **100709 bytes**, SHA-256 `02a522c98d60cc3dd8f34411e485d6eb8e1ac90cad623f52316717f65256bb12`.

Checkpoint: [TheCopyDraft2-8980.pt](../Logs/AI-NPC/P4-06-ppo-pilot-review-20261124/trainer/TheCopy-10398d97ac25/TheCopyDraft2/TheCopyDraft2-8980.pt), **584506 bytes**, SHA-256 `3cfec9ca1e88deb73b9589c949b71dd601a4a3306f5cd0a3b6d57462e99aa36b`.

## Погоджений обсяг

- Користувач явно погодив пілот відповіддю «та, продовжуй». [Запис погодження](../Logs/AI-NPC/P4-06-ppo-pilot-review-20261124/user-approval.json).
- Run ID TheCopy-10398d97ac25; policy seed=11, shuffle seed=7011, generation seed=1001.
- 20 повних train-проходів × 449 рецептів = 8980 одноетапних рішень. ID/OOD не входять у цей запуск.
- PPO/MLP 2×128, 50 inputs, discrete [7], дії 5/6 masked, без руху та механізмів. Batch=256, buffer=8192, num_epoch=3, погоджений expert-attention reward.
- Runtime limit=3600 с, без resume/restart. Actual trainer/global steps мають збігтися з 8980; max_steps=8981 у конфігурації є safety cap для фінального terminal.
- [Sealed spec/source hashes](../Logs/AI-NPC/P4-06-ppo-pilot-review-20261124/prepared.json), [конфігурація](../Logs/AI-NPC/P4-06-ppo-pilot-review-20261124/ppo.yaml), [виконані команди](../Logs/AI-NPC/P4-06-ppo-pilot-review-20261124/started.json), [worker log](../Logs/AI-NPC/P4-06-ppo-pilot-review-20261124/worker.log).

## Моніторинг і перевірка

[Read-AINpcTrainingProgress.ps1](../tools/Read-AINpcTrainingProgress.ps1) відкриває активний JSONL з FileShare.ReadWrite|Delete й рахує тільки завершені рядки. Звичайний Get-Content/ReadLines для цього файлу під час append може блокувати Unity. Читач перевірено під час поточного запуску; його source не належить до frozen trainer/Unity джерел і не змінює епізоди.

```powershell
.\tools\Read-AINpcTrainingProgress.ps1 `
  -Prepared Logs/AI-NPC/P4-06-ppo-pilot-review-20261124
```

Після успішного завершення [inspect_ai_npc_pilot.py](../tools/inspect_ai_npc_pilot.py) виконав перевірки:

1. Supervisor result=pass / valid_run=true, повний capture й обидва exit=0.
2. Trainer steps та scalar checkpoint global_step дорівнюють 8980.
3. Adam parameter state містить позитивні інтегральні step values. Це доказ фактичних optimizer steps, окремий від environment steps. Різні parameter steps звітуються діапазоном, не підміняються одним числом.
4. Policy tensors скінченні; final ONNX проходить onnx.checker і перевірку сигнатури obs_0=[batch,50], action_masks=[batch,7], discrete_actions=[batch,1].
5. Повторний audit captures/schedule/reward та hashes checkpoint/ONNX.

```powershell
& 'D:/PROGS/anaconda3/envs/project-s-ml/python.exe' -B tools/inspect_ai_npc_pilot.py `
  --prepared Logs/AI-NPC/P4-06-ppo-pilot-review-20261124 `
  --output Logs/AI-NPC/P4-06-ppo-pilot-review-20261124/artifact-inspection.json
```

Інспектор не читає активний checkpoint: до появи завершеного valid run він не може повернути PASS. Torch checkpoint дозволений тільки з власного trainer-каталогу цього supervisor-а. Інспекція не запускає ONNX/Unity inference й не перевіряє ID/OOD якість.

Нові unit-тести Adam state: позитивний лічильник, неоднорідні parameter steps, порожній state, нуль/NaN/Infinity/дробові та відсутні значення. **136/136 Python PASS** на 3.13.7 та 3.10.12. [Python 3.13](../Logs/AI-NPC/P4-06-pilot-python-20261124.log), [Python 3.10](../Logs/AI-NPC/P4-06-pilot-python310-20261124.log).

## Межі висновків

Це вже навчений технічний артефакт sensory-intent-v1 у командному проєкті, але не перевірена ігрова політика. Пілот не замінює кілька незалежних експериментів, model selection за ID та фінальний OOD. Середня reward=0.535 у логу на step=5000 описує збір train-rollouts, не якість фінального checkpoint. Сценарії керовані в disposable fixture через чинні сенсори; це не ручна кооперативна гра/мікрофон. Production CopyDecisionAgent/сцени не отримали модель автоматично. Пакети та shared-компоненти не змінено; комітів/push/merge немає.

SDK-експорт не містить embedded input-schema metadata: shape agreement підтверджено, семантика пов’язана sealed контрактом/джерелами. Поля compatibleModel/trainingRun та історичний status=frozen-single-decision-not-trained у JSON-контракті лишилися замороженими: цей старий статус тепер не описує наявність дослідницького пілота. Model registry/production compatibility слід оформити під час вибору моделі та inference-перевірки; під час пілота контракт не переписувався.

## Діагностика завершення SDK та наступний крок

У worker.log після завершення Unity є `[ERROR] Worker 0 exceeded the allowed number of restarts` та `SubprocessEnvManager had workers that didn't signal shutdown`. Їх не вилучали й не приховували. Unity завершила погоджений schedule, trainer cap=8981 очікував наступний environment step, а SDK завершив learning і зберіг модель на **8980**. Фінальні terminal/capture counts, checkpoint та два exit=0 перевірено незалежно від тексту логу.

В установленому SDK SubprocessEnvManager.close має fallback terminate для worker, що не підтвердив CLOSED. Scoped Win32_Process-запит після завершення не знайшов прямих SDK/Unity descendants для власних PID 31532/6232; supervisor також підтвердив завершення своїх Popen. Це не дає підстав називати shutdown-маршрут повністю чистим: повідомлення залишаються технічним обмеженням.

**Наступний зріз P4-06:** перевірити коректне планове завершення trainer/Unity у короткому діагностичному режимі без тривалого навчання й усунути SDK shutdown-діагностику, зберігаючи фінальний terminal. Потім підготувати й погодити бюджет повних незалежних PPO-експериментів. Додаткових seeds або навчання понад цей пілот не запускати автоматично. ID/OOD і Unity-inference навченої моделі лишаються неперевіреними; P4-06 не Done.
