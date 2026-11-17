using TheCopy.Shared;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(NetworkObject))]
public class NetworkGridOsTerminal : NetworkBehaviour, IInteractable
{
    public static NetworkGridOsTerminal ActiveLocalTerminal { get; private set; }
    public static bool IsAnyGridTerminalOpen => ActiveLocalTerminal != null;

    public enum TerminalRole
    {
        MasterControl_EastLab = 0,   // Головний комп'ютер зі списком програм (Східний корпус)
        DiagnosticsNode_Guardhouse = 1 // Діагностичний монітор із підказками для Рації [T] (Будка КПП)
    }

    [Header("Роль комп'ютера GRiD-87 у кооперативному пазлі")]
    [SerializeField] private TerminalRole role = TerminalRole.MasterControl_EastLab;
    [SerializeField] private string systemPassword = "8701";

    [Header("Зв'язок із верхньою брамою спуску та важелями бункера")]
    [Tooltip("Перетягни сюди верхні двері/заслінку над спуском у бункер, які піднімуться вгору")]
    [SerializeField] private NetworkAirlockDoor outerRampShutterDoor;

    [Tooltip("Або перетягни сюди вузол живлення, який розблокує парні важелі внизу")]
    [SerializeField] private NetworkInteractiveObject hydraulicPowerRelay;

    public NetworkVariable<bool> IsMediaRepaired = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> IsMediaInitialized = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> IsAirlockAuthorized = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private struct GridFileEntry
    {
        public string Title;
        public string Kind;
        public string Subject;

        public GridFileEntry(string title, string kind, string subject = "Programs")
        {
            Title = title;
            Kind = kind;
            Subject = subject;
        }
    }

    // Точний список файлів 1-в-1 зі скріншота GRiD-OS (1-Jan-87)
    private readonly GridFileEntry[] _files =
    {
        new GridFileEntry("Duplicate Media", "Run", "Utilities"),
        new GridFileEntry("Executive", "Com", "System"),
        new GridFileEntry("Executive", "Run", "System"),
        new GridFileEntry("GRiD 53", "Font", "Fonts"),
        new GridFileEntry("GRiD 64", "Font", "Fonts"),
        new GridFileEntry("GRiD 80", "Font", "Fonts"),
        new GridFileEntry("GRiDManager", "Run Sign-on", "Airlock Control"),
        new GridFileEntry("GRiDWrite", "Run", "Programs"),
        new GridFileEntry("GRiDWrite", "Run Text", "Programs"),
        new GridFileEntry("Initialize Media", "Run", "Bubble Memory"),
        new GridFileEntry("MediaRepair", "Run", "Diagnostics"),
        new GridFileEntry("Screen.Init", "ScreenImage", "Display"),
        new GridFileEntry("User", "Profile", "Security")
    };

    private int _selectedIndex = 5; // За замовчуванням виділено "GRiD 80 | Font" як на референсі!
    private string _passwordInput = "";
    private bool _awaitingPasswordPrompt = false;
    private string _footerLine1 = "Select a file and confirm";
    private string _footerLine2 = "Or press CODE-? for help";

    private Texture2D _blackBgTex;
    private Texture2D _amberTex;
    private Texture2D _darkAmberTex;
    private Texture2D _scanlineTex;

    private void Awake()
    {
        CreateCrtTextures();
    }

    public string GetPromptText()
    {
        if (role == TerminalRole.DiagnosticsNode_Guardhouse)
        {
            return "[E] Переглянути діагностику ЦП «GRiD-87» (КПП)";
        }

        return IsAirlockAuthorized.Value
            ? "[GRiD-87] Гідропривід бункера активовано"
            : "[E] Термінал «GRiD-87» (Bubble Memory OS)";
    }

    public void Interact()
    {
        if (ActiveLocalTerminal == this)
        {
            CloseTerminal();
            return;
        }

        OpenTerminal();
    }

    public void OpenTerminal()
    {
        ActiveLocalTerminal = this;
        _awaitingPasswordPrompt = false;
        _passwordInput = "";
        UpdateFooterStatus();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseTerminal()
    {
        if (ActiveLocalTerminal == this)
        {
            ActiveLocalTerminal = null;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (ActiveLocalTerminal != this) return;

        // Навігація стрілками W/S або Up/Down як на справжньому DOS/GRiD комп'ютері 80-х
        if (!_awaitingPasswordPrompt)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                _selectedIndex = (_selectedIndex - 1 + _files.Length) % _files.Length;
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                _selectedIndex = (_selectedIndex + 1) % _files.Length;
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                ExecuteSelectedFile();
            }
        }
        else
        {
            // Ввід пароля з клавіатури для Initialize Media
            for (int i = 0; i <= 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
                {
                    if (_passwordInput.Length < 8) _passwordInput += i.ToString();
                }
            }

            if (Input.GetKeyDown(KeyCode.Backspace) && _passwordInput.Length > 0)
            {
                _passwordInput = _passwordInput.Substring(0, _passwordInput.Length - 1);
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                SubmitInitializePasswordServerRpc(_passwordInput);
            }
        }
    }

    private void ExecuteSelectedFile()
    {
        if (role == TerminalRole.DiagnosticsNode_Guardhouse) return;

        string title = _files[_selectedIndex].Title;
        string kind = _files[_selectedIndex].Kind;

        if (title == "MediaRepair")
        {
            RunMediaRepairServerRpc();
        }
        else if (title == "Initialize Media")
        {
            if (!IsMediaRepaired.Value)
            {
                _footerLine1 = "ERROR: BUBBLE MEMORY CORRUPTED!";
                _footerLine2 = "Run 'MediaRepair' utility first.";
                EmitBeepNoiseServerRpc(0.45f);
                return;
            }

            _awaitingPasswordPrompt = true;
            _passwordInput = "";
            _footerLine1 = "ENTER 4-DIGIT BUBBLE MEMORY PASSCODE";
            _footerLine2 = "Check Guardhouse Diagnostics via Radio [T]";
        }
        else if (title == "GRiDManager" && kind == "Run Sign-on")
        {
            RunGridManagerSignOnServerRpc();
        }
        else
        {
            _footerLine1 = $"Cannot execute '{title} ({kind})' in Lockdown";
            _footerLine2 = "Required: MediaRepair -> Initialize -> GRiDManager";
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RunMediaRepairServerRpc()
    {
        IsMediaRepaired.Value = true;
        UpdateStatusClientRpc("BUBBLE MEMORY SECTORS REPAIRED [OK]", "Next step: Run 'Initialize Media'");
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitInitializePasswordServerRpc(string enteredPass)
    {
        if (!IsMediaRepaired.Value) return;

        if (enteredPass == systemPassword)
        {
            IsMediaInitialized.Value = true;
            PasswordResultClientRpc(true);
        }
        else
        {
            NoiseDispatcher.EmitOnServer(new NoiseEvent
            {
                Position = transform.position,
                Loudness = 0.7f,
                Source = NoiseSource.Environmental
            });
            PasswordResultClientRpc(false);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RunGridManagerSignOnServerRpc()
    {
        if (!IsMediaRepaired.Value || !IsMediaInitialized.Value)
        {
            UpdateStatusClientRpc("ACCESS DENIED: MEDIA NOT INITIALIZED", "Complete MediaRepair & Initialize Media first!");
            return;
        }

        IsAirlockAuthorized.Value = true;

        if (outerRampShutterDoor != null)
        {
            outerRampShutterDoor.IsDoorOpen.Value = true;
        }

        if (hydraulicPowerRelay != null)
        {
            hydraulicPowerRelay.IsActive.Value = true;
        }

        NoiseDispatcher.EmitOnServer(new NoiseEvent
        {
            Position = transform.position,
            Loudness = 0.65f,
            Source = NoiseSource.Environmental
        });

        UpdateStatusClientRpc("AIRLOCK HYDRAULICS AUTHORIZED [ONLINE]", "Outer Gate Open! Pull BOTH Bunker Levers simultaneously.");
    }

    [ServerRpc(RequireOwnership = false)]
    private void EmitBeepNoiseServerRpc(float loudness)
    {
        NoiseDispatcher.EmitOnServer(new NoiseEvent
        {
            Position = transform.position,
            Loudness = loudness,
            Source = NoiseSource.Environmental
        });
    }

    [ClientRpc]
    private void PasswordResultClientRpc(bool success)
    {
        _awaitingPasswordPrompt = false;
        if (success)
        {
            _footerLine1 = "BUBBLE MEMORY INITIALIZED [OK]";
            _footerLine2 = "Now select 'GRiDManager (Run Sign-on)' to unlock Gate!";
        }
        else
        {
            _passwordInput = "";
            _footerLine1 = "INVALID PASSWORD! SECURITY ALERT LOGGED";
            _footerLine2 = "Ask partner in Guardhouse for Bubble Memory Code [T]";
        }
    }

    [ClientRpc]
    private void UpdateStatusClientRpc(string line1, string line2)
    {
        _footerLine1 = line1;
        _footerLine2 = line2;

        if (IsAirlockAuthorized.Value && RetroAuditorHud.Instance != null)
        {
            RetroAuditorHud.ShowThoughtBanner(
                "ВЕРХНЮ ЗАСЛІНКУ БУНКЕРА ВІДКРИТО!",
                "Спустіться вниз до входу та одночасно поверніть лівий і правий важелі",
                8.0f
            );
        }
    }

    private void UpdateFooterStatus()
    {
        if (IsAirlockAuthorized.Value)
        {
            _footerLine1 = "SYSTEM ONLINE — BUNKER HYDRAULICS ACTIVE";
            _footerLine2 = "Proceed to Bunker Ramp and pull dual levers";
        }
        else if (IsMediaInitialized.Value)
        {
            _footerLine1 = "Bubble Memory Ready. Select 'GRiDManager'";
            _footerLine2 = "Press [Enter] on 'GRiDManager | Run Sign-on'";
        }
        else if (IsMediaRepaired.Value)
        {
            _footerLine1 = "Media Repaired. Select 'Initialize Media'";
            _footerLine2 = "Password required from Guardhouse Terminal";
        }
        else
        {
            _footerLine1 = "Select a file and confirm";
            _footerLine2 = "Or press CODE-? for help";
        }
    }

    private void OnGUI()
    {
        if (ActiveLocalTerminal != this || NetworkBootstrapUI.IsPauseMenuOpen) return;

        float w = 640f;
        float h = 480f;
        float x = (Screen.width - w) / 2f;
        float y = (Screen.height - h) / 2f;

        Color amberBright = new Color(1.0f, 0.74f, 0.12f, 1f); // Фірмовий бурштиновий фосфор GRiD-OS
        Color amberDim = new Color(0.78f, 0.52f, 0.08f, 1f);
        Color blackPhosphor = new Color(0.06f, 0.04f, 0.01f, 1f);

        // Чорний екран монітора та подвійна бурштинова рамка (1-в-1 як на image_6312a0.png)
        GUI.DrawTexture(new Rect(x - 6f, y - 6f, w + 12f, h + 12f), _blackBgTex);
        DrawAmberBorder(new Rect(x, y, w, h), 2f);
        GUI.DrawTexture(new Rect(x + 2f, y + 2f, w - 4f, h - 4f), _blackBgTex);

        GUIStyle amberFont = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        amberFont.normal.textColor = amberBright;

        GUIStyle amberRightFont = new GUIStyle(amberFont)
        {
            alignment = TextAnchor.MiddleRight
        };

        GUIStyle blackOnAmberFont = new GUIStyle(amberFont);
        blackOnAmberFont.normal.textColor = blackPhosphor;

        GUIStyle blackOnAmberRight = new GUIStyle(amberRightFont);
        blackOnAmberRight.normal.textColor = blackPhosphor;

        // 1. ВЕРХНІЙ РЯДОК ДАТИ Й ЧАСУ: "1-Jan-87" зліва та "1:44 am" справа
        GUI.Label(new Rect(x + 18f, y + 6f, 250f, 24f), "1-Jan-87", amberFont);
        GUI.Label(new Rect(x + w - 268f, y + 6f, 250f, 24f), "1:44 am", amberRightFont);
        GUI.DrawTexture(new Rect(x, y + 32f, w, 2f), _amberTex);

        if (role == TerminalRole.DiagnosticsNode_Guardhouse)
        {
            DrawGuardhouseDiagnosticsScreen(x, y, w, h, amberFont, blackOnAmberFont);
        }
        else
        {
            DrawMasterGridOsFileScreen(x, y, w, h, amberFont, amberRightFont, blackOnAmberFont, blackOnAmberRight, amberDim);
        }

        // Ефект горизонтальних ліній старого бурштинового дисплея поверх вікна
        Color prev = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.08f);
        GUI.DrawTextureWithTexCoords(new Rect(x, y, w, h), _scanlineTex, new Rect(0, 0, 1f, h / 4f));
        GUI.color = prev;
    }

    // --- ЕКРАН 1-В-1 ЯК НА ТВОЄМУ СКРІНШОТІ (ДЛЯ СХІДНОГО КОРПУСУ) ---
    private void DrawMasterGridOsFileScreen(
        float x, float y, float w, float h,
        GUIStyle amberL, GUIStyle amberR,
        GUIStyle blackL, GUIStyle blackR, Color amberDim)
    {
        float listStartY = y + 38f;
        float rowH = 20f;

        // СПИСОК ФАЙЛІВ (13 рядків із виділеною бурштиновою смугою)
        for (int i = 0; i < _files.Length; i++)
        {
            Rect rowRect = new Rect(x + 14f, listStartY + i * rowH, w - 28f, rowH);
            bool isSelected = (i == _selectedIndex);

            if (isSelected)
            {
                GUI.DrawTexture(rowRect, _amberTex);
                GUI.Label(new Rect(rowRect.x + 12f, rowRect.y, 340f, rowH), _files[i].Title, blackL);
                GUI.Label(new Rect(rowRect.x + w - 310f, rowRect.y, 270f, rowH), _files[i].Kind, blackR);
            }
            else
            {
                GUI.Label(new Rect(rowRect.x + 12f, rowRect.y, 340f, rowH), _files[i].Title, amberL);
                GUI.Label(new Rect(rowRect.x + w - 310f, rowRect.y, 270f, rowH), _files[i].Kind, amberR);
            }

            // Підтримка кліку мишкою по рядку (1 клік — вибрати, повторний клік — запустити)
            if (GUI.Button(rowRect, "", GUIStyle.none))
            {
                if (_selectedIndex == i)
                {
                    ExecuteSelectedFile();
                }
                else
                {
                    _selectedIndex = i;
                    _awaitingPasswordPrompt = false;
                }
            }
        }

        // Горизонтальна лінія відділення метаданих
        float metaY = y + 306f;
        GUI.DrawTexture(new Rect(x, metaY, w, 2f), _amberTex);

        // НИЖНІЙ БЛОК МЕТАДАНИХ: Device / Subject / Title / Kind / Password
        GridFileEntry sel = _files[_selectedIndex];
        float labelX = x + 26f;
        float valX = x + 175f;

        GUI.Label(new Rect(labelX, metaY + 8f, 140f, 22f), "Device", amberL);
        GUI.Label(new Rect(valX, metaY + 8f, 350f, 22f), "Bubble Memory", amberL);

        GUI.Label(new Rect(labelX, metaY + 30f, 140f, 22f), "Subject", amberL);
        GUI.Label(new Rect(valX, metaY + 30f, 350f, 22f), sel.Subject, amberL);

        GUI.Label(new Rect(labelX, metaY + 52f, 140f, 22f), "Title", amberL);
        Rect titleBox = new Rect(valX - 4f, metaY + 52f, 415f, 22f);
        DrawAmberBorder(titleBox, 2f);
        GUI.Label(new Rect(valX + 4f, metaY + 52f, 400f, 22f), sel.Title, amberL);

        GUI.Label(new Rect(labelX, metaY + 74f, 140f, 22f), "Kind", amberL);
        GUI.Label(new Rect(valX, metaY + 74f, 350f, 22f), sel.Kind, amberL);

        GUI.Label(new Rect(labelX, metaY + 96f, 140f, 22f), "Password", amberL);
        if (_awaitingPasswordPrompt)
        {
            Rect passBox = new Rect(valX - 4f, metaY + 96f, 220f, 22f);
            DrawAmberBorder(passBox, 2f);
            GUI.Label(new Rect(valX + 6f, metaY + 96f, 200f, 22f), _passwordInput + "_", amberL);

            if (GUI.Button(new Rect(valX + 230f, metaY + 95f, 130f, 24f), "CONFIRM [OK]"))
            {
                SubmitInitializePasswordServerRpc(_passwordInput);
            }
        }
        else
        {
            string passState = IsMediaInitialized.Value ? "**** [VERIFIED]" : "";
            GUI.Label(new Rect(valX, metaY + 96f, 350f, 22f), passState, amberL);
        }

        // Кнопки швидкої дії справа внизу для зручності мишки
        if (!_awaitingPasswordPrompt)
        {
            if (GUI.Button(new Rect(x + w - 165f, metaY + 95f, 145f, 24f), "RUN FILE [Enter]"))
            {
                ExecuteSelectedFile();
            }
        }

        // НИЖНЯ СУЦІЛЬНА БУРШТИНОВА ПЛАШКА ПІДКАЗКИ (1-в-1 як на скріні!)
        Rect footerRect = new Rect(x + 2f, y + h - 48f, w - 4f, 46f);
        GUI.DrawTexture(footerRect, _amberTex);

        GUIStyle footerCenter = new GUIStyle(blackL)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 15
        };
        GUI.Label(new Rect(x, y + h - 46f, w, 22f), _footerLine1, footerCenter);
        GUI.Label(new Rect(x, y + h - 25f, w, 22f), _footerLine2 + "   |   [Esc] Вийти", footerCenter);
    }

    // --- ЕКРАН ДІАГНОСТИКИ В КПП (ДЛЯ ГРАВЦЯ 1, ЯКИЙ ДИКТУЄ ПО РАЦІЇ [T]) ---
    private void DrawGuardhouseDiagnosticsScreen(float x, float y, float w, float h, GUIStyle amberL, GUIStyle blackL)
    {
        float cy = y + 48f;
        GUI.Label(new Rect(x + 24f, cy, w - 48f, 26f), "=== ВУЗОЛ ТЕЛЕМЕТРІЇ ШЛЮЗУ «GRiD-87» (КПП ОХОРОНИ) ===", amberL);
        cy += 36f;

        string step1 = IsMediaRepaired.Value ? "[ВИКОНАНО]" : "[ОЧІКУЄТЬСЯ]";
        string step2 = IsMediaInitialized.Value ? "[ВИКОНАНО]" : "[ОЧІКУЄТЬСЯ]";
        string step3 = IsAirlockAuthorized.Value ? "[ВИКОНАНО]" : "[ОЧІКУЄТЬСЯ]";

        GUI.Label(new Rect(x + 24f, cy, w - 48f, 24f), "СТАТУС ГЕРМОБРАМИ БУНКЕРА: АВАРІЙНИЙ КАРАНТИН ЦП", amberL);
        cy += 34f;

        GUI.Label(new Rect(x + 24f, cy, w - 48f, 24f), "ІНСТРУКЦІЯ ДЛЯ ОПЕРАТОРА СХІДНОГО КОРПУСУ (ПЕРЕДАЙТЕ ПО РАЦІЇ [T]):", amberL);
        cy += 32f;

        GUI.Label(new Rect(x + 34f, cy, w - 68f, 24f), $"1. Запустити утиліту:  MediaRepair  (Run) ........ {step1}", amberL);
        cy += 28f;
        GUI.Label(new Rect(x + 34f, cy, w - 68f, 24f), $"2. Запустити утиліту:  Initialize Media (Run) ... {step2}", amberL);
        cy += 28f;

        Rect codeBox = new Rect(x + 34f, cy, w - 68f, 34f);
        DrawAmberBorder(codeBox, 2f);
        GUI.Label(new Rect(x + 48f, cy + 5f, w - 90f, 24f), $"   КОД ДОСТУПУ BUBBLE MEMORY (PASSWORD):  {systemPassword}", amberL);
        cy += 46f;

        GUI.Label(new Rect(x + 34f, cy, w - 68f, 24f), $"3. Авторизувати шлюз:  GRiDManager (Run Sign-on)  {step3}", amberL);
        cy += 38f;

        GUI.Label(new Rect(x + 24f, cy, w - 48f, 24f), "4. Після підняття верхньої заслінки — спустіться в бункер і", amberL);
        cy += 22f;
        GUI.Label(new Rect(x + 24f, cy, w - 48f, 24f), "   ОДНОЧАСНО поверніть ЛІВИЙ та ПРАВИЙ важелі біля гермодверей!", amberL);

        Rect footerRect = new Rect(x + 2f, y + h - 48f, w - 4f, 46f);
        GUI.DrawTexture(footerRect, _amberTex);
        GUIStyle footerCenter = new GUIStyle(blackL) { alignment = TextAnchor.MiddleCenter };
        GUI.Label(new Rect(x, y + h - 38f, w, 28f), "Натисніть [Esc], затисніть рацію [T] і продиктуйте кроки напарнику", footerCenter);
    }

    private void DrawAmberBorder(Rect r, float thickness)
    {
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, thickness), _amberTex);
        GUI.DrawTexture(new Rect(r.x, r.y + r.height - thickness, r.width, thickness), _amberTex);
        GUI.DrawTexture(new Rect(r.x, r.y, thickness, r.height), _amberTex);
        GUI.DrawTexture(new Rect(r.x + r.width - thickness, r.y, thickness, r.height), _amberTex);
    }

    private void CreateCrtTextures()
    {
        _blackBgTex = new Texture2D(1, 1);
        _blackBgTex.SetPixel(0, 0, new Color(0.05f, 0.03f, 0.01f, 0.98f));
        _blackBgTex.Apply();

        _amberTex = new Texture2D(1, 1);
        _amberTex.SetPixel(0, 0, new Color(0.98f, 0.72f, 0.12f, 1f));
        _amberTex.Apply();

        _darkAmberTex = new Texture2D(1, 1);
        _darkAmberTex.SetPixel(0, 0, new Color(0.45f, 0.30f, 0.05f, 1f));
        _darkAmberTex.Apply();

        _scanlineTex = new Texture2D(1, 4, TextureFormat.RGBA32, false);
        _scanlineTex.filterMode = FilterMode.Point;
        _scanlineTex.wrapMode = TextureWrapMode.Repeat;
        _scanlineTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 1f));
        _scanlineTex.SetPixel(0, 1, new Color(0f, 0f, 0f, 0f));
        _scanlineTex.SetPixel(0, 2, new Color(0f, 0f, 0f, 0.4f));
        _scanlineTex.SetPixel(0, 3, new Color(0f, 0f, 0f, 0f));
        _scanlineTex.Apply();
    }
}