using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(LevelData))]
public class LevelDataEditor : Editor
{
    private LevelData _levelData;

    private Vector2 _gridScrollPosition;
    private Vector2 _passengerScrollPosition;

    private SerializedProperty _busesProperty;
    private ReorderableList _busList;
    private int _busRemoveIndex = -1;

    private readonly HashSet<Vector2Int> _activeCellLookup = new HashSet<Vector2Int>();
    private readonly Dictionary<Vector2Int, PassengerData> _passengerLookup = new Dictionary<Vector2Int, PassengerData>();

    private static readonly GUIContent ColorLabel = new GUIContent("Color");
    private static readonly GUIContent RemovePassengerContent = new GUIContent("Remove Passenger");
    private static readonly GUIContent[] ColorMenuContents = CreateColorMenuContents();

    private const float CellSize = 30f;
    private const float CellSpacing = 3f;
    private const float MaxGridHeight = 240f;
    private const float ScrollbarSize = 18f;

    private static GUIContent[] CreateColorMenuContents()
    {
        GameColor[] colors = LevelEditorWindow.AllColors;
        GUIContent[] contents = new GUIContent[colors.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            contents[i] = new GUIContent(colors[i].ToString());
        }
        return contents;
    }

    private void OnEnable()
    {
        _levelData = (LevelData)target;
        EnsureLevelDataLists();
        _busesProperty = serializedObject.FindProperty("Buses");
        Undo.undoRedoPerformed += Repaint;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= Repaint;
    }

    public override void OnInspectorGUI()
    {
        EnsureLevelDataLists();
        RebuildLookupCache();

        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.25f, 0.75f, 1f, 1f);
        if (GUILayout.Button("🎨 Open in Level Editor Window", GUILayout.Height(30f)))
        {
            LevelEditorWindow.OpenWithLevel(_levelData);
        }
        GUI.backgroundColor = prevBg;

        EditorGUILayout.Space(5f);

        DrawLevelSettings();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Grid Cells", EditorStyles.boldLabel);
        DrawGrid(false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Passengers", EditorStyles.boldLabel);
        DrawGrid(true);

        EditorGUILayout.Space();
        DrawBuses();
    }

    private void EnsureLevelDataLists()
    {
        if (_levelData == null) return;
        if (_levelData.GridCells == null) _levelData.GridCells = new List<GridCellData>();
        if (_levelData.Passengers == null) _levelData.Passengers = new List<PassengerData>();
        if (_levelData.Buses == null) _levelData.Buses = new List<BusData>();
    }

    private void RebuildLookupCache()
    {
        _activeCellLookup.Clear();
        _passengerLookup.Clear();

        if (_levelData == null) return;

        List<GridCellData> cells = _levelData.GridCells;
        for (int i = 0; i < cells.Count; i++)
        {
            GridCellData cell = cells[i];
            if (cell != null)
            {
                _activeCellLookup.Add(cell.Coordinates);
            }
        }

        List<PassengerData> passengers = _levelData.Passengers;
        for (int i = 0; i < passengers.Count; i++)
        {
            PassengerData passenger = passengers[i];
            if (passenger != null)
            {
                _passengerLookup[passenger.Coordinates] = passenger;
            }
        }
    }

    private void DrawLevelSettings()
    {
        EditorGUI.BeginChangeCheck();

        int levelNumber = EditorGUILayout.IntField("Level Number", _levelData.LevelNumber);

        float timeLimit = EditorGUILayout.FloatField("Time Limit", _levelData.TimeLimit);

        Vector2Int gridSize = EditorGUILayout.Vector2IntField("Grid Size", _levelData.GridSize);

        if (!EditorGUI.EndChangeCheck()) return;

        Undo.RecordObject(_levelData, "Change Level Settings");

        if (float.IsNaN(timeLimit) || float.IsInfinity(timeLimit))
        {
            timeLimit = GameConstants.MinTimeLimit;
        }

        _levelData.LevelNumber = Mathf.Max(1, levelNumber);

        _levelData.TimeLimit = Mathf.Max(GameConstants.MinTimeLimit, timeLimit);

        _levelData.GridSize = new Vector2Int(
            Mathf.Clamp(gridSize.x, 1, 10),
            Mathf.Clamp(gridSize.y, 1, 10)
        );

        RemoveInvalidData();
        RebuildLookupCache();
        MarkDataChanged();
    }

    private void DrawGrid(bool isPassengerGrid)
    {
        Vector2Int gridSize = _levelData.GridSize;

        if (gridSize.x <= 0 || gridSize.y <= 0)
        {
            EditorGUILayout.HelpBox("Set Grid Size to display the grid.", MessageType.Info);
            return;
        }

        float step = CellSize + CellSpacing;

        float contentWidth = gridSize.x * step - CellSpacing;
        float contentHeight = gridSize.y * step - CellSpacing;

        float availableWidth = Mathf.Max(CellSize, EditorGUIUtility.currentViewWidth - 40f);

        if (contentHeight > MaxGridHeight)
        {
            availableWidth -= ScrollbarSize;
        }

        bool needsHorizontalScroll = contentWidth > availableWidth;

        float viewHeight = contentHeight;

        if (needsHorizontalScroll)
        {
            viewHeight += ScrollbarSize;
        }

        viewHeight = Mathf.Min(viewHeight, MaxGridHeight);

        Vector2 scrollPosition = isPassengerGrid ? _passengerScrollPosition : _gridScrollPosition;

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(viewHeight), GUILayout.ExpandHeight(false));

        Rect gridRect = GUILayoutUtility.GetRect(contentWidth, contentHeight, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));

        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                Vector2Int coordinates = new Vector2Int(x, y);

                Rect cellRect = new Rect(gridRect.x + x * step, gridRect.y + y * step, CellSize, CellSize);

                if (isPassengerGrid)
                {
                    DrawPassengerCell(cellRect, coordinates);
                }
                else
                {
                    DrawGridCell(cellRect, coordinates);
                }
            }
        }

        EditorGUILayout.EndScrollView();

        if (isPassengerGrid)
        {
            _passengerScrollPosition = scrollPosition;
        }
        else
        {
            _gridScrollPosition = scrollPosition;
        }
    }

    private void DrawGridCell(Rect rect, Vector2Int coordinates)
    {
        bool isActive = IsCellActive(coordinates);

        Color color = isActive ? new Color(0.3f, 0.7f, 0.4f) : new Color(0.25f, 0.25f, 0.25f);

        if (DrawCell(rect, color))
        {
            ToggleGridCell(coordinates);
        }
    }

    private void DrawPassengerCell(Rect rect, Vector2Int coordinates)
    {
        bool isActive = IsCellActive(coordinates);
        PassengerData passenger = GetPassenger(coordinates);

        Color color = new Color(0.15f, 0.15f, 0.15f);

        if (isActive)
        {
            color = passenger != null ? passenger.Color.ToUnityColor() : new Color(0.4f, 0.4f, 0.4f);
        }

        using (new EditorGUI.DisabledScope(!isActive))
        {
            if (DrawCell(rect, color))
            {
                ShowPassengerMenu(coordinates);
            }
        }
    }

    private bool DrawCell(Rect rect, Color color)
    {
        if (Event.current.type == EventType.Repaint)
        {
            EditorGUI.DrawRect(rect, color);
        }

        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    private void ToggleGridCell(Vector2Int coordinates)
    {
        Undo.RecordObject(_levelData, "Toggle Grid Cell");

        if (IsCellActive(coordinates))
        {
            List<GridCellData> cells = _levelData.GridCells;
            for (int i = cells.Count - 1; i >= 0; i--)
            {
                if (cells[i].Coordinates == coordinates)
                {
                    cells.RemoveAt(i);
                }
            }
            _activeCellLookup.Remove(coordinates);

            List<PassengerData> passengers = _levelData.Passengers;
            for (int i = passengers.Count - 1; i >= 0; i--)
            {
                if (passengers[i].Coordinates == coordinates)
                {
                    passengers.RemoveAt(i);
                }
            }
            _passengerLookup.Remove(coordinates);
        }
        else
        {
            _levelData.GridCells.Add(new GridCellData
            {
                Coordinates = coordinates
            });
            _activeCellLookup.Add(coordinates);
        }

        MarkDataChanged();
    }

    private void ShowPassengerMenu(Vector2Int coordinates)
    {
        GenericMenu menu = new GenericMenu();
        PassengerData passenger = GetPassenger(coordinates);

        GameColor[] allColors = LevelEditorWindow.AllColors;
        for (int i = 0; i < allColors.Length; i++)
        {
            GameColor color = allColors[i];
            bool isSelected = passenger != null && passenger.Color == color;

            menu.AddItem(ColorMenuContents[i], isSelected, () => SetPassenger(coordinates, color));
        }

        menu.AddSeparator("");

        if (passenger != null)
        {
            menu.AddItem(RemovePassengerContent, false, () => RemovePassenger(coordinates));
        }
        else
        {
            menu.AddDisabledItem(RemovePassengerContent);
        }

        menu.ShowAsContext();
    }

    private void SetPassenger(Vector2Int coordinates, GameColor color)
    {
        if (_levelData == null) return;
        if (!IsInsideGrid(coordinates)) return;
        if (!IsCellActive(coordinates)) return;

        Undo.RecordObject(_levelData, "Set Passenger");

        PassengerData passenger = GetPassenger(coordinates);

        if (passenger == null)
        {
            passenger = new PassengerData
            {
                Coordinates = coordinates,
                Color = color
            };
            _levelData.Passengers.Add(passenger);
            _passengerLookup[coordinates] = passenger;
        }
        else
        {
            passenger.Color = color;
        }

        MarkDataChanged();
    }

    private void RemovePassenger(Vector2Int coordinates)
    {
        if (_levelData == null) return;

        Undo.RecordObject(_levelData, "Remove Passenger");

        List<PassengerData> passengers = _levelData.Passengers;
        for (int i = passengers.Count - 1; i >= 0; i--)
        {
            if (passengers[i].Coordinates == coordinates)
            {
                passengers.RemoveAt(i);
            }
        }
        _passengerLookup.Remove(coordinates);

        MarkDataChanged();
    }

    private void DrawBuses()
    {
        serializedObject.Update();

        if (_busesProperty == null)
        {
            _busesProperty = serializedObject.FindProperty("Buses");
        }

        if (_busList == null)
        {
            CreateBusList(_busesProperty);
        }

        _busesProperty.isExpanded = EditorGUILayout.Foldout(_busesProperty.isExpanded, $"Buses ({_busesProperty.arraySize})", true);

        if (_busesProperty.isExpanded)
        {
            EditorGUILayout.Space(3f);

            if (_busesProperty.arraySize == 0)
            {
                EditorGUILayout.LabelField("No buses added.", EditorStyles.miniLabel);
            }
            else
            {
                _busList.DoLayoutList();
            }

            if (_busRemoveIndex >= 0)
            {
                _busesProperty.DeleteArrayElementAtIndex(_busRemoveIndex);

                _busList.index = Mathf.Clamp(_busList.index, -1, _busesProperty.arraySize - 1);

                _busRemoveIndex = -1;
            }

            if (GUILayout.Button("Add Bus", GUILayout.Height(26f)))
            {
                AddBus(_busesProperty);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void CreateBusList(SerializedProperty buses)
    {
        _busList = new ReorderableList(serializedObject, buses, true, false, false, false)
        {
            headerHeight = 0f,
            footerHeight = 0f,

            elementHeight = EditorGUIUtility.singleLineHeight * 4f + 10f,

            drawElementCallback = DrawBusElement
        };
    }

    private void DrawBusElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty bus = _busList.serializedProperty.GetArrayElementAtIndex(index);

        SerializedProperty color = bus.FindPropertyRelative("Color");

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = 5f;

        rect.y += 4f;
        rect.width = Mathf.Max(1f, rect.width - 4f);

        Rect colorRect = new Rect(rect.x, rect.y, rect.width, 4f);

        if (Event.current.type == EventType.Repaint)
        {
            EditorGUI.DrawRect(colorRect, ((GameColor)color.intValue).ToUnityColor());
        }

        rect.y += 9f;
        rect.height = lineHeight;

        EditorGUI.LabelField(rect, $"Bus {index + 1}", EditorStyles.boldLabel);

        rect.y += lineHeight + spacing;

        float prevLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = Mathf.Min(80f, rect.width * 0.4f);

        EditorGUI.PropertyField(rect, color, ColorLabel);

        EditorGUIUtility.labelWidth = prevLabelWidth;

        rect.y += lineHeight + spacing;

        Rect removeRect = new Rect(rect.x, rect.y, Mathf.Min(75f, rect.width), lineHeight);

        if (GUI.Button(removeRect, "Remove"))
        {
            _busRemoveIndex = index;
        }
    }

    private void AddBus(SerializedProperty buses)
    {
        int index = buses.arraySize;

        buses.InsertArrayElementAtIndex(index);

        SerializedProperty bus = buses.GetArrayElementAtIndex(index);

        bus.FindPropertyRelative("Color").intValue = (int)GameColor.Red;

        _busList.index = index;
    }

    private bool IsCellActive(Vector2Int coordinates)
    {
        return _activeCellLookup.Contains(coordinates);
    }

    private PassengerData GetPassenger(Vector2Int coordinates)
    {
        _passengerLookup.TryGetValue(coordinates, out PassengerData passenger);
        return passenger;
    }

    private bool IsInsideGrid(Vector2Int coordinates)
    {
        return coordinates.x >= 0 && coordinates.y >= 0 && coordinates.x < _levelData.GridSize.x && coordinates.y < _levelData.GridSize.y;
    }

    private void RemoveInvalidData()
    {
        _activeCellLookup.Clear();
        List<GridCellData> cells = _levelData.GridCells;
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

        List<PassengerData> passengers = _levelData.Passengers;
        for (int i = passengers.Count - 1; i >= 0; i--)
        {
            Vector2Int coord = passengers[i].Coordinates;
            if (!IsInsideGrid(coord) || !_activeCellLookup.Contains(coord))
            {
                passengers.RemoveAt(i);
            }
        }
    }

    private void MarkDataChanged()
    {
        EditorUtility.SetDirty(_levelData);
        Repaint();
    }
}
