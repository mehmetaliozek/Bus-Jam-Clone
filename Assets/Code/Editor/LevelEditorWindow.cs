using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class LevelEditorWindow : EditorWindow
{
    private const string LevelsDirectory = "Assets/Resources/Levels";
    private const string PassengerTypesDirectory = "Assets/Resources/PassengerTypes";

    private const float MinWindowWidth = 900f;
    private const float MinWindowHeight = 520f;
    private const float TopToolbarHeight = 21f;
    private const float ToolbarItemHeight = 24f;
    private const float CellSpacing = 4f;

    public static readonly GameColor[] AllColors = (GameColor[])Enum.GetValues(typeof(GameColor));
    private static readonly System.Random s_Rng = new System.Random();

    private readonly List<LevelDataSO> _allLevels = new List<LevelDataSO>();
    private readonly List<PassengerTypeSO> _passengerTypes = new List<PassengerTypeSO>();
    private LevelDataSO _selectedLevel;
    private Vector2 _levelsScrollPos;
    private Vector2 _gridScrollPos;
    private Vector2 _rightPanelScrollPos;
    private string _searchFilter = string.Empty;

    private readonly HashSet<Vector2Int> _activeCellLookup = new HashSet<Vector2Int>();
    private readonly Dictionary<Vector2Int, PassengerData> _passengerLookup = new Dictionary<Vector2Int, PassengerData>();
    private readonly int[] _passengerCountsByColor = new int[AllColors.Length];
    private readonly int[] _passengerSeatDemandByColor = new int[AllColors.Length];
    private readonly int[] _busCountsByColor = new int[AllColors.Length];
    private readonly List<GameColor> _activeBalanceColors = new List<GameColor>(AllColors.Length);
    private int _selectedToolIndex = 0;
    private readonly string[] _toolLabels = {
        "Paint Passenger",
        "Toggle Tile",
        "Remove Passenger",
        "Delete Tile"
    };

    private GameColor _activeColor = GameColor.Red;
    private PassengerTypeSO _activePassengerType;
    private int _activeTypeParameter = 1;
    private bool _isTypePaintMode;

    private Vector2Int _lastInteractedCoord = new Vector2Int(-1, -1);
    private bool _isDragging;

    private GUIStyle _centerLabelStyle;
    private GUIStyle CenterLabelStyle
    {
        get
        {
            if (_centerLabelStyle == null)
            {
                _centerLabelStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 11,
                    margin = new RectOffset(0, 4, 2, 2),
                    padding = new RectOffset(0, 0, 0, 0)
                };
            }
            return _centerLabelStyle;
        }
    }

    private GUIStyle _titleStyle;
    private GUIStyle TitleStyle
    {
        get
        {
            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    margin = new RectOffset(4, 4, 0, 0),
                    padding = new RectOffset(0, 0, 0, 0)
                };
            }
            return _titleStyle;
        }
    }

    private GUIStyle _levelInfoStyle;
    private GUIStyle LevelInfoStyle
    {
        get
        {
            if (_levelInfoStyle == null)
            {
                _levelInfoStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    margin = new RectOffset(4, 4, 0, 0),
                    padding = new RectOffset(0, 0, 0, 0)
                };
            }
            return _levelInfoStyle;
        }
    }

    private GUIStyle _levelBtnStyle;
    private GUIStyle LevelBtnStyle
    {
        get
        {
            if (_levelBtnStyle == null)
            {
                _levelBtnStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 11,
                    padding = new RectOffset(8, 8, 5, 5)
                };
            }
            return _levelBtnStyle;
        }
    }

    private GUIStyle _toolsBoxStyle;
    private GUIStyle ToolsBoxStyle
    {
        get
        {
            if (_toolsBoxStyle == null)
            {
                _toolsBoxStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(10, 10, 8, 8)
                };
            }
            return _toolsBoxStyle;
        }
    }

    private GUIStyle _colorBtnLabelStyle;
    private GUIStyle ColorBtnLabelStyle
    {
        get
        {
            if (_colorBtnLabelStyle == null)
            {
                _colorBtnLabelStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 10
                };
            }
            return _colorBtnLabelStyle;
        }
    }

    private GUIStyle _cellBadgeStyle;
    private GUIStyle CellBadgeStyle
    {
        get
        {
            if (_cellBadgeStyle == null)
            {
                _cellBadgeStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 10
                };
            }
            return _cellBadgeStyle;
        }
    }

    private GUIStyle _rightStatusStyle;
    private GUIStyle RightStatusStyle
    {
        get
        {
            if (_rightStatusStyle == null)
            {
                _rightStatusStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleRight,
                    fontSize = 11
                };
            }
            return _rightStatusStyle;
        }
    }

    private LevelDataSO _levelToDelete;
    private int _busToRemove = -1;
    private bool _shouldAddBus;
    private bool _shouldClearBuses;
    private bool _shouldAutoBuses;
    private bool _shouldShuffleBuses;
    private ReorderableList _busReorderableList;
    private LevelDataSO _busListBoundLevel;

    [MenuItem("Tools/Bus Jam/Level Editor")]
    [MenuItem("Window/Bus Jam/Level Editor")]
    public static void OpenWindow()
    {
        LevelEditorWindow window = GetWindow<LevelEditorWindow>("Level Editor");
        window.minSize = new Vector2(MinWindowWidth, MinWindowHeight);
        window.Show();
    }

    public static void OpenWithLevel(LevelDataSO level)
    {
        LevelEditorWindow window = GetWindow<LevelEditorWindow>("Level Editor");
        window.minSize = new Vector2(MinWindowWidth, MinWindowHeight);
        window.SelectLevel(level);
        window.Show();
    }

    private void OnEnable()
    {
        minSize = new Vector2(MinWindowWidth, MinWindowHeight);
        RefreshPassengerTypes();
        RefreshLevelList();
        Undo.undoRedoPerformed += OnUndoRedo;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndoRedo;
    }

    private void OnProjectChange()
    {
        RefreshPassengerTypes();
        RefreshLevelList();
        Repaint();
    }

    private void OnUndoRedo()
    {
        ResetDeferredBusFlags();
        if (_selectedLevel != null)
        {
            EnsureLevelDataLists(_selectedLevel);
            RebuildLevelCache();
            EditorUtility.SetDirty(_selectedLevel);
        }
        Repaint();
    }

    private void OnGUI()
    {
        ProcessDeferredActions();

        RebuildLevelCache();

        GUILayout.BeginArea(new Rect(0f, 0f, position.width, TopToolbarHeight));
        DrawTopToolbar();
        GUILayout.EndArea();

        const float outerPad = 4f;
        const float columnGap = 6f;
        float topY = TopToolbarHeight + 4f;
        float contentHeight = Mathf.Max(100f, position.height - topY - outerPad);
        float totalAvailableWidth = Mathf.Max(600f, position.width - (outerPad * 2f) - (columnGap * 2f));

        float t = Mathf.InverseLerp(960f, 1920f, position.width);
        float leftWidth = Mathf.Round(Mathf.Lerp(190f, 240f, t));
        float rightWidth = Mathf.Round(Mathf.Lerp(285f, 345f, t));
        float middleWidth = Mathf.Max(300f, totalAvailableWidth - leftWidth - rightWidth);

        Rect leftRect = new Rect(outerPad, topY, leftWidth, contentHeight);
        Rect middleRect = new Rect(leftRect.xMax + columnGap, topY, middleWidth, contentHeight);
        Rect rightRect = new Rect(middleRect.xMax + columnGap, topY, rightWidth, contentHeight);

        DrawLeftExplorer(leftRect);
        DrawMiddleCanvas(middleRect);
        DrawRightPanel(rightRect);

        if (Event.current.type == EventType.MouseUp)
        {
            _isDragging = false;
            _lastInteractedCoord = new Vector2Int(-1, -1);
        }
    }

    private void RebuildLevelCache()
    {
        _activeCellLookup.Clear();
        _passengerLookup.Clear();
        Array.Clear(_passengerCountsByColor, 0, _passengerCountsByColor.Length);
        Array.Clear(_passengerSeatDemandByColor, 0, _passengerSeatDemandByColor.Length);
        Array.Clear(_busCountsByColor, 0, _busCountsByColor.Length);
        _activeBalanceColors.Clear();

        if (_selectedLevel == null) return;

        EnsureLevelDataLists(_selectedLevel);

        List<GridCellData> cells = _selectedLevel.GridCells;
        for (int i = 0; i < cells.Count; i++)
        {
            GridCellData cell = cells[i];
            if (cell != null)
            {
                _activeCellLookup.Add(cell.Coordinates);
            }
        }

        List<PassengerData> passengers = _selectedLevel.Passengers;
        for (int i = 0; i < passengers.Count; i++)
        {
            PassengerData p = passengers[i];
            if (p != null)
            {
                _passengerLookup[p.Coordinates] = p;
                int colorIndex = (int)p.Color;
                if (colorIndex >= 0 && colorIndex < _passengerCountsByColor.Length)
                {
                    _passengerCountsByColor[colorIndex]++;
                    _passengerSeatDemandByColor[colorIndex] += p.SeatRequirement;
                }
            }
        }

        List<BusData> buses = _selectedLevel.Buses;
        for (int i = 0; i < buses.Count; i++)
        {
            BusData b = buses[i];
            if (b != null)
            {
                int colorIndex = (int)b.Color;
                if (colorIndex >= 0 && colorIndex < _busCountsByColor.Length)
                {
                    _busCountsByColor[colorIndex]++;
                }
            }
        }

        for (int i = 0; i < AllColors.Length; i++)
        {
            if (_passengerCountsByColor[i] > 0 || _busCountsByColor[i] > 0)
            {
                _activeBalanceColors.Add(AllColors[i]);
            }
        }
    }

    private void ProcessDeferredActions()
    {
        if (_levelToDelete != null)
        {
            DeleteLevel(_levelToDelete);
            _levelToDelete = null;
            Repaint();
        }

        if (_selectedLevel == null)
        {
            ResetDeferredBusFlags();
            return;
        }

        EnsureLevelDataLists(_selectedLevel);

        if (_shouldAddBus)
        {
            AddBus(GameColor.Red);
        }

        if (_shouldClearBuses)
        {
            Undo.RecordObject(_selectedLevel, "Clear Buses");
            _selectedLevel.Buses.Clear();
            MarkSelectedDirty();
        }

        if (_shouldAutoBuses)
        {
            AutoGenerateBuses();
        }

        if (_shouldShuffleBuses)
        {
            ShuffleBuses();
        }

        if (_busToRemove >= 0 && _busToRemove < _selectedLevel.Buses.Count)
        {
            Undo.RecordObject(_selectedLevel, "Remove Bus");
            _selectedLevel.Buses.RemoveAt(_busToRemove);
            MarkSelectedDirty();
        }

        ResetDeferredBusFlags();
    }

    private void ResetDeferredBusFlags()
    {
        _busToRemove = -1;
        _shouldAddBus = false;
        _shouldClearBuses = false;
        _shouldAutoBuses = false;
        _shouldShuffleBuses = false;
    }

    private void EnsureLevelDataLists(LevelDataSO level)
    {
        if (level == null) return;
        if (level.GridCells == null) level.GridCells = new List<GridCellData>();
        if (level.Passengers == null) level.Passengers = new List<PassengerData>();
        if (level.Buses == null) level.Buses = new List<BusData>();
    }

    private void DrawTopToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        GUILayout.Label("Level Editor Studio", TitleStyle);

        GUILayout.FlexibleSpace();

        if (_selectedLevel != null)
        {
            bool isDirty = EditorUtility.IsDirty(_selectedLevel);
            string dirty = isDirty ? " *" : "";
            Color labelColor = isDirty ? new Color(1f, 0.75f, 0.2f) : Color.white;

            GUIStyle levelInfoStyle = LevelInfoStyle;
            levelInfoStyle.normal.textColor = labelColor;
            GUILayout.Label($"{_selectedLevel.name} (Level {_selectedLevel.LevelNumber}){dirty}", levelInfoStyle);

            GUILayout.Space(6f);

            if (GUILayout.Button("Save Level", EditorStyles.toolbarButton, GUILayout.Width(75f)))
            {
                SaveSelectedLevel();
            }

            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("Delete Level", EditorStyles.toolbarButton, GUILayout.Width(80f)))
            {
                if (EditorUtility.DisplayDialog("Delete Level", $"Are you sure you want to delete '{_selectedLevel.name}' permanently?", "Delete", "Cancel"))
                {
                    _levelToDelete = _selectedLevel;
                }
            }
            GUI.backgroundColor = prevBg;

            if (GUILayout.Button("Ping in Project", EditorStyles.toolbarButton, GUILayout.Width(90f)))
            {
                EditorGUIUtility.PingObject(_selectedLevel);
                Selection.activeObject = _selectedLevel;
            }
        }
        else
        {
            GUILayout.Label("No Level Selected", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawLeftExplorer(Rect panelRect)
    {
        GUILayout.BeginArea(panelRect, GUI.skin.box);
        EditorGUILayout.BeginVertical();
        EditorGUILayout.Space(2f);

        EditorGUILayout.LabelField("Levels", EditorStyles.boldLabel);

        if (GUILayout.Button("New Level", GUILayout.Height(26f), GUILayout.ExpandWidth(true)))
        {
            CreateNewLevel();
        }

        EditorGUILayout.Space(3f);

        EditorGUILayout.BeginHorizontal();
        _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField, GUILayout.ExpandWidth(true));
        if (!string.IsNullOrEmpty(_searchFilter))
        {
            if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.Width(18f)))
            {
                _searchFilter = string.Empty;
                GUI.FocusControl(null);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(3f);

        _levelsScrollPos = EditorGUILayout.BeginScrollView(
            _levelsScrollPos,
            false,
            false,
            GUIStyle.none,
            GUI.skin.verticalScrollbar,
            GUIStyle.none
        );

        EditorGUILayout.BeginVertical();

        bool hasSearch = !string.IsNullOrEmpty(_searchFilter);
        GUIStyle levelBtnStyle = LevelBtnStyle;

        for (int i = 0; i < _allLevels.Count; i++)
        {
            LevelDataSO level = _allLevels[i];
            if (level == null) continue;

            if (hasSearch &&
                level.name.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            bool isSelected = _selectedLevel == level;
            bool isDirty = EditorUtility.IsDirty(level);

            Color prevBg = GUI.backgroundColor;
            if (isSelected)
            {
                GUI.backgroundColor = new Color(0.35f, 0.65f, 1f, 1f);
            }

            int pCount = level.Passengers?.Count ?? 0;
            int bCount = level.Buses?.Count ?? 0;
            string buttonText = $"{level.name}{(isDirty ? " *" : "")}\n{pCount} Pass • {bCount} Buses";

            levelBtnStyle.fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal;

            if (GUILayout.Button(buttonText, levelBtnStyle, GUILayout.Height(40f), GUILayout.ExpandWidth(true)))
            {
                SelectLevel(level);
            }

            GUI.backgroundColor = prevBg;
            EditorGUILayout.Space(2f);
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawMiddleCanvas(Rect middleRect)
    {
        GUILayout.BeginArea(middleRect);
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        if (_selectedLevel == null)
        {
            EditorGUILayout.HelpBox("Select a level from the left panel or click 'New Level' to start designing.", MessageType.Info);
            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
            return;
        }

        int extraRows = DrawToolsAndPalette(middleRect.width);

        EditorGUILayout.Space(5f);

        DrawAdaptiveGrid(middleRect.width, middleRect.height, extraRows);

        EditorGUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private int DrawToolsAndPalette(float middleWidth)
    {
        float innerWidth = Mathf.Max(220f, middleWidth - 28f);
        int extraVerticalRows = 0;

        EditorGUILayout.BeginVertical(ToolsBoxStyle);

        if (innerWidth >= 720f)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Tool:", CenterLabelStyle, GUILayout.Width(42f), GUILayout.Height(26f));
            _selectedToolIndex = GUILayout.Toolbar(_selectedToolIndex, _toolLabels, EditorStyles.miniButton, GUILayout.Height(26f));
            GUILayout.FlexibleSpace();
            DrawBatchActionButtons(false);
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Tool:", CenterLabelStyle, GUILayout.Width(42f), GUILayout.Height(26f));
            if (innerWidth >= 380f)
            {
                _selectedToolIndex = GUILayout.Toolbar(_selectedToolIndex, _toolLabels, EditorStyles.miniButton, GUILayout.Height(26f), GUILayout.ExpandWidth(true));
            }
            else
            {
                _selectedToolIndex = GUILayout.SelectionGrid(_selectedToolIndex, _toolLabels, 2, EditorStyles.miniButton, GUILayout.Height(52f), GUILayout.ExpandWidth(true));
                extraVerticalRows++;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4f);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Grid:", CenterLabelStyle, GUILayout.Width(42f), GUILayout.Height(24f));
            DrawBatchActionButtons(true);
            EditorGUILayout.EndHorizontal();
            extraVerticalRows++;
        }

        EditorGUILayout.Space(5f);

        const float labelColWidth = 42f;
        const float btnGap = 5f;
        float paletteAreaWidth = Mathf.Max(160f, innerWidth - labelColWidth - 4f);
        int colsPerRow = paletteAreaWidth >= 680f ? 10 : (paletteAreaWidth >= 320f ? 5 : 2);
        float btnWidth = Mathf.Floor((paletteAreaWidth - (colsPerRow - 1) * btnGap) / colsPerRow);

        for (int i = 0; i < AllColors.Length; i++)
        {
            if (i % colsPerRow == 0)
            {
                if (i > 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space(4f);
                    extraVerticalRows++;
                }

                EditorGUILayout.BeginHorizontal();
                if (i == 0)
                {
                    GUILayout.Label("Color:", CenterLabelStyle, GUILayout.Width(labelColWidth), GUILayout.Height(ToolbarItemHeight));
                }
                else
                {
                    GUILayout.Space(labelColWidth + 4f);
                }
            }

            GameColor color = AllColors[i];
            int colorIndex = (int)color;
            int count = (colorIndex >= 0 && colorIndex < _passengerCountsByColor.Length) ? _passengerCountsByColor[colorIndex] : 0;
            string labelText = $"{color} ({count})";

            DrawColorButton(color, labelText, btnWidth);

            if ((i % colsPerRow) < colsPerRow - 1 && i < AllColors.Length - 1)
            {
                GUILayout.Space(btnGap);
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5f);
        extraVerticalRows += DrawPassengerTypeRow(labelColWidth);

        EditorGUILayout.EndVertical();

        return extraVerticalRows;
    }

    private int DrawPassengerTypeRow(float labelColWidth)
    {
        int addedRows = 1;
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Type:", CenterLabelStyle, GUILayout.Width(labelColWidth), GUILayout.Height(ToolbarItemHeight));

        DrawTypeOptionButton(null, "Normal", new Color(0.55f, 0.62f, 0.72f, 1f));

        for (int i = 0; i < _passengerTypes.Count; i++)
        {
            PassengerTypeSO type = _passengerTypes[i];
            if (type == null) continue;

            GUILayout.Space(4f);
            string btnLabel = $"{type.DisplayName} [{type.EditorShortTag}]";
            DrawTypeOptionButton(type, btnLabel, type.EditorTagColor);
        }

        if (_isTypePaintMode && _activePassengerType != null && _activePassengerType.HasCustomParameter)
        {
            GUILayout.Space(10f);
            GUILayout.Label($"{_activePassengerType.ParameterLabel}:", CenterLabelStyle, GUILayout.ExpandWidth(false), GUILayout.Height(ToolbarItemHeight));
            int newVal = EditorGUILayout.IntField(_activeTypeParameter, GUILayout.Width(42f), GUILayout.Height(ToolbarItemHeight - 2f));
            _activeTypeParameter = _activePassengerType.ClampParameter(newVal);
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        return addedRows;
    }

    private void DrawTypeOptionButton(PassengerTypeSO type, string labelText, Color accentColor)
    {
        bool isSelected = _selectedToolIndex == 0 && _isTypePaintMode && _activePassengerType == type;
        GUIContent content = new GUIContent(labelText);
        float calcWidth = Mathf.Max(68f, ColorBtnLabelStyle.CalcSize(content).x + 18f);

        Rect rawRect = GUILayoutUtility.GetRect(calcWidth, ToolbarItemHeight, GUIStyle.none, GUILayout.Width(calcWidth), GUILayout.Height(ToolbarItemHeight));
        Rect rect = new Rect(
            Mathf.Round(rawRect.x),
            Mathf.Round(rawRect.y + 1f),
            Mathf.Round(rawRect.width),
            Mathf.Round(rawRect.height)
        );

        EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

        if (Event.current.type == EventType.Repaint)
        {
            Texture2D whiteTex = EditorGUIUtility.whiteTexture;

            if (isSelected)
            {
                GUI.DrawTexture(rect, whiteTex, ScaleMode.StretchToFill, true, 0f, Color.white, 0f, 5f);
                Rect innerRect = new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f);
                Color selectedBg = Color.Lerp(accentColor, new Color(0.15f, 0.17f, 0.22f, 1f), 0.35f);
                GUI.DrawTexture(innerRect, whiteTex, ScaleMode.StretchToFill, true, 0f, selectedBg, 0f, 3.5f);
            }
            else
            {
                Color matteBg = new Color(0.18f, 0.19f, 0.22f, 1f);
                Color fillColor = Color.Lerp(matteBg, accentColor, 0.25f);
                Color borderColor = new Color(0.08f, 0.08f, 0.10f, 0.65f);

                GUI.DrawTexture(rect, whiteTex, ScaleMode.StretchToFill, true, 0f, borderColor, 0f, 5f);
                Rect innerRect = new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f);
                GUI.DrawTexture(innerRect, whiteTex, ScaleMode.StretchToFill, true, 0f, fillColor, 0f, 4f);
            }
        }

        GUIStyle style = ColorBtnLabelStyle;
        style.normal.textColor = isSelected ? Color.white : new Color(0.92f, 0.92f, 0.95f, 0.92f);
        GUI.Label(rect, labelText, style);

        if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
        {
            _activePassengerType = type;
            _isTypePaintMode = true;
            if (type != null && type.HasCustomParameter)
            {
                _activeTypeParameter = type.ClampParameter(_activeTypeParameter);
            }
            _selectedToolIndex = 0;
            Repaint();
        }
    }

    private void DrawBatchActionButtons(bool expandToFit)
    {
        float height = expandToFit ? 24f : 26f;

        if (expandToFit)
        {
            if (GUILayout.Button("Fill All Tiles", EditorStyles.miniButton, GUILayout.Height(height), GUILayout.ExpandWidth(true)))
            {
                FillAllGridCells();
            }
            GUILayout.Space(4f);

            if (GUILayout.Button("Clear Passengers", EditorStyles.miniButton, GUILayout.Height(height), GUILayout.ExpandWidth(true)))
            {
                ClearAllPassengers();
            }
            GUILayout.Space(4f);

            if (GUILayout.Button("Clear Entire Grid", EditorStyles.miniButton, GUILayout.Height(height), GUILayout.ExpandWidth(true)))
            {
                if (EditorUtility.DisplayDialog("Clear Grid", "Reset all cells and passengers for this level?", "Clear", "Cancel"))
                {
                    ClearAllCells();
                }
            }
        }
        else
        {
            if (GUILayout.Button("Fill All Tiles", EditorStyles.miniButton, GUILayout.Height(height), GUILayout.Width(85f)))
            {
                FillAllGridCells();
            }
            GUILayout.Space(4f);

            if (GUILayout.Button("Clear Passengers", EditorStyles.miniButton, GUILayout.Height(height), GUILayout.Width(115f)))
            {
                ClearAllPassengers();
            }
            GUILayout.Space(4f);

            if (GUILayout.Button("Clear Entire Grid", EditorStyles.miniButton, GUILayout.Height(height), GUILayout.Width(110f)))
            {
                if (EditorUtility.DisplayDialog("Clear Grid", "Reset all cells and passengers for this level?", "Clear", "Cancel"))
                {
                    ClearAllCells();
                }
            }
        }
    }

    private void DrawColorButton(GameColor color, string labelText, float btnWidth)
    {
        bool isSelected = _selectedToolIndex == 0 && !_isTypePaintMode && _activeColor == color;
        Color unityColor = color.ToUnityColor();

        Rect rawRect = GUILayoutUtility.GetRect(btnWidth, ToolbarItemHeight, GUIStyle.none, GUILayout.Width(btnWidth), GUILayout.Height(ToolbarItemHeight));
        Rect rect = new Rect(
            Mathf.Round(rawRect.x),
            Mathf.Round(rawRect.y + 1f),
            Mathf.Round(rawRect.width),
            Mathf.Round(rawRect.height)
        );

        EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

        if (Event.current.type == EventType.Repaint)
        {
            Texture2D whiteTex = EditorGUIUtility.whiteTexture;

            if (isSelected)
            {
                GUI.DrawTexture(rect, whiteTex, ScaleMode.StretchToFill, true, 0f, Color.white, 0f, 5f);
                Rect innerRect = new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f);
                GUI.DrawTexture(innerRect, whiteTex, ScaleMode.StretchToFill, true, 0f, unityColor, 0f, 3.5f);
            }
            else
            {
                Color matteBg = new Color(0.16f, 0.16f, 0.18f, 1f);
                Color fillColor = Color.Lerp(unityColor, matteBg, 0.18f);
                Color borderColor = new Color(0.08f, 0.08f, 0.10f, 0.65f);

                GUI.DrawTexture(rect, whiteTex, ScaleMode.StretchToFill, true, 0f, borderColor, 0f, 5f);
                Rect innerRect = new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f);
                GUI.DrawTexture(innerRect, whiteTex, ScaleMode.StretchToFill, true, 0f, fillColor, 0f, 4f);
            }
        }

        Color textCol = GetLuminance(unityColor) > 0.55f
            ? new Color(0.08f, 0.08f, 0.08f, isSelected ? 1f : 0.9f)
            : new Color(1f, 1f, 1f, isSelected ? 1f : 0.92f);

        GUIStyle style = ColorBtnLabelStyle;
        style.normal.textColor = textCol;
        GUI.Label(rect, labelText, style);

        if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
        {
            _activeColor = color;
            _isTypePaintMode = false;
            _selectedToolIndex = 0;
            Repaint();
        }
    }

    private void DrawAdaptiveGrid(float middleWidth, float middleHeight, int extraToolbarRows)
    {
        Vector2Int size = _selectedLevel.GridSize;
        if (size.x <= 0 || size.y <= 0)
        {
            EditorGUILayout.HelpBox("Grid Size must be greater than 0.", MessageType.Warning);
            return;
        }

        EditorGUILayout.HelpBox("Left Click: Apply / Paint | Right Click: Erase | Click & Drag: Multi-Paint", MessageType.None);

        _gridScrollPos = EditorGUILayout.BeginScrollView(_gridScrollPos);

        float availableWidth = Mathf.Max(180f, middleWidth - 36f);
        float availableHeight = Mathf.Max(180f, middleHeight - 145f - extraToolbarRows * 30f);

        float calcSizeX = (availableWidth - (size.x - 1) * CellSpacing) / size.x;
        float calcSizeY = (availableHeight - (size.y - 1) * CellSpacing) / size.y;
        float dynamicCellSize = Mathf.Clamp(Mathf.Min(calcSizeX, calcSizeY), 30f, 74f);

        float totalGridWidth = size.x * (dynamicCellSize + CellSpacing) - CellSpacing;
        float totalGridHeight = size.y * (dynamicCellSize + CellSpacing) - CellSpacing;

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        EditorGUILayout.BeginVertical("box");

        Rect gridRect = GUILayoutUtility.GetRect(totalGridWidth, totalGridHeight, GUIStyle.none, GUILayout.Width(totalGridWidth), GUILayout.Height(totalGridHeight));

        Event currentEvent = Event.current;
        EventType eventType = currentEvent.type;
        bool isRepaint = eventType == EventType.Repaint;
        bool isMouse = currentEvent.isMouse;

        if (isRepaint || isMouse)
        {
            float step = dynamicCellSize + CellSpacing;
            float padding = dynamicCellSize * 0.08f;
            float paxSize = dynamicCellSize - padding * 2f;
            Color activeCellColor = new Color(0.32f, 0.38f, 0.45f);
            Color inactiveCellColor = new Color(0.20f, 0.20f, 0.20f);
            Color cellOutlineColor = new Color(0.12f, 0.12f, 0.12f, 0.8f);
            Color paxOutlineColor = Color.white * 0.95f;

            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    Vector2Int coord = new Vector2Int(x, y);
                    Rect cellRect = new Rect(
                        gridRect.x + x * step,
                        gridRect.y + y * step,
                        dynamicCellSize,
                        dynamicCellSize
                    );

                    if (isRepaint)
                    {
                        bool isActive = IsCellActive(coord);
                        PassengerData passenger = GetPassenger(coord);

                        EditorGUI.DrawRect(cellRect, isActive ? activeCellColor : inactiveCellColor);
                        DrawRectOutline(cellRect, cellOutlineColor, 1f);

                        if (passenger != null && isActive)
                        {
                            Rect paxRect = new Rect(cellRect.x + padding, cellRect.y + padding, paxSize, paxSize);
                            EditorGUI.DrawRect(paxRect, passenger.Color.ToUnityColor());

                            if (passenger.Type != null)
                            {
                                float badgeHeight = Mathf.Round(Mathf.Clamp(paxSize * 0.34f, 13f, 18f));
                                float badgeY = Mathf.Round(paxRect.y + (paxSize - badgeHeight) * 0.5f);
                                Rect badgeRect = new Rect(
                                    paxRect.x,
                                    badgeY,
                                    paxRect.width,
                                    badgeHeight
                                );

                                EditorGUI.DrawRect(badgeRect, new Color(0.10f, 0.12f, 0.16f, 0.82f));

                                Color lineCol = Color.Lerp(passenger.Type.EditorTagColor, Color.white, 0.25f);
                                lineCol.a = 0.75f;
                                EditorGUI.DrawRect(new Rect(badgeRect.x, badgeRect.y, badgeRect.width, 1f), lineCol);
                                EditorGUI.DrawRect(new Rect(badgeRect.x, badgeRect.yMax - 1f, badgeRect.width, 1f), lineCol);

                                GUIStyle badgeStyle = CellBadgeStyle;
                                badgeStyle.normal.textColor = Color.Lerp(passenger.Type.EditorTagColor, Color.white, 0.35f);
                                GUI.Label(badgeRect, passenger.Type.GetEditorCellLabel(passenger.Parameter), badgeStyle);
                            }

                            DrawRectOutline(paxRect, paxOutlineColor, 1.5f);
                        }
                    }

                    if (isMouse && cellRect.Contains(currentEvent.mousePosition))
                    {
                        if (eventType == EventType.MouseDown)
                        {
                            _isDragging = true;
                            _lastInteractedCoord = coord;

                            if (currentEvent.button == 0)
                            {
                                ApplyToolToCell(coord);
                            }
                            else if (currentEvent.button == 1)
                            {
                                QuickErase(coord);
                            }
                            currentEvent.Use();
                        }
                        else if (eventType == EventType.MouseDrag && _isDragging)
                        {
                            if (_lastInteractedCoord != coord)
                            {
                                _lastInteractedCoord = coord;
                                if (currentEvent.button == 0)
                                {
                                    ApplyToolToCell(coord);
                                }
                                else if (currentEvent.button == 1)
                                {
                                    QuickErase(coord);
                                }
                                currentEvent.Use();
                            }
                        }
                    }
                }
            }
        }

        EditorGUILayout.EndVertical();

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();
    }

    private void ApplyToolToCell(Vector2Int coord)
    {
        Undo.RecordObject(_selectedLevel, "Edit Level Cell");

        switch (_selectedToolIndex)
        {
            case 0:
                if (_isTypePaintMode)
                {
                    SetPassengerTypeOnly(coord, _activePassengerType, _activeTypeParameter);
                }
                else
                {
                    EnsureCellActive(coord);
                    SetPassengerColorOnly(coord, _activeColor);
                }
                break;

            case 1:
                ToggleCellActive(coord);
                break;

            case 2:
                RemovePassenger(coord);
                break;

            case 3:
                RemoveCellAndPassenger(coord);
                break;
        }

        RebuildLevelCache();
        MarkSelectedDirty();
    }

    private void QuickErase(Vector2Int coord)
    {
        Undo.RecordObject(_selectedLevel, "Erase Cell");

        if (GetPassenger(coord) != null)
        {
            RemovePassenger(coord);
        }
        else
        {
            RemoveCellAndPassenger(coord);
        }

        RebuildLevelCache();
        MarkSelectedDirty();
    }

    private void DrawRightPanel(Rect panelRect)
    {
        GUILayout.BeginArea(panelRect, GUI.skin.box);

        if (_selectedLevel == null)
        {
            GUILayout.EndArea();
            return;
        }

        float prevLabelWidth = EditorGUIUtility.labelWidth;
        float labelWidth = Mathf.Floor(Mathf.Clamp(panelRect.width * 0.33f, 80f, 100f));
        EditorGUIUtility.labelWidth = labelWidth;

        _rightPanelScrollPos = EditorGUILayout.BeginScrollView(
            _rightPanelScrollPos,
            false,
            false,
            GUIStyle.none,
            GUI.skin.verticalScrollbar,
            GUIStyle.none
        );

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        EditorGUILayout.Space(2f);

        GUILayout.Label("Level Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUI.BeginChangeCheck();
        int levelNum = EditorGUILayout.IntField("Level Number", _selectedLevel.LevelNumber);
        float timeLimit = EditorGUILayout.FloatField("Time Limit (s)", _selectedLevel.TimeLimit);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Grid Size", GUILayout.Width(labelWidth - 2f));
        EditorGUIUtility.labelWidth = 13f;
        int gridX = EditorGUILayout.IntField("X", _selectedLevel.GridSize.x, GUILayout.MinWidth(32f), GUILayout.ExpandWidth(true));
        GUILayout.Space(4f);
        int gridY = EditorGUILayout.IntField("Y", _selectedLevel.GridSize.y, GUILayout.MinWidth(32f), GUILayout.ExpandWidth(true));
        EditorGUIUtility.labelWidth = labelWidth;
        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_selectedLevel, "Change Level Settings");

            if (float.IsNaN(timeLimit) || float.IsInfinity(timeLimit))
            {
                timeLimit = GameConstants.MinTimeLimit;
            }

            int clampedLevelNum = Mathf.Max(1, levelNum);
            bool levelNumberChanged = _selectedLevel.LevelNumber != clampedLevelNum;

            _selectedLevel.LevelNumber = clampedLevelNum;
            _selectedLevel.TimeLimit = Mathf.Max(GameConstants.MinTimeLimit, timeLimit);

            Vector2Int clampedSize = new Vector2Int(
                Mathf.Clamp(gridX, 1, GameConstants.MaxGridSize),
                Mathf.Clamp(gridY, 1, GameConstants.MaxGridSize)
            );
            _selectedLevel.GridSize = clampedSize;

            RemoveInvalidData();
            RebuildLevelCache();

            if (levelNumberChanged)
            {
                _allLevels.Sort(CompareLevelsByNumber);
            }

            MarkSelectedDirty();
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6f);

        DrawBalanceSection();

        EditorGUILayout.Space(6f);

        DrawBusesSection();

        EditorGUILayout.EndVertical();
        GUILayout.Space(4f);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();

        EditorGUIUtility.labelWidth = prevLabelWidth;

        GUILayout.EndArea();
    }

    private void DrawBalanceSection()
    {
        GUILayout.Label("Balance & Validation", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        int busCap = GameConstants.BusCapacity;
        int totalPax = _selectedLevel.Passengers.Count;
        int totalRequiredSeats = 0;
        for (int i = 0; i < _passengerSeatDemandByColor.Length; i++)
        {
            totalRequiredSeats += _passengerSeatDemandByColor[i];
        }
        int totalSeats = _selectedLevel.Buses.Count * busCap;

        bool isBalanced = true;
        for (int i = 0; i < _activeBalanceColors.Count; i++)
        {
            int colorIndex = (int)_activeBalanceColors[i];
            if (_passengerSeatDemandByColor[colorIndex] != _busCountsByColor[colorIndex] * busCap)
            {
                isBalanced = false;
                break;
            }
        }

        if (totalPax == 0 && _selectedLevel.Buses.Count == 0)
        {
            EditorGUILayout.HelpBox("Grid is empty. Paint passengers or add buses.", MessageType.Info);
        }
        else if (isBalanced)
        {
            EditorGUILayout.HelpBox($"Level is Balanced! ({totalRequiredSeats} Req Seats / {totalSeats} Seats)", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox($"Seat Mismatch! ({totalRequiredSeats} Req Seats vs {totalSeats} Seats)", MessageType.Warning);
        }

        EditorGUILayout.Space(3f);

        bool isRepaint = Event.current.type == EventType.Repaint;

        for (int i = 0; i < _activeBalanceColors.Count; i++)
        {
            GameColor c = _activeBalanceColors[i];
            int colorIndex = (int)c;
            int pDemand = _passengerSeatDemandByColor[colorIndex];
            int b = _busCountsByColor[colorIndex];
            int seats = b * busCap;
            int diff = seats - pDemand;

            EditorGUILayout.BeginHorizontal();

            Rect bulletRect = GUILayoutUtility.GetRect(10f, 10f, GUILayout.ExpandWidth(false));
            bulletRect.y += 4f;
            if (isRepaint)
            {
                EditorGUI.DrawRect(bulletRect, c.ToUnityColor());
            }

            GUILayout.Space(4f);

            GUILayout.Label(c.ToString(), EditorStyles.boldLabel, GUILayout.Width(48f));

            string detailText = $"{pDemand} Seat → {b} Bus ({seats}s)";
            GUILayout.Label(detailText, EditorStyles.label);

            GUILayout.FlexibleSpace();

            if (diff == 0)
            {
                GUI.color = Color.green;
                GUILayout.Label("OK", RightStatusStyle, GUILayout.Width(26f));
            }
            else if (diff > 0)
            {
                GUI.color = Color.yellow;
                GUILayout.Label($"+{diff}s", RightStatusStyle, GUILayout.Width(34f));
            }
            else
            {
                GUI.color = new Color(1f, 0.4f, 0.4f);
                GUILayout.Label($"{diff}p!", RightStatusStyle, GUILayout.Width(34f));
            }
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawBusesSection()
    {
        int busCount = _selectedLevel.Buses.Count;

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label($"Bus Queue ({busCount})", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Add Bus", EditorStyles.miniButton, GUILayout.Width(66f)))
        {
            _shouldAddBus = true;
        }
        GUILayout.Space(2f);
        if (GUILayout.Button("Clear", EditorStyles.miniButton, GUILayout.Width(44f)))
        {
            if (EditorUtility.DisplayDialog("Clear Buses", "Remove all buses in queue?", "Clear", "Cancel"))
            {
                _shouldClearBuses = true;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2f);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Auto-Buses", EditorStyles.miniButton, GUILayout.Height(22f), GUILayout.ExpandWidth(true)))
        {
            _shouldAutoBuses = true;
        }
        GUILayout.Space(4f);
        if (GUILayout.Button("Shuffle", EditorStyles.miniButton, GUILayout.MinWidth(66f), GUILayout.MaxWidth(82f), GUILayout.Height(22f)))
        {
            _shouldShuffleBuses = true;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4f);

        if (busCount == 0)
        {
            EditorGUILayout.HelpBox("No buses added. Click '+ Add Bus' or 'Auto-Buses'.", MessageType.None);
        }
        else
        {
            EnsureBusReorderableList();
            _busReorderableList.DoLayoutList();
        }
    }

    private void EnsureBusReorderableList()
    {
        if (_selectedLevel == null) return;

        if (_busReorderableList != null &&
            _busListBoundLevel == _selectedLevel &&
            _busReorderableList.list == _selectedLevel.Buses)
        {
            return;
        }

        _busListBoundLevel = _selectedLevel;
        _busReorderableList = new ReorderableList(_selectedLevel.Buses, typeof(BusData), true, false, false, false)
        {
            headerHeight = 0f,
            footerHeight = 0f,
            elementHeight = 60f,
            showDefaultBackground = false,
            drawElementBackgroundCallback = DrawBusElementBackground,
            drawElementCallback = DrawBusElement,
            onReorderCallbackWithDetails = OnBusReorderedWithDetails
        };
    }

    private void DrawBusElementBackground(Rect rect, int index, bool isActive, bool isFocused)
    {
        if (Event.current.type != EventType.Repaint) return;

        if (isActive || isFocused)
        {
            Rect highlightRect = new Rect(rect.x, rect.y + 1f, rect.width, rect.height - 4f);
            EditorGUI.DrawRect(highlightRect, new Color(0.24f, 0.48f, 0.90f, 0.22f));
        }
    }

    private void DrawBusElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        if (_selectedLevel == null || index < 0 || index >= _selectedLevel.Buses.Count) return;

        BusData bus = _selectedLevel.Buses[index];
        if (bus == null) return;

        Rect cardRect = new Rect(rect.x, rect.y + 1f, rect.width, rect.height - 4f);
        GUI.Box(cardRect, GUIContent.none, EditorStyles.helpBox);

        Rect stripeRect = new Rect(cardRect.x + 4f, cardRect.y + 4f, Mathf.Max(10f, cardRect.width - 8f), 4f);
        if (Event.current.type == EventType.Repaint)
        {
            EditorGUI.DrawRect(stripeRect, bus.Color.ToUnityColor());
        }

        float row1Y = stripeRect.yMax + 4f;
        Rect titleRect = new Rect(cardRect.x + 6f, row1Y, Mathf.Max(20f, cardRect.width - 36f), 18f);
        GUI.Label(titleRect, $"Bus {index + 1}", EditorStyles.boldLabel);

        Rect removeBtnRect = new Rect(cardRect.xMax - 28f, row1Y, 22f, 18f);
        if (GUI.Button(removeBtnRect, "X", EditorStyles.miniButton))
        {
            _busToRemove = index;
        }

        float row2Y = row1Y + 21f;
        Rect colorRect = new Rect(cardRect.x + 6f, row2Y, Mathf.Max(40f, cardRect.width - 12f), 18f);

        float prevLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = Mathf.Floor(Mathf.Clamp(cardRect.width * 0.32f, 55f, 85f));

        EditorGUI.BeginChangeCheck();
        GameColor newColor = (GameColor)EditorGUI.EnumPopup(colorRect, "Color", bus.Color);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_selectedLevel, "Change Bus Color");
            bus.Color = newColor;
            RebuildLevelCache();
            MarkSelectedDirty();
        }

        EditorGUIUtility.labelWidth = prevLabelWidth;
    }

    private void OnBusReorderedWithDetails(ReorderableList list, int oldIndex, int newIndex)
    {
        if (_selectedLevel == null ||
            oldIndex == newIndex ||
            oldIndex < 0 || oldIndex >= _selectedLevel.Buses.Count ||
            newIndex < 0 || newIndex >= _selectedLevel.Buses.Count)
        {
            return;
        }

        // Restore pre-drag order temporarily so Undo snapshots the original list order
        BusData movedBus = _selectedLevel.Buses[newIndex];
        _selectedLevel.Buses.RemoveAt(newIndex);
        _selectedLevel.Buses.Insert(oldIndex, movedBus);

        Undo.RecordObject(_selectedLevel, "Reorder Buses");

        _selectedLevel.Buses.RemoveAt(oldIndex);
        _selectedLevel.Buses.Insert(newIndex, movedBus);

        RebuildLevelCache();
        MarkSelectedDirty();
    }

    private void AutoGenerateBuses()
    {
        RebuildLevelCache();

        Undo.RecordObject(_selectedLevel, "Auto-Generate Buses");
        _selectedLevel.Buses.Clear();

        int busCap = GameConstants.BusCapacity;

        for (int i = 0; i < AllColors.Length; i++)
        {
            int seatDemand = _passengerSeatDemandByColor[i];
            if (seatDemand <= 0) continue;

            GameColor color = AllColors[i];
            int neededBuses = Mathf.CeilToInt((float)seatDemand / busCap);
            for (int b = 0; b < neededBuses; b++)
            {
                _selectedLevel.Buses.Add(new BusData { Color = color });
            }
        }

        MarkSelectedDirty();
    }

    private void ShuffleBuses()
    {
        if (_selectedLevel.Buses.Count <= 1) return;

        Undo.RecordObject(_selectedLevel, "Shuffle Buses");
        int n = _selectedLevel.Buses.Count;
        while (n > 1)
        {
            n--;
            int k = s_Rng.Next(n + 1);
            BusData val = _selectedLevel.Buses[k];
            _selectedLevel.Buses[k] = _selectedLevel.Buses[n];
            _selectedLevel.Buses[n] = val;
        }

        MarkSelectedDirty();
    }

    private void FillAllGridCells()
    {
        Undo.RecordObject(_selectedLevel, "Fill All Cells");
        for (int y = 0; y < _selectedLevel.GridSize.y; y++)
        {
            for (int x = 0; x < _selectedLevel.GridSize.x; x++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                if (!IsCellActive(coord))
                {
                    _selectedLevel.GridCells.Add(new GridCellData { Coordinates = coord });
                    _activeCellLookup.Add(coord);
                }
            }
        }
        MarkSelectedDirty();
    }

    private void ClearAllPassengers()
    {
        if (_selectedLevel.Passengers.Count == 0) return;
        Undo.RecordObject(_selectedLevel, "Clear Passengers");
        _selectedLevel.Passengers.Clear();
        RebuildLevelCache();
        MarkSelectedDirty();
    }

    private void ClearAllCells()
    {
        Undo.RecordObject(_selectedLevel, "Clear Grid");
        _selectedLevel.GridCells.Clear();
        _selectedLevel.Passengers.Clear();
        RebuildLevelCache();
        MarkSelectedDirty();
    }

    private bool IsCellActive(Vector2Int coordinates)
    {
        return _selectedLevel != null && _activeCellLookup.Contains(coordinates);
    }

    private void EnsureCellActive(Vector2Int coordinates)
    {
        if (!IsCellActive(coordinates))
        {
            _selectedLevel.GridCells.Add(new GridCellData { Coordinates = coordinates });
            _activeCellLookup.Add(coordinates);
        }
    }

    private void ToggleCellActive(Vector2Int coordinates)
    {
        if (IsCellActive(coordinates))
        {
            RemoveCellAndPassenger(coordinates);
        }
        else
        {
            _selectedLevel.GridCells.Add(new GridCellData { Coordinates = coordinates });
            _activeCellLookup.Add(coordinates);
        }
    }

    private void RemoveCellAndPassenger(Vector2Int coordinates)
    {
        List<GridCellData> cells = _selectedLevel.GridCells;
        for (int i = cells.Count - 1; i >= 0; i--)
        {
            if (cells[i].Coordinates == coordinates)
            {
                cells.RemoveAt(i);
            }
        }
        _activeCellLookup.Remove(coordinates);

        RemovePassenger(coordinates);
    }

    private PassengerData GetPassenger(Vector2Int coordinates)
    {
        if (_selectedLevel == null) return null;
        _passengerLookup.TryGetValue(coordinates, out PassengerData passenger);
        return passenger;
    }

    private void SetPassengerColorOnly(Vector2Int coordinates, GameColor color)
    {
        PassengerData passenger = GetPassenger(coordinates);

        if (passenger == null)
        {
            passenger = new PassengerData
            {
                Coordinates = coordinates,
                Color = color
            };
            _selectedLevel.Passengers.Add(passenger);
            _passengerLookup[coordinates] = passenger;
        }
        else
        {
            passenger.Color = color;
        }
    }

    private void SetPassengerTypeOnly(Vector2Int coordinates, PassengerTypeSO type, int parameter)
    {
        PassengerData passenger = GetPassenger(coordinates);
        if (passenger == null) return;

        passenger.Type = type;
        passenger.Parameter = type != null && type.HasCustomParameter ? type.ClampParameter(parameter) : 0;
    }

    private void RemovePassenger(Vector2Int coordinates)
    {
        List<PassengerData> passengers = _selectedLevel.Passengers;
        for (int i = passengers.Count - 1; i >= 0; i--)
        {
            if (passengers[i].Coordinates == coordinates)
            {
                passengers.RemoveAt(i);
            }
        }
        _passengerLookup.Remove(coordinates);
    }

    private void RemoveInvalidData()
    {
        if (_selectedLevel == null) return;

        _activeCellLookup.Clear();
        List<GridCellData> cells = _selectedLevel.GridCells;
        for (int i = cells.Count - 1; i >= 0; i--)
        {
            Vector2Int coord = cells[i].Coordinates;
            if (!IsInsideGrid(coord))
            {
                cells.RemoveAt(i);
            }
            else
            {
                _activeCellLookup.Add(coord);
            }
        }

        List<PassengerData> passengers = _selectedLevel.Passengers;
        for (int i = passengers.Count - 1; i >= 0; i--)
        {
            Vector2Int coord = passengers[i].Coordinates;
            if (!IsInsideGrid(coord) || !_activeCellLookup.Contains(coord))
            {
                passengers.RemoveAt(i);
            }
        }
    }

    private void AddBus(GameColor color)
    {
        Undo.RecordObject(_selectedLevel, "Add Bus");
        _selectedLevel.Buses.Add(new BusData { Color = color });
        MarkSelectedDirty();
    }

    private bool IsInsideGrid(Vector2Int coordinates)
    {
        return _selectedLevel != null &&
               coordinates.x >= 0 && coordinates.y >= 0 &&
               coordinates.x < _selectedLevel.GridSize.x && coordinates.y < _selectedLevel.GridSize.y;
    }

    private void MarkSelectedDirty()
    {
        if (_selectedLevel != null)
        {
            EditorUtility.SetDirty(_selectedLevel);
            Repaint();
        }
    }

    private void SaveSelectedLevel()
    {
        if (_selectedLevel != null)
        {
            EditorUtility.SetDirty(_selectedLevel);
            AssetDatabase.SaveAssetIfDirty(_selectedLevel);
            Repaint();
        }
    }

    private static int CompareLevelsByNumber(LevelDataSO a, LevelDataSO b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return 1;
        if (b == null) return -1;
        return a.LevelNumber.CompareTo(b.LevelNumber);
    }

    private void RefreshPassengerTypes()
    {
        EnsureStarterPassengerTypes();

        _passengerTypes.Clear();
        string[] guids = AssetDatabase.FindAssets("t:PassengerTypeSO");

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            PassengerTypeSO type = AssetDatabase.LoadAssetAtPath<PassengerTypeSO>(path);
            if (type != null)
            {
                _passengerTypes.Add(type);
            }
        }

        _passengerTypes.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));

        if (_activePassengerType != null && !_passengerTypes.Contains(_activePassengerType))
        {
            _activePassengerType = null;
        }
    }

    private void EnsureStarterPassengerTypes()
    {
        if (Directory.Exists(PassengerTypesDirectory)) return;

        Directory.CreateDirectory(PassengerTypesDirectory);

        MysteryPassengerTypeSO mystery = CreateInstance<MysteryPassengerTypeSO>();
        mystery.ConfigurePreset(
            displayName: "Mystery",
            shortTag: "?",
            tagColor: new Color(0.85f, 0.55f, 1f, 1f)
        );
        AssetDatabase.CreateAsset(mystery, $"{PassengerTypesDirectory}/MysteryPassenger.asset");

        FrozenPassengerTypeSO frozen = CreateInstance<FrozenPassengerTypeSO>();
        frozen.ConfigurePreset(
            displayName: "Frozen",
            shortTag: "ICE",
            tagColor: new Color(0.35f, 0.85f, 1f, 1f),
            seatRequirement: 1,
            scaleMultiplier: 1f,
            hasParameter: true,
            parameterLabel: "Freeze Moves",
            defaultParameter: 3
        );
        AssetDatabase.CreateAsset(frozen, $"{PassengerTypesDirectory}/FrozenPassenger.asset");

        PassengerTypeSO vip = CreateInstance<PassengerTypeSO>();
        vip.ConfigurePreset(
            displayName: "VIP",
            shortTag: "VIP",
            tagColor: new Color(1f, 0.82f, 0.22f, 1f),
            seatRequirement: 2,
            scaleMultiplier: 1.18f
        );
        AssetDatabase.CreateAsset(vip, $"{PassengerTypesDirectory}/VipPassenger.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void RefreshLevelList()
    {
        EnsureDirectoryExists();

        _allLevels.Clear();
        string[] guids = AssetDatabase.FindAssets("t:LevelDataSO", new[] { LevelsDirectory });

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            LevelDataSO level = AssetDatabase.LoadAssetAtPath<LevelDataSO>(path);
            if (level != null)
            {
                EnsureLevelDataLists(level);
                _allLevels.Add(level);
            }
        }

        _allLevels.Sort(CompareLevelsByNumber);

        if (_selectedLevel == null && _allLevels.Count > 0)
        {
            SelectLevel(_allLevels[0]);
        }
    }

    public void SelectLevel(LevelDataSO level)
    {
        _selectedLevel = level;
        _busReorderableList = null;
        _busListBoundLevel = null;
        EnsureLevelDataLists(_selectedLevel);
        RebuildLevelCache();
        ResetDeferredBusFlags();
        Repaint();
    }

    private void CreateNewLevel()
    {
        EnsureDirectoryExists();

        int nextNumber = 1;
        for (int i = 0; i < _allLevels.Count; i++)
        {
            LevelDataSO lvl = _allLevels[i];
            if (lvl != null && lvl.LevelNumber >= nextNumber)
            {
                nextNumber = lvl.LevelNumber + 1;
            }
        }

        string assetPath = $"{LevelsDirectory}/Level{nextNumber}.asset";
        assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

        LevelDataSO newLevel = CreateInstance<LevelDataSO>();
        newLevel.LevelNumber = nextNumber;
        newLevel.TimeLimit = 60f;
        newLevel.GridSize = new Vector2Int(5, 5);
        EnsureLevelDataLists(newLevel);

        AssetDatabase.CreateAsset(newLevel, assetPath);
        AssetDatabase.SaveAssetIfDirty(newLevel);

        RefreshLevelList();
        SelectLevel(newLevel);
    }

    private void DeleteLevel(LevelDataSO level)
    {
        string path = AssetDatabase.GetAssetPath(level);
        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();

            if (_selectedLevel == level)
            {
                _selectedLevel = null;
            }

            RefreshLevelList();
        }
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(LevelsDirectory))
        {
            Directory.CreateDirectory(LevelsDirectory);
            AssetDatabase.Refresh();
        }
    }

    private void DrawRectOutline(Rect rect, Color color, float thickness)
    {
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    private float GetLuminance(Color color)
    {
        return 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
    }
}
