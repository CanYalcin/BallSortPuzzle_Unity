#if UNITY_EDITOR
using System.IO;
using SortPuzzle.Data;
using SortPuzzle.Generation;
using UnityEditor;
using UnityEngine;

namespace SortPuzzle.Editor
{
    public class LevelEditorWindow : EditorWindow
    {
        private int     _tubeCount    = 6;
        private int     _emptyTubes   = 2;
        private int     _capacity     = 4;
        private int     _colorCount   = 4;
        private int     _worldIndex   = 0;
        private int     _levelIndex   = 0;
        private int     _difficulty   = 3;
        private int     _goldReward   = 20;
        private bool    _isDailyLevel = false;
        private int[][] _state;
        private int     _paintColor   = 1;
        private string  _validMsg     = "";
        private bool    _isValid      = false;
        private int     _par          = 0;
        private string  _solution     = "";
        private string  _folder       = "Assets/SortPuzzle/Settings/Levels/World1";
        private Vector2 _scroll;

        private static readonly Color[] SC =
        {
            Color.grey,
            new Color(0.92f,0.26f,0.21f), new Color(0.13f,0.59f,0.95f),
            new Color(0.30f,0.69f,0.31f), new Color(1.00f,0.76f,0.03f),
            new Color(0.61f,0.15f,0.69f), new Color(1.00f,0.60f,0.00f),
            new Color(0.91f,0.12f,0.39f), new Color(0.00f,0.74f,0.83f),
            new Color(0.47f,0.33f,0.28f), new Color(0.62f,0.62f,0.62f),
        };

        [MenuItem("SortPuzzle/Level Editor")]
        public static void Open()
        {
            var w = GetWindow<LevelEditorWindow>("Level Editor");
            w.minSize = new Vector2(580, 680);
            w._state  = new int[w._tubeCount][];
            for (int i = 0; i < w._tubeCount; i++) w._state[i] = new int[w._capacity];
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.LabelField("Water Sort Level Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // Config
            EditorGUI.BeginChangeCheck();
            _worldIndex   = EditorGUILayout.IntSlider("World",       _worldIndex,  0, 2);
            _levelIndex   = EditorGUILayout.IntField ("Level Index", _levelIndex);
            _tubeCount    = EditorGUILayout.IntSlider("Tubes",       _tubeCount,   3, 14);
            _emptyTubes   = EditorGUILayout.IntSlider("Empty Tubes", _emptyTubes,  1, 4);
            _capacity     = EditorGUILayout.IntSlider("Capacity",    _capacity,    3, 6);
            _colorCount   = EditorGUILayout.IntSlider("Colors",      _colorCount,  2, 10);
            _goldReward   = EditorGUILayout.IntSlider("Gold Reward", _goldReward,  10, 50);
            _isDailyLevel = EditorGUILayout.Toggle   ("Daily Level", _isDailyLevel);
            if (EditorGUI.EndChangeCheck())
            {
                int old = _state?.Length ?? 0;
                var ns = new int[_tubeCount][];
                for (int i = 0; i < _tubeCount; i++)
                {
                    ns[i] = new int[_capacity];
                    if (i < old && _state[i] != null)
                        for (int j = 0; j < Mathf.Min(_capacity, _state[i].Length); j++)
                            ns[i][j] = _state[i][j];
                }
                _state = ns; _validMsg = ""; _isValid = false;
            }
            EditorGUILayout.Space(4);

            // Palette
            EditorGUILayout.LabelField("Paint Color", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            for (int c = 1; c <= 10; c++)
            {
                GUI.backgroundColor = c < SC.Length ? SC[c] : Color.white;
                if (GUILayout.Button(c.ToString(), GUILayout.Width(36), GUILayout.Height(22)))
                    _paintColor = c;
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Erase", GUILayout.Width(60), GUILayout.Height(22)))
                _paintColor = 0;
            EditorGUILayout.Space(4);

            // Tube grid
            EditorGUILayout.LabelField($"Tubes (0=bottom, {_capacity-1}=top)", EditorStyles.boldLabel);
            if (_state == null || _state.Length != _tubeCount)
            {
                _state = new int[_tubeCount][];
                for (int i = 0; i < _tubeCount; i++) _state[i] = new int[_capacity];
            }
            int cols  = Mathf.Min(7, _tubeCount);
            int gRows = Mathf.CeilToInt((float)_tubeCount / cols);
            for (int gr = 0; gr < gRows; gr++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int gc = 0; gc < cols; gc++)
                {
                    int ti = gr * cols + gc;
                    if (ti >= _tubeCount) break;
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(46));
                    EditorGUILayout.LabelField("T"+ti, EditorStyles.centeredGreyMiniLabel, GUILayout.Width(46));
                    for (int si = _capacity-1; si >= 0; si--)
                    {
                        int cid = _state[ti][si];
                        GUI.backgroundColor = cid > 0 && cid < SC.Length ? SC[cid] : new Color(0.18f,0.18f,0.18f);
                        if (GUILayout.Button(cid>0?cid.ToString():"·", GUILayout.Width(36), GUILayout.Height(28)))
                        {
                            _state[ti][si] = Event.current.button==1 ? 0 : _paintColor;
                            _validMsg = ""; _isValid = false;
                        }
                    }
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
            }
            EditorGUILayout.Space(4);

            // Validate — build temp inline
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            if (GUILayout.Button("Validate (Run Solver)", GUILayout.Height(26)))
            {
                var vt = CreateInstance<LevelData>();
                vt.WorldIndex=_worldIndex; vt.LevelIndex=_levelIndex;
                vt.TubeCount=_tubeCount; vt.EmptyTubeCount=_emptyTubes;
                vt.TubeCapacity=_capacity; vt.ColorCount=_colorCount;
                vt.GoldReward=_goldReward; vt.DifficultyRating=_difficulty;
                vt.Tubes = new TubeRow[_tubeCount];
                for (int i=0;i<_tubeCount;i++)
                {
                    var vb = new int[_capacity];
                    if (_state!=null&&i<_state.Length&&_state[i]!=null)
                        for (int j=0;j<Mathf.Min(_capacity,_state[i].Length);j++) vb[j]=_state[i][j];
                    vt.Tubes[i] = new TubeRow(vb);
                }
                var res = LevelSolver.Solve(vt);
                _isValid=res.IsSolvable; _par=res.ParMoves; _solution=res.SolutionPath;
                _validMsg = _isValid ? $"OK Solvable in {_par} moves." : "FAIL Unsolvable.";
            }
            if (!string.IsNullOrEmpty(_validMsg))
                EditorGUILayout.HelpBox(_validMsg, _isValid ? MessageType.Info : MessageType.Error);
            EditorGUILayout.Space(4);

            // Generator
            EditorGUILayout.LabelField("Auto-Generator", EditorStyles.boldLabel);
            _difficulty = EditorGUILayout.IntSlider("Difficulty", _difficulty, 1, 10);
            if (GUILayout.Button("Auto-Generate", GUILayout.Height(26)))
            {
                var gen = LevelGenerator.Generate(_difficulty, _worldIndex, _levelIndex, _capacity);
                if (gen != null)
                {
                    _tubeCount=gen.TubeCount; _emptyTubes=gen.EmptyTubeCount;
                    _colorCount=gen.ColorCount; _goldReward=gen.GoldReward;
                    _par=gen.ParMoves; _solution=gen.ValidatedSolution; _isValid=true;
                    _state = new int[_tubeCount][];
                    for (int i=0;i<_tubeCount;i++)
                    {
                        _state[i] = new int[_capacity];
                        if (gen.Tubes!=null&&i<gen.Tubes.Length&&gen.Tubes[i]?.Balls!=null)
                            for (int j=0;j<Mathf.Min(_capacity,gen.Tubes[i].Balls.Length);j++)
                                _state[i][j]=gen.Tubes[i].Balls[j];
                    }
                    _validMsg = $"Generated! Solvable in {_par} moves.";
                }
                else { _validMsg="Generator failed."; _isValid=false; }
            }
            EditorGUILayout.Space(4);

            // Save — build asset inline
            EditorGUILayout.LabelField("Save", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _folder = EditorGUILayout.TextField("Folder", _folder);
            if (GUILayout.Button("...", GUILayout.Width(28)))
            {
                string p = EditorUtility.OpenFolderPanel("Save folder", _folder, "");
                if (!string.IsNullOrEmpty(p))
                {
                    if (p.StartsWith(Application.dataPath))
                        p = "Assets" + p.Substring(Application.dataPath.Length);
                    _folder = p;
                }
            }
            EditorGUILayout.EndHorizontal();
            GUI.enabled = _isValid;
            if (GUILayout.Button("Save Level Asset", GUILayout.Height(30)))
            {
                if (!Directory.Exists(_folder)) Directory.CreateDirectory(_folder);
                var ld = CreateInstance<LevelData>();
                ld.WorldIndex=_worldIndex; ld.LevelIndex=_levelIndex;
                ld.TubeCount=_tubeCount; ld.EmptyTubeCount=_emptyTubes;
                ld.TubeCapacity=_capacity; ld.ColorCount=_colorCount;
                ld.GoldReward=_goldReward; ld.DifficultyRating=_difficulty;
                ld.IsDailyLevel=_isDailyLevel;
                ld.DisplayName=$"Level {_levelIndex+1}";
                ld.name=$"Level_W{_worldIndex+1}_{(_levelIndex+1):D3}";
                ld.Tubes = new TubeRow[_tubeCount];
                for (int i=0;i<_tubeCount;i++)
                {
                    var sb = new int[_capacity];
                    if (_state!=null&&i<_state.Length&&_state[i]!=null)
                        for (int j=0;j<Mathf.Min(_capacity,_state[i].Length);j++) sb[j]=_state[i][j];
                    ld.Tubes[i]=new TubeRow(sb);
                }
                ld.SetValidationResult(_par, _solution);
                string ap=$"{_folder}/{ld.name}.asset";
                AssetDatabase.CreateAsset(ld, ap);
                AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
                EditorUtility.FocusProjectWindow(); Selection.activeObject=ld;
                Debug.Log($"[LevelEditor] Saved {ap}  Par:{_par}");
            }
            GUI.enabled = true;
            if (!_isValid) EditorGUILayout.HelpBox("Validate before saving.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
