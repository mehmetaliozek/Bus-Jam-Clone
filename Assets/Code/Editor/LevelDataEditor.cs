using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(LevelData))]
public class LevelDataEditor : Editor
{
    private LevelData _levelData;

    private Vector2 _gridScrollPosition;
    private Vector2 _passengerScrollPosition;

    private ReorderableList _busList;
    private int _busRemoveIndex = -1;

    private const float CellSize = 30f;
    private const float CellSpacing = 3f;
    private const float MaxGridHeight = 240f;
    private const float ScrollbarSize = 18f;

    private void OnEnable()
    {
        _levelData = (LevelData)target;
        Undo.undoRedoPerformed += Repaint;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= Repaint;
    }

    public override void OnInspectorGUI()
    {
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
        EditorGUI.DrawRect(rect, color);

        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    private void ToggleGridCell(Vector2Int coordinates)
    {
        Undo.RecordObject(_levelData, "Toggle Grid Cell");

        if (IsCellActive(coordinates))
        {
            _levelData.GridCells.RemoveAll(cell => cell.Coordinates == coordinates);

            _levelData.Passengers.RemoveAll(passenger => passenger.Coordinates == coordinates);
        }
        else
        {
            _levelData.GridCells.Add(new GridCellData
            {
                Coordinates = coordinates
            });
        }

        MarkDataChanged();
    }

    private void ShowPassengerMenu(Vector2Int coordinates)
    {
        GenericMenu menu = new GenericMenu();
        PassengerData passenger = GetPassenger(coordinates);

        foreach (GameColor color in Enum.GetValues(typeof(GameColor)))
        {
            bool isSelected = passenger != null && passenger.Color == color;

            menu.AddItem(new GUIContent(color.ToString()), isSelected, () => SetPassenger(coordinates, color));
        }

        menu.AddSeparator("");

        if (passenger != null)
        {
            menu.AddItem(new GUIContent("Remove Passenger"), false, () => RemovePassenger(coordinates));
        }
        else
        {
            menu.AddDisabledItem(new GUIContent("Remove Passenger"));
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
            _levelData.Passengers.Add(new PassengerData
            {
                Coordinates = coordinates,
                Color = color
            });
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

        _levelData.Passengers.RemoveAll(passenger => passenger.Coordinates == coordinates);

        MarkDataChanged();
    }

    private void DrawBuses()
    {
        serializedObject.Update();

        SerializedProperty buses = serializedObject.FindProperty("Buses");

        if (_busList == null)
        {
            CreateBusList(buses);
        }

        buses.isExpanded = EditorGUILayout.Foldout(buses.isExpanded, $"Buses ({buses.arraySize})", true);

        if (buses.isExpanded)
        {
            EditorGUILayout.Space(3f);

            if (buses.arraySize == 0)
            {
                EditorGUILayout.LabelField("No buses added.", EditorStyles.miniLabel);
            }
            else
            {
                _busList.DoLayoutList();
            }

            if (_busRemoveIndex >= 0)
            {
                buses.DeleteArrayElementAtIndex(_busRemoveIndex);

                _busList.index = Mathf.Clamp(_busList.index, -1, buses.arraySize - 1);

                _busRemoveIndex = -1;
            }

            if (GUILayout.Button("Add Bus", GUILayout.Height(26f)))
            {
                AddBus(buses);
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

            elementHeight = EditorGUIUtility.singleLineHeight * 4f + 34f,

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

        EditorGUI.DrawRect(colorRect, ((GameColor)color.intValue).ToUnityColor());

        rect.y += 9f;
        rect.height = lineHeight;

        EditorGUI.LabelField(rect, $"Bus {index + 1}", EditorStyles.boldLabel);

        rect.y += lineHeight + spacing;

        float previousLabelWidth = EditorGUIUtility.labelWidth;

        EditorGUIUtility.labelWidth = Mathf.Min(80f, rect.width * 0.4f);

        EditorGUI.PropertyField(rect, color, new GUIContent("Color"));

        rect.y += lineHeight + spacing;

        EditorGUI.LabelField(rect, $"Capacity: {GameConstants.BusCapacity}");

        EditorGUIUtility.labelWidth = previousLabelWidth;

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
        return _levelData.GridCells.Exists(cell => cell.Coordinates == coordinates);
    }

    private PassengerData GetPassenger(Vector2Int coordinates)
    {
        return _levelData.Passengers.Find(passenger => passenger.Coordinates == coordinates);
    }

    private bool IsInsideGrid(Vector2Int coordinates)
    {
        return coordinates.x >= 0 && coordinates.y >= 0 && coordinates.x < _levelData.GridSize.x && coordinates.y < _levelData.GridSize.y;
    }

    private void RemoveInvalidData()
    {
        _levelData.GridCells.RemoveAll(cell => !IsInsideGrid(cell.Coordinates));

        _levelData.Passengers.RemoveAll(passenger => !IsInsideGrid(passenger.Coordinates) || !IsCellActive(passenger.Coordinates));
    }

    private void MarkDataChanged()
    {
        EditorUtility.SetDirty(_levelData);
        Repaint();
    }
}