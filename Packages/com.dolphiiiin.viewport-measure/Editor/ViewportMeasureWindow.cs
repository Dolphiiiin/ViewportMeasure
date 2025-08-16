using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace ViewportMeasure.Editor
{
    public class ViewportMeasureWindow : EditorWindow
    {
        #region Private Fields
        private Label _statusLabel;
        private Label _distanceLabel;
        private Toggle _activeToggle;
        private EnumField _snapModeField;
        private ScrollView _historyScrollView;
        private Label _historyCountLabel;
        
        // スタイル設定フィールド
        private Foldout _styleSettings;
        private SliderInt _gizmoFontSizeSlider;
        private Slider _gizmoSphereSlider;
        private Toggle _showCoordinatesToggle;
        
        // Gizmo設定値
        private int _gizmoFontSize = 12;
        private Color _gizmoTextColor = Color.white;
        private float _gizmoSphereSize = 0.1f;
        private bool _showCoordinates = true;
        #endregion

        #region Menu Item
        [MenuItem("Tools/Viewport Measure")]
        public static void ShowWindow()
        {
            var window = GetWindow<ViewportMeasureWindow>();
            window.titleContent = new GUIContent("Viewport Measure");
            window.Show();
        }
        #endregion

        #region EditorWindow Implementation
        public void CreateGUI()
        {
            var root = new VisualElement();
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;
            root.style.paddingTop = 10;
            root.style.paddingBottom = 10;
            
            // タイトル
            var titleLabel = new Label("Viewport Measure Tool");
            titleLabel.style.fontSize = 14;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 10;
            
            // ツール有効化トグル
            _activeToggle = new Toggle("Enable Measurement Tool");
            _activeToggle.value = false;
            _activeToggle.RegisterValueChangedCallback(evt =>
            {
                ViewportMeasureTool.Instance?.SetActive(evt.newValue);
            });
            
            // Snap Modeセレクター
            _snapModeField = new EnumField("Snap Mode", ViewportMeasureTool.SnapMode.Free);
            _snapModeField.style.marginTop = 5;
            _snapModeField.RegisterValueChangedCallback(evt =>
            {
                if (ViewportMeasureTool.Instance != null)
                {
                    ViewportMeasureTool.Instance.CurrentSnapMode = (ViewportMeasureTool.SnapMode)evt.newValue;
                }
            });
            
            // スタイル設定パネル
            CreateStyleSettingsPanel();
            
            // 状態表示
            _statusLabel = new Label("Tool is disabled");
            _statusLabel.style.fontSize = 12;
            _statusLabel.style.whiteSpace = WhiteSpace.Normal;
            _statusLabel.style.marginTop = 10;
            
            _distanceLabel = new Label("Distance: -");
            _distanceLabel.style.fontSize = 12;
            _distanceLabel.style.marginTop = 5;
            _distanceLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            
            // ボタンコンテナ
            var buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row;
            buttonContainer.style.marginTop = 10;
            
            var clearCurrentButton = new Button(() => ViewportMeasureTool.Instance?.ClearMeasurement())
            {
                text = "Clear Current"
            };
            clearCurrentButton.style.flexGrow = 1;
            clearCurrentButton.style.marginRight = 5;
            
            var clearAllButton = new Button(() => ViewportMeasureTool.Instance?.ClearAllHistory())
            {
                text = "Clear All"
            };
            clearAllButton.style.flexGrow = 1;
            
            buttonContainer.Add(clearCurrentButton);
            buttonContainer.Add(clearAllButton);
            
            // 測定履歴セクション
            var historyLabel = new Label("Measurement History");
            historyLabel.style.fontSize = 14;
            historyLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            historyLabel.style.marginTop = 15;
            historyLabel.style.marginBottom = 5;
            
            _historyCountLabel = new Label("0 measurements");
            _historyCountLabel.style.fontSize = 10;
            _historyCountLabel.style.color = Color.gray;
            _historyCountLabel.style.marginBottom = 5;
            
            _historyScrollView = new ScrollView();
            _historyScrollView.style.maxHeight = 200;
            _historyScrollView.style.minHeight = 100;
            _historyScrollView.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
            _historyScrollView.style.borderTopWidth = 1;
            _historyScrollView.style.borderBottomWidth = 1;
            _historyScrollView.style.borderLeftWidth = 1;
            _historyScrollView.style.borderRightWidth = 1;
            _historyScrollView.style.borderTopColor = Color.gray;
            _historyScrollView.style.borderBottomColor = Color.gray;
            _historyScrollView.style.borderLeftColor = Color.gray;
            _historyScrollView.style.borderRightColor = Color.gray;
            
            // 要素を追加
            root.Add(titleLabel);
            root.Add(_activeToggle);
            root.Add(_snapModeField);
            root.Add(_styleSettings);
            root.Add(_statusLabel);
            root.Add(_distanceLabel);
            root.Add(buttonContainer);
            root.Add(historyLabel);
            root.Add(_historyCountLabel);
            root.Add(_historyScrollView);
            
            rootVisualElement.Add(root);
            
            // ツールインスタンスにUIを登録
            ViewportMeasureTool.Instance?.SetUI(_statusLabel, _distanceLabel);
            ViewportMeasureTool.Instance?.SetOnHistoryChanged(UpdateHistoryDisplay);
            
            // 初期履歴表示
            UpdateHistoryDisplay();
        }
        #endregion
        
        #region Style Settings
        private void CreateStyleSettingsPanel()
        {
            _styleSettings = new Foldout();
            _styleSettings.text = "Gizmo Settings";
            _styleSettings.style.marginTop = 10;
            _styleSettings.style.marginBottom = 10;
            
            // Gizmoフォントサイズスライダー
            _gizmoFontSizeSlider = new SliderInt("Gizmo Font Size", 8, 32);
            _gizmoFontSizeSlider.value = _gizmoFontSize;
            _gizmoFontSizeSlider.RegisterValueChangedCallback(evt =>
            {
                _gizmoFontSize = evt.newValue;
                ApplyGizmoSettings();
            });
            
            // Gizmoスフィアサイズスライダー
            _gizmoSphereSlider = new Slider("Sphere Size", 0.01f, 0.5f);
            _gizmoSphereSlider.value = _gizmoSphereSize;
            _gizmoSphereSlider.RegisterValueChangedCallback(evt =>
            {
                _gizmoSphereSize = evt.newValue;
                ApplyGizmoSettings();
            });
            
            // 座標表示トグル
            _showCoordinatesToggle = new Toggle("Show Coordinates");
            _showCoordinatesToggle.value = _showCoordinates;
            _showCoordinatesToggle.RegisterValueChangedCallback(evt =>
            {
                _showCoordinates = evt.newValue;
                UpdateHistoryDisplay();
            });
            
            // デフォルトに戻すボタン
            var resetButton = new Button(ResetGizmoSettings)
            {
                text = "Reset to Default"
            };
            resetButton.style.marginTop = 5;
            
            _styleSettings.Add(_gizmoFontSizeSlider);
            _styleSettings.Add(_gizmoSphereSlider);
            _styleSettings.Add(_showCoordinatesToggle);
            _styleSettings.Add(resetButton);
        }
        
        private void ApplyGizmoSettings()
        {
            // Gizmo設定をツールに適用
            ViewportMeasureTool.Instance?.UpdateGizmoSettings(_gizmoFontSize, _gizmoTextColor, _gizmoSphereSize);
        }
        
        private void ResetGizmoSettings()
        {
            _gizmoFontSize = 12;
            _gizmoTextColor = Color.white;
            _gizmoSphereSize = 0.1f;
            _showCoordinates = true;
            
            // UI要素の値を更新
            _gizmoFontSizeSlider.value = _gizmoFontSize;
            _showCoordinatesToggle.value = _showCoordinates;
            _gizmoSphereSlider.value = _gizmoSphereSize;
            
            // Gizmo設定を適用
            ApplyGizmoSettings();
        }
        #endregion
        
        #region History Management
        private void UpdateHistoryDisplay()
        {
            if (_historyScrollView == null || _historyCountLabel == null)
                return;
                
            _historyScrollView.Clear();
            
            var history = ViewportMeasureTool.Instance?.MeasurementHistory;
            if (history == null || history.Count == 0)
            {
                _historyCountLabel.text = "0 measurements";
                
                var emptyLabel = new Label("No measurements yet");
                emptyLabel.style.fontSize = 10;
                emptyLabel.style.color = Color.gray;
                emptyLabel.style.alignSelf = Align.Center;
                emptyLabel.style.marginTop = 20;
                _historyScrollView.Add(emptyLabel);
                return;
            }
            
            _historyCountLabel.text = $"{history.Count} measurement{(history.Count > 1 ? "s" : "")}";
            
            for (int i = 0; i < history.Count; i++)
            {
                var line = history[i];
                var lineElement = CreateHistoryLineElement(line, i);
                _historyScrollView.Add(lineElement);
            }
        }
        
        private VisualElement CreateHistoryLineElement(ViewportMeasureTool.MeasurementLine line, int index)
        {
            var container = new VisualElement();
            container.style.marginBottom = 5;
            container.style.paddingLeft = 5;
            container.style.paddingRight = 5;
            container.style.paddingTop = 3;
            container.style.paddingBottom = 3;
            container.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            
            // ヘッダー行
            var headerContainer = new VisualElement();
            headerContainer.style.flexDirection = FlexDirection.Row;
            headerContainer.style.alignItems = Align.Center;
            
            var nameLabel = new Label(line.name);
            nameLabel.style.fontSize = 11;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.flexGrow = 1;
            
            // Snap Modeインジケーター
            var modeLabel = new Label(GetSnapModeDisplayText(line.snapMode));
            modeLabel.style.fontSize = 9;
            modeLabel.style.color = GetSnapModeUIColor(line.snapMode);
            modeLabel.style.marginRight = 5;
            
            var deleteButton = new Button(() =>
            {
                ViewportMeasureTool.Instance?.RemoveMeasurementLine(index);
            })
            {
                text = "×"
            };
            deleteButton.style.width = 20;
            deleteButton.style.height = 16;
            deleteButton.style.fontSize = 10;
            deleteButton.style.backgroundColor = new Color(0.8f, 0.3f, 0.3f, 0.8f);
            
            headerContainer.Add(nameLabel);
            headerContainer.Add(modeLabel);
            headerContainer.Add(deleteButton);
            
            // 詳細行
            var detailContainer = new VisualElement();
            detailContainer.style.marginTop = 2;
            
            var distanceLabel = new Label($"Distance: {line.distance:F3}m");
            distanceLabel.style.fontSize = 10;
            
            detailContainer.Add(distanceLabel);
            
            // 座標表示（オプション）
            if (_showCoordinates)
            {
                var coordsLabel = new Label($"From: {FormatVector3(line.startPoint)} To: {FormatVector3(line.endPoint)}");
                coordsLabel.style.fontSize = 9;
                coordsLabel.style.color = Color.gray;
                coordsLabel.style.whiteSpace = WhiteSpace.Normal;
                detailContainer.Add(coordsLabel);
            }
            
            container.Add(headerContainer);
            container.Add(detailContainer);
            
            return container;
        }
        
        private string GetSnapModeDisplayText(ViewportMeasureTool.SnapMode mode)
        {
            return mode switch
            {
                ViewportMeasureTool.SnapMode.AxisX => "X",
                ViewportMeasureTool.SnapMode.AxisY => "Y",
                ViewportMeasureTool.SnapMode.AxisZ => "Z",
                ViewportMeasureTool.SnapMode.Free => "FREE",
                _ => "?"
            };
        }
        
        private Color GetSnapModeUIColor(ViewportMeasureTool.SnapMode mode)
        {
            return mode switch
            {
                ViewportMeasureTool.SnapMode.AxisX => Color.red,
                ViewportMeasureTool.SnapMode.AxisY => Color.green,
                ViewportMeasureTool.SnapMode.AxisZ => Color.blue,
                ViewportMeasureTool.SnapMode.Free => Color.yellow,
                _ => Color.white
            };
        }
        
        private string FormatVector3(Vector3 vector)
        {
            return $"({vector.x:F2}, {vector.y:F2}, {vector.z:F2})";
        }
        #endregion
    }
}
