using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

namespace ViewportMeasure.Editor
{
    [InitializeOnLoad]
    public class ViewportMeasureTool
    {
        #region Enums
        public enum SnapMode
        {
            Free,
            AxisX,
            AxisY,
            AxisZ
        }
        #endregion

        #region Data Structures
        [Serializable]
        public class MeasurementLine
        {
            public Vector3 startPoint;
            public Vector3 endPoint;
            public float distance;
            public string name;
            public SnapMode snapMode;

            public MeasurementLine(Vector3 start, Vector3 end, SnapMode mode)
            {
                startPoint = start;
                endPoint = end;
                distance = Vector3.Distance(start, end);
                snapMode = mode;
                name = $"Line {DateTime.Now:HH:mm:ss}";
            }
        }

        [Serializable]
        public class GizmoSettings
        {
            public int fontSize = 12;
            public Color textColor = Color.white;
            public float sphereSize = 0.1f;
            public float lineWidth = 2f;
        }
        #endregion

        #region Static Instance Management
        private static ViewportMeasureTool _instance;
        public static ViewportMeasureTool Instance => _instance;

        static ViewportMeasureTool()
        {
            _instance = new ViewportMeasureTool();
        }
        #endregion

        #region Private Fields
        private readonly List<Vector3> _currentMeasurePoints = new List<Vector3>();
        private readonly List<MeasurementLine> _measurementHistory = new List<MeasurementLine>();
        private Label _statusLabel;
        private Label _distanceLabel;
        private bool _isActive;
        private SnapMode _currentSnapMode = SnapMode.Free;
        private Action _onHistoryChanged;
        private GizmoSettings _gizmoSettings = new GizmoSettings();
        #endregion

        #region Constructor
        private ViewportMeasureTool()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        #endregion

        #region Public Properties
        public SnapMode CurrentSnapMode
        {
            get => _currentSnapMode;
            set
            {
                _currentSnapMode = value;
                UpdateUI();
            }
        }

        public List<MeasurementLine> MeasurementHistory => new List<MeasurementLine>(_measurementHistory);
        
        public GizmoSettings Settings => _gizmoSettings;
        #endregion

        #region Public Methods
        public void SetUI(Label statusLabel, Label distanceLabel)
        {
            _statusLabel = statusLabel;
            _distanceLabel = distanceLabel;
            UpdateUI();
        }

        public void SetOnHistoryChanged(Action callback)
        {
            _onHistoryChanged = callback;
        }

        public void UpdateGizmoSettings(int fontSize, Color textColor, float sphereSize)
        {
            _gizmoSettings.fontSize = fontSize;
            _gizmoSettings.textColor = textColor;
            _gizmoSettings.sphereSize = sphereSize;
            SceneView.RepaintAll();
        }

        public void ClearMeasurement()
        {
            _currentMeasurePoints.Clear();
            UpdateUI();
            SceneView.RepaintAll();
        }

        public void ClearAllHistory()
        {
            _measurementHistory.Clear();
            _onHistoryChanged?.Invoke();
            SceneView.RepaintAll();
        }

        public void RemoveMeasurementLine(int index)
        {
            if (index >= 0 && index < _measurementHistory.Count)
            {
                _measurementHistory.RemoveAt(index);
                _onHistoryChanged?.Invoke();
                SceneView.RepaintAll();
            }
        }

        public void SetActive(bool active)
        {
            _isActive = active;
            if (!active)
            {
                ClearMeasurement();
            }
            UpdateUI();
        }
        #endregion

        #region Event Handlers
        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_isActive)
                return;

            HandleInput();
            DrawMeasurementGizmos();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                ClearMeasurement();
            }
        }
        #endregion

        #region Input Handling
        private void HandleInput()
        {
            Event current = Event.current;
            
            if (current.type == EventType.MouseDown && current.button == 0)
            {
                if (current.control || current.command)
                {
                    Vector3 worldPosition = GetWorldPositionFromMouse(current.mousePosition);
                    if (worldPosition != Vector3.zero)
                    {
                        AddMeasurePoint(worldPosition);
                        current.Use();
                    }
                }
            }
        }

        private Vector3 GetWorldPositionFromMouse(Vector2 mousePosition)
        {
            Camera sceneCamera = SceneView.lastActiveSceneView?.camera;
            if (sceneCamera == null)
                return Vector3.zero;

            Vector3 mousePos = new Vector3(mousePosition.x, sceneCamera.pixelHeight - mousePosition.y, 0);
            Ray ray = sceneCamera.ScreenPointToRay(mousePos);
            
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                return hit.point;
            }
            
            if (Mathf.Abs(ray.direction.y) > 0.001f)
            {
                float distance = -ray.origin.y / ray.direction.y;
                if (distance > 0)
                {
                    return ray.GetPoint(distance);
                }
            }
            
            return ray.GetPoint(10f);
        }

        private void AddMeasurePoint(Vector3 point)
        {
            if (_currentMeasurePoints.Count >= 2)
            {
                _currentMeasurePoints.Clear();
            }
            
            if (_currentMeasurePoints.Count == 1)
            {
                // 2点目の場合、Axis Snapモードを適用
                point = ApplyAxisSnap(_currentMeasurePoints[0], point);
                
                // 測定ラインを履歴に追加
                var measurementLine = new MeasurementLine(_currentMeasurePoints[0], point, _currentSnapMode);
                _measurementHistory.Add(measurementLine);
                _onHistoryChanged?.Invoke();
                
                _currentMeasurePoints.Clear();
            }
            else
            {
                _currentMeasurePoints.Add(point);
            }
            
            UpdateUI();
            SceneView.RepaintAll();
        }

        private Vector3 ApplyAxisSnap(Vector3 startPoint, Vector3 endPoint)
        {
            switch (_currentSnapMode)
            {
                case SnapMode.AxisX:
                    return new Vector3(endPoint.x, startPoint.y, startPoint.z);
                case SnapMode.AxisY:
                    return new Vector3(startPoint.x, endPoint.y, startPoint.z);
                case SnapMode.AxisZ:
                    return new Vector3(startPoint.x, startPoint.y, endPoint.z);
                case SnapMode.Free:
                default:
                    return endPoint;
            }
        }
        #endregion

        #region Gizmo Drawing
        private void DrawMeasurementGizmos()
        {
            // 履歴の測定ラインを描画
            DrawHistoryLines();
            
            // 現在の測定点を描画
            DrawCurrentMeasurement();
        }

        private void DrawHistoryLines()
        {
            for (int i = 0; i < _measurementHistory.Count; i++)
            {
                var line = _measurementHistory[i];
                
                // ライン描画
                Handles.color = GetSnapModeColor(line.snapMode);
                Handles.DrawLine(line.startPoint, line.endPoint);
                
                // 端点描画
                Handles.SphereHandleCap(0, line.startPoint, Quaternion.identity, _gizmoSettings.sphereSize, EventType.Repaint);
                Handles.SphereHandleCap(0, line.endPoint, Quaternion.identity, _gizmoSettings.sphereSize, EventType.Repaint);
                
                // 距離ラベル描画（カスタムスタイル適用）
                Vector3 midPoint = (line.startPoint + line.endPoint) * 0.5f;
                string label = $"{line.name}\n{line.distance:F2}m";
                
                var labelStyle = CreateCustomLabelStyle();
                Handles.Label(midPoint + Vector3.up * 0.3f, label, labelStyle);
            }
        }

        private void DrawCurrentMeasurement()
        {
            if (_currentMeasurePoints.Count == 0)
                return;

            Handles.color = Color.red;
            
            // 現在の測定点を描画
            for (int i = 0; i < _currentMeasurePoints.Count; i++)
            {
                Handles.SphereHandleCap(0, _currentMeasurePoints[i], Quaternion.identity, _gizmoSettings.sphereSize, EventType.Repaint);
                
                Vector3 labelPos = _currentMeasurePoints[i] + Vector3.up * 0.2f;
                var pointLabelStyle = CreateCustomLabelStyle(true);
                Handles.Label(labelPos, $"P{i + 1}", pointLabelStyle);
            }
            
            // プレビューライン描画（マウス位置まで）
            if (_currentMeasurePoints.Count == 1 && Event.current != null)
            {
                Vector3 mouseWorldPos = GetWorldPositionFromMouse(Event.current.mousePosition);
                if (mouseWorldPos != Vector3.zero)
                {
                    Vector3 snappedPos = ApplyAxisSnap(_currentMeasurePoints[0], mouseWorldPos);
                    
                    Handles.color = GetSnapModeColor(_currentSnapMode) * 0.7f;
                    Handles.DrawDottedLine(_currentMeasurePoints[0], snappedPos, 5f);
                    
                    // プレビュー距離表示（カスタムスタイル適用）
                    float previewDistance = Vector3.Distance(_currentMeasurePoints[0], snappedPos);
                    Vector3 midPoint = (_currentMeasurePoints[0] + snappedPos) * 0.5f;
                    var previewStyle = CreateCustomLabelStyle();
                    Handles.Label(midPoint + Vector3.up * 0.5f, $"{previewDistance:F2}m", previewStyle);
                }
            }
        }

        private GUIStyle CreateCustomLabelStyle(bool bold = false)
        {
            var style = new GUIStyle(bold ? EditorStyles.boldLabel : EditorStyles.label);
            style.fontSize = _gizmoSettings.fontSize;
            style.normal.textColor = _gizmoSettings.textColor;
            style.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            
            // 背景を半透明にして読みやすくする
            var bgColor = Color.black;
            bgColor.a = 0.7f;
            style.normal.background = CreateBackgroundTexture(bgColor);
            
            return style;
        }

        private Texture2D CreateBackgroundTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private Color GetSnapModeColor(SnapMode mode)
        {
            return mode switch
            {
                SnapMode.AxisX => Color.red,
                SnapMode.AxisY => Color.green,
                SnapMode.AxisZ => Color.blue,
                SnapMode.Free => Color.yellow,
                _ => Color.white
            };
        }
        #endregion

        #region UI Updates
        private void UpdateUI()
        {
            if (_statusLabel == null || _distanceLabel == null)
                return;

            if (!_isActive)
            {
                _statusLabel.text = "Tool is disabled. Enable the tool to start measuring.";
                _distanceLabel.text = "Distance: -";
                return;
            }

            string snapModeText = _currentSnapMode switch
            {
                SnapMode.AxisX => " (X-Axis)",
                SnapMode.AxisY => " (Y-Axis)", 
                SnapMode.AxisZ => " (Z-Axis)",
                SnapMode.Free => " (Free)",
                _ => ""
            };

            switch (_currentMeasurePoints.Count)
            {
                case 0:
                    _statusLabel.text = $"Hold Ctrl+Click to place first point{snapModeText}";
                    _distanceLabel.text = "Distance: -";
                    break;
                case 1:
                    _statusLabel.text = $"Hold Ctrl+Click to place second point{snapModeText}";
                    _distanceLabel.text = "Distance: -";
                    break;
            }
        }
        #endregion
    }
}
