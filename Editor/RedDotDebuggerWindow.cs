using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RedDotSystem.EditorTools
{
    /// <summary>
    /// 레드닷 트리를 계층으로 보여주고, 플레이 중에는 각 노드의 실효 카운트를 실시간으로
    /// 관찰하거나 값을 주입할 수 있는 디버거 창입니다. (Window ▸ RedDot ▸ Tree Debugger)
    ///
    /// 이 창이 답하려는 질문은 하나다 — "이 레드닷은 왜 켜져(꺼져) 있는가".
    /// 그래서 실효 카운트만 보여주지 않고 자기 카운트와 자식 합계를 나눠 표시하고,
    /// 잠금 상태와 enum 계층의 구조적 문제를 같은 화면에 함께 보여준다.
    ///
    /// 플레이 중이 아니어도 enum만으로 구조를 그릴 수 있다. 값을 잘못 매겨 노드가 조용히
    /// 루트가 되는 실수는 실행 전에 잡는 편이 싸다. 표시할 내용은 RedDotTreeModel(순수 C#)이
    /// 만들고 이 파일은 그리기만 담당하므로, 트리 구성 규칙은 EditMode 테스트로 검증된다.
    /// </summary>
    public sealed class RedDotDebuggerWindow : EditorWindow
    {
        private const float FoldoutWidth = 14f;
        private const float IndentPerDepth = 14f;
        private const float CountColumnWidth = 140f;
        private const float LockColumnWidth = 40f;
        private const float RepaintInterval = 0.1f;

        private IReadOnlyList<RedDotTreeEntry> _roots;
        private IReadOnlyList<RedDotTreeIssue> _issues;

        private readonly HashSet<RedDotType> _collapsed = new HashSet<RedDotType>();
        private readonly Dictionary<RedDotType, int> _pendingCounts = new Dictionary<RedDotType, int>();

        private Vector2 _scroll;
        private string _search = string.Empty;
        private bool _showIssues = true;
        private double _nextRepaint;

        private GUIStyle _nodeStyle;
        private GUIStyle _nodeOnStyle;
        private GUIStyle _nodeLockedStyle;

        [MenuItem("Window/RedDot/Tree Debugger")]
        public static void Open()
        {
            var window = GetWindow<RedDotDebuggerWindow>();
            window.titleContent = new GUIContent("RedDot Tree");
            window.minSize = new Vector2(520f, 300f);
            window.Show();
        }

        private void OnEnable() => Rebuild();

        // 플레이 중에는 카운트가 게임 로직으로 바뀌므로 주기적으로 다시 그린다.
        // Update는 초당 수십 회 호출되므로 그대로 Repaint하면 낭비다. 10Hz로 제한한다.
        private void Update()
        {
            if (!Application.isPlaying) return;
            if (EditorApplication.timeSinceStartup < _nextRepaint) return;

            _nextRepaint = EditorApplication.timeSinceStartup + RepaintInterval;
            Repaint();
        }

        private void Rebuild()
        {
            _roots = RedDotTreeModel.BuildRoots();
            _issues = RedDotTreeModel.Validate();
        }

        // GUIStyle을 OnGUI에서 매번 new 하면 리페인트마다 힙을 두드린다. 한 번 만들어 재사용한다.
        private void EnsureStyles()
        {
            if (_nodeStyle != null) return;

            _nodeStyle = new GUIStyle(EditorStyles.label);
            _nodeOnStyle = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold };
            _nodeLockedStyle = new GUIStyle(EditorStyles.label);
            _nodeLockedStyle.normal.textColor = Color.gray;
        }

        private void OnGUI()
        {
            EnsureStyles();
            if (_roots == null) Rebuild();

            DrawToolbar();
            DrawIssues();

            bool live = Application.isPlaying;
            DrawStatusBanner();
            DrawHeaderRow(live);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var root in _roots)
                DrawEntry(root, live);
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            _search = GUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(120f));

            if (GUILayout.Button("모두 펼치기", EditorStyles.toolbarButton, GUILayout.Width(90f)))
                _collapsed.Clear();

            if (GUILayout.Button("모두 접기", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                CollapseAll();

            if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(76f)))
                Rebuild();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStatusBanner()
        {
            if (Application.isPlaying) return;

            EditorGUILayout.HelpBox(
                "구조 미리보기입니다. 실효 카운트와 잠금 상태는 플레이 중에만 표시됩니다.",
                MessageType.Info);
        }

        private void CollapseAll()
        {
            _collapsed.Clear();

            void Walk(RedDotTreeEntry entry)
            {
                if (entry.Children.Count > 0) _collapsed.Add(entry.Type);
                foreach (var child in entry.Children) Walk(child);
            }

            foreach (var root in _roots) Walk(root);
        }

        private void DrawIssues()
        {
            if (_issues == null || _issues.Count == 0) return;

            _showIssues = EditorGUILayout.Foldout(_showIssues, $"구조 진단 ({_issues.Count})", true);
            if (!_showIssues) return;

            using (new EditorGUI.IndentLevelScope())
            {
                foreach (var issue in _issues)
                {
                    EditorGUILayout.HelpBox(
                        $"{issue.Type} ({(int)issue.Type}) — {issue.Message}",
                        issue.IsWarning ? MessageType.Warning : MessageType.Info);
                }
            }
        }

        private void DrawHeaderRow(bool live)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("노드", EditorStyles.miniBoldLabel);

            if (live)
            {
                EditorGUILayout.LabelField("카운트 (자기/자식)", EditorStyles.miniBoldLabel,
                    GUILayout.Width(CountColumnWidth));
                EditorGUILayout.LabelField("잠금", EditorStyles.miniBoldLabel, GUILayout.Width(LockColumnWidth));
                EditorGUILayout.LabelField("값 주입", EditorStyles.miniBoldLabel, GUILayout.Width(140f));
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawEntry(RedDotTreeEntry entry, bool live)
        {
            if (!Matches(entry)) return;

            bool searching = !string.IsNullOrWhiteSpace(_search);
            bool hasChildren = entry.Children.Count > 0;
            // 검색 중에는 접힘 상태를 무시한다. 찾은 항목이 접힌 부모 아래 숨으면 검색이 무의미하다.
            bool expanded = searching || !_collapsed.Contains(entry.Type);

            var node = live ? RedDotTree.GetNode(entry.Type) : null;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(entry.Depth * IndentPerDepth);

            if (hasChildren && !searching)
            {
                bool nowExpanded = GUILayout.Toggle(expanded, GUIContent.none, EditorStyles.foldout,
                    GUILayout.Width(FoldoutWidth));

                if (nowExpanded != expanded)
                {
                    if (nowExpanded) _collapsed.Remove(entry.Type);
                    else _collapsed.Add(entry.Type);
                    expanded = nowExpanded;
                }
            }
            else
            {
                GUILayout.Space(FoldoutWidth);
            }

            DrawNodeLabel(entry, node);

            if (live)
                DrawLiveControls(entry, node);

            EditorGUILayout.EndHorizontal();

            if (hasChildren && expanded)
            {
                foreach (var child in entry.Children)
                    DrawEntry(child, live);
            }
        }

        private void DrawNodeLabel(RedDotTreeEntry entry, RedDotNode node)
        {
            bool on = node != null && node.Count > 0;
            bool locked = node != null && node.Locked;

            GUIStyle style = locked ? _nodeLockedStyle : (on ? _nodeOnStyle : _nodeStyle);
            EditorGUILayout.LabelField($"{(on ? "●" : "○")} {entry.Type} ({(int)entry.Type})", style);
        }

        private void DrawLiveControls(RedDotTreeEntry entry, RedDotNode node)
        {
            if (node == null)
            {
                EditorGUILayout.LabelField("미등록", GUILayout.Width(CountColumnWidth));
                return;
            }

            EditorGUILayout.LabelField($"{node.Count}  ({node.SelfCount}/{node.ChildrenCount})",
                GUILayout.Width(CountColumnWidth));

            bool locked = EditorGUILayout.Toggle(node.Locked, GUILayout.Width(LockColumnWidth));
            if (locked != node.Locked)
                node.SetLocked(locked);

            _pendingCounts.TryGetValue(entry.Type, out int pending);
            int edited = EditorGUILayout.IntField(pending, GUILayout.Width(44f));
            if (edited != pending)
                _pendingCounts[entry.Type] = edited;

            if (GUILayout.Button("설정", EditorStyles.miniButtonLeft, GUILayout.Width(38f)))
                node.SetCount(Mathf.Max(0, edited));

            if (GUILayout.Button("+1", EditorStyles.miniButtonMid, GUILayout.Width(26f)))
                node.SetCount(node.SelfCount + 1);

            if (GUILayout.Button("-1", EditorStyles.miniButtonRight, GUILayout.Width(26f)))
                node.SetCount(Mathf.Max(0, node.SelfCount - 1));
        }

        // 검색어가 있으면 자기 이름이 맞거나, 후손 중 맞는 게 있으면 표시한다.
        private bool Matches(RedDotTreeEntry entry)
        {
            if (string.IsNullOrWhiteSpace(_search)) return true;

            if (entry.Type.ToString().IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            foreach (var child in entry.Children)
            {
                if (Matches(child)) return true;
            }

            return false;
        }
    }
}
