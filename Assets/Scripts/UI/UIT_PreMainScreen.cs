using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class UIT_PreMainScreen : MonoBehaviour
{
    [SerializeField] private UIDocument _uiDocument;
    private VisualElement _root;
    private Button _btn_continue, _btn_new, _btn_load, _btn_language, _btn_options, _btn_quit;

    private VisualElement _slotPanel;
    private enum SlotMode { New, Load }
    private SlotMode _mode;

    void OnEnable()
    {
        _root = _uiDocument.rootVisualElement;

        _btn_continue = _root.Q<Button>("btn_continue");
        _btn_new = _root.Q<Button>("btn_new");
        _btn_load = _root.Q<Button>("btn_load");
        _btn_language = _root.Q<Button>("btn_language");
        _btn_options = _root.Q<Button>("btn_options");
        _btn_quit = _root.Q<Button>("btn_quit");

        if (_btn_continue != null) _btn_continue.clicked += OnContinueClicked;
        if (_btn_new != null) _btn_new.clicked += OnNewClicked;
        if (_btn_load != null) _btn_load.clicked += OnLoadClicked;
        if (_btn_language != null) _btn_language.clicked += OnLanguageClicked;
        if (_btn_options != null) _btn_options.clicked += OnOptionsClicked;
        if (_btn_quit != null) _btn_quit.clicked += OnQuitClicked;

        // Continue / Load are meaningless with no saves on disk.
        bool hasSave = SaveManager.HasAnySave();
        _btn_continue?.SetEnabled(hasSave);
        _btn_load?.SetEnabled(hasSave);

        _root.schedule.Execute(() => (hasSave ? _btn_continue : _btn_new)?.Focus());
    }

    void OnDisable()
    {
        if (_btn_continue != null) _btn_continue.clicked -= OnContinueClicked;
        if (_btn_new != null) _btn_new.clicked -= OnNewClicked;
        if (_btn_load != null) _btn_load.clicked -= OnLoadClicked;
        if (_btn_language != null) _btn_language.clicked -= OnLanguageClicked;
        if (_btn_options != null) _btn_options.clicked -= OnOptionsClicked;
        if (_btn_quit != null) _btn_quit.clicked -= OnQuitClicked;
    }

    // ── Navigation ───────────────────────────────────────────────
    private void OnContinueClicked()
    {
        int slot = SaveManager.MostRecentSlot();
        if (slot < 0) return;
        if (SaveManager.Load(slot) != null) StartGame();
    }

    private void OnNewClicked() => OpenSlotPanel(SlotMode.New);
    private void OnLoadClicked() => OpenSlotPanel(SlotMode.Load);

    private void OnLanguageClicked() { /* Add language logic here */ }
    private void OnOptionsClicked() { /* Add options logic here */ }

    private void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void StartGame() => SceneManager.LoadScene("ChoosePlayer");

    // ── Slot selection panel (built in code) ─────────────────────
    private void OpenSlotPanel(SlotMode mode)
    {
        _mode = mode;
        BuildSlotPanel();
        _slotPanel.style.display = DisplayStyle.Flex;
    }

    private void CloseSlotPanel()
    {
        if (_slotPanel != null) _slotPanel.style.display = DisplayStyle.None;
    }

    private void BuildSlotPanel()
    {
        if (_slotPanel == null)
        {
            _slotPanel = new VisualElement();
            _slotPanel.style.position = Position.Absolute;
            _slotPanel.style.left = 0;
            _slotPanel.style.top = 0;
            _slotPanel.style.right = 0;
            _slotPanel.style.bottom = 0;
            _slotPanel.style.backgroundColor = new Color(0f, 0f, 0f, 0.8f);
            _slotPanel.style.alignItems = Align.Center;
            _slotPanel.style.justifyContent = Justify.Center;
            _root.Add(_slotPanel);
        }
        _slotPanel.Clear();

        var card = new VisualElement();
        card.style.minWidth = 520;
        card.style.paddingLeft = 24;
        card.style.paddingRight = 24;
        card.style.paddingTop = 20;
        card.style.paddingBottom = 20;
        card.style.backgroundColor = new Color(0.08f, 0.09f, 0.10f, 0.98f);
        card.style.borderTopWidth = card.style.borderBottomWidth = 1;
        card.style.borderLeftWidth = card.style.borderRightWidth = 1;
        var accent = new Color(0.37f, 0.88f, 0.65f, 0.5f);
        card.style.borderTopColor = card.style.borderBottomColor = accent;
        card.style.borderLeftColor = card.style.borderRightColor = accent;
        _slotPanel.Add(card);

        var title = new Label(_mode == SlotMode.New ? "NEW GAME — CHOOSE SLOT" : "LOAD GAME — CHOOSE SLOT");
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.fontSize = 18;
        title.style.color = new Color(0.37f, 0.88f, 0.65f);
        title.style.marginBottom = 14;
        card.Add(title);

        foreach (var info in SaveManager.ListSlots())
            card.Add(BuildSlotRow(info));

        var close = new Button(CloseSlotPanel) { text = "CLOSE" };
        close.style.marginTop = 14;
        close.style.height = 32;
        card.Add(close);
    }

    private VisualElement BuildSlotRow(SlotInfo info)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.marginTop = 6;
        row.style.marginBottom = 6;
        row.style.paddingLeft = 10;
        row.style.paddingRight = 10;
        row.style.height = 44;
        row.style.backgroundColor = new Color(1f, 1f, 1f, 0.04f);

        var summary = new Label(DescribeSlot(info));
        summary.style.flexGrow = 1;
        summary.style.color = Color.white;
        row.Add(summary);

        // Primary action: CREATE/OVERWRITE in New mode, LOAD in Load mode.
        string primaryText = _mode == SlotMode.New
            ? (info.exists ? "OVERWRITE" : "CREATE")
            : "LOAD";
        var primary = new Button(() => OnSlotPrimary(info.slot)) { text = primaryText };
        primary.style.marginLeft = 8;
        primary.style.minWidth = 90;
        if (_mode == SlotMode.Load && !info.exists) primary.SetEnabled(false);
        row.Add(primary);

        var del = new Button(() => OnSlotDelete(info.slot)) { text = "DELETE" };
        del.style.marginLeft = 8;
        del.style.minWidth = 80;
        del.SetEnabled(info.exists);
        row.Add(del);

        return row;
    }

    private string DescribeSlot(SlotInfo info)
    {
        if (!info.exists) return $"Slot {info.slot + 1} — Empty";
        string when = "";
        if (DateTime.TryParse(info.lastPlayedIso, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var t))
            when = "  ·  " + t.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        return $"Slot {info.slot + 1} — {info.player}  ·  XP {info.globalXP}{when}";
    }

    private void OnSlotPrimary(int slot)
    {
        if (_mode == SlotMode.New)
        {
            SaveManager.NewGame(slot);
            StartGame();
        }
        else
        {
            if (SaveManager.Load(slot) != null) StartGame();
        }
    }

    private void OnSlotDelete(int slot)
    {
        SaveManager.Delete(slot);
        // Rebuild rows + refresh menu button enabled-state.
        BuildSlotPanel();
        bool hasSave = SaveManager.HasAnySave();
        _btn_continue?.SetEnabled(hasSave);
        _btn_load?.SetEnabled(hasSave);
    }
}
