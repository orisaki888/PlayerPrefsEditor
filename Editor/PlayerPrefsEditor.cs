using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

#if UNITY_EDITOR_WIN
using Microsoft.Win32;
#endif

public class PlayerPrefsEditor : EditorWindow
{
    // PlayerPrefsのデータ型を表現するためのenum
    private enum PrefType { String, Int, Float }

    // PlayerPrefsのキーと値を保持するための内部クラス
    private class PlayerPrefData
    {
        public string Key { get; set; }
        public object Value { get; set; }
        public PrefType Type { get; set; }
        
        // 値を文字列として編集するために使用
        public string StringValue { get; set; }
    }

    private List<PlayerPrefData> _prefsData = new List<PlayerPrefData>();
    private Vector2 _scrollPosition;

    // 新規追加用の変数
    private string _newKey = "";
    private string _newValue = "";
    private PrefType _newType = PrefType.String;

    // メニューからウィンドウを開くためのメソッド
    [MenuItem("Tools/PlayerPrefs Editor")]
    public static void ShowWindow()
    {
        GetWindow<PlayerPrefsEditor>("PlayerPrefs Editor");
    }

    // ウィンドウが有効になったときに呼ばれる
    private void OnEnable()
    {
        LoadPlayerPrefs();
    }

    // GUIを描画するメインのメソッド
    private void OnGUI()
    {
        DrawToolbar();
        DrawDataView();
        DrawAddNewPrefArea();
    }

    /// <summary>
    /// ツールバーを描画します (リフレッシュ、全削除ボタン)
    /// </summary>
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("リフレッシュ", EditorStyles.toolbarButton))
        {
            LoadPlayerPrefs();
        }

        GUILayout.FlexibleSpace();

        // 全削除ボタン
        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f); // 少し赤くして警告
        if (GUILayout.Button("すべて削除", EditorStyles.toolbarButton))
        {
            if (EditorUtility.DisplayDialog("すべてのPlayerPrefsを削除",
                "本当にすべてのPlayerPrefsを削除しますか？\nこの操作は元に戻せません。",
                "はい、すべて削除します", "キャンセル"))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                LoadPlayerPrefs(); // 表示を更新
            }
        }
        GUI.backgroundColor = Color.white; // 色を元に戻す

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// PlayerPrefsの一覧データを描画します
    /// </summary>
    private void DrawDataView()
    {
        // ヘッダー
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("キー", EditorStyles.boldLabel, GUILayout.MinWidth(150));
        EditorGUILayout.LabelField("型", EditorStyles.boldLabel, GUILayout.Width(60));
        EditorGUILayout.LabelField("値", EditorStyles.boldLabel, GUILayout.MinWidth(150));
        EditorGUILayout.LabelField("操作", EditorStyles.boldLabel, GUILayout.Width(120));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2);

        // スクロールビュー
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        // 削除対象のアイテムを一時的に保持するリスト
        var itemsToRemove = new List<PlayerPrefData>();

        if (_prefsData.Count == 0)
        {
             EditorGUILayout.HelpBox("保存されているPlayerPrefsはありません。\nまたは、Windows以外の環境ではキーが自動検出されません。", MessageType.Info);
        }

        foreach (var pref in _prefsData)
        {
            EditorGUILayout.BeginHorizontal();

            // キー (読み取り専用)
            EditorGUILayout.LabelField(new GUIContent(pref.Key, pref.Key), GUILayout.MinWidth(150));

            // 型 (読み取り専用)
            EditorGUILayout.LabelField(pref.Type.ToString(), GUILayout.Width(60));

            // 値 (編集可能)
            pref.StringValue = EditorGUILayout.TextField(pref.StringValue, GUILayout.MinWidth(150));

            // 保存ボタン
            if (GUILayout.Button("保存", GUILayout.Width(55)))
            {
                if (SavePref(pref))
                {
                    GUI.FocusControl(null); // フォーカスを外して編集を確定
                }
            }

            // 削除ボタン
            if (GUILayout.Button("削除", GUILayout.Width(55)))
            {
                if (EditorUtility.DisplayDialog("PlayerPrefを削除",
                    $"キー '{pref.Key}' を削除しますか？",
                    "はい", "いいえ"))
                {
                    PlayerPrefs.DeleteKey(pref.Key);
                    PlayerPrefs.Save();
                    itemsToRemove.Add(pref);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        // 削除対象のアイテムをメインリストから削除
        if (itemsToRemove.Count > 0)
        {
            _prefsData.RemoveAll(itemsToRemove.Contains);
        }
    }

    /// <summary>
    /// 新規PlayerPrefを追加するエリアを描画します
    /// </summary>
    private void DrawAddNewPrefArea()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("新しいPlayerPrefを追加", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        _newKey = EditorGUILayout.TextField("キー", _newKey);
        _newType = (PrefType)EditorGUILayout.EnumPopup("型", _newType);
        _newValue = EditorGUILayout.TextField("値", _newValue);

        EditorGUILayout.Space(5);

        GUI.backgroundColor = new Color(0.7f, 1f, 0.7f); // 少し緑色に
        if (GUILayout.Button("追加して保存", GUILayout.Height(25)))
        {
            if (string.IsNullOrEmpty(_newKey))
            {
                EditorUtility.DisplayDialog("エラー", "キーは空にできません。", "OK");
                return;
            }
            if (PlayerPrefs.HasKey(_newKey))
            {
                 EditorUtility.DisplayDialog("エラー", $"キー '{_newKey}' は既に存在します。", "OK");
                return;
            }
            
            var newPrefData = new PlayerPrefData
            {
                Key = _newKey,
                Type = _newType,
                StringValue = _newValue
            };

            if (SavePref(newPrefData, isNew: true))
            {
                _prefsData.Add(newPrefData);
                _prefsData = _prefsData.OrderBy(p => p.Key).ToList(); // ソート
                
                // 入力フィールドをリセット
                _newKey = "";
                _newValue = "";
            }
        }
        GUI.backgroundColor = Color.white; // 色を元に戻す

        EditorGUILayout.EndVertical();
    }


    /// <summary>
    /// 指定されたPlayerPrefデータを保存します
    /// </summary>
    private bool SavePref(PlayerPrefData pref, bool isNew = false)
    {
        try
        {
            switch (pref.Type)
            {
                case PrefType.Int:
                    int intValue = int.Parse(pref.StringValue);
                    PlayerPrefs.SetInt(pref.Key, intValue);
                    pref.Value = intValue;
                    break;
                case PrefType.Float:
                    // グローバリゼーションに関わらず'.'を小数点として解釈
                    float floatValue = float.Parse(pref.StringValue, CultureInfo.InvariantCulture);
                    PlayerPrefs.SetFloat(pref.Key, floatValue);
                    pref.Value = floatValue;
                    break;
                case PrefType.String:
                    PlayerPrefs.SetString(pref.Key, pref.StringValue);
                    pref.Value = pref.StringValue;
                    break;
            }
            PlayerPrefs.Save();

            if (!isNew)
            {
                Debug.Log($"PlayerPref '{pref.Key}' が正常に保存されました。");
            }
            else
            {
                 Debug.Log($"PlayerPref '{pref.Key}' が正常に追加されました。");
            }

            return true;
        }
        catch (FormatException)
        {
            EditorUtility.DisplayDialog("保存エラー", $"値のフォーマットが型 '{pref.Type}' と一致しません。", "OK");
            // 既存の値の場合は、編集中の文字列を元の値に戻す
            if (!isNew)
            {
               pref.StringValue = pref.Value.ToString();
            }
            return false;
        }
    }

    /// <summary>
    /// PlayerPrefsのデータを読み込みます。
    /// 現在はWindows Editorでのみキーの自動検出をサポートしています。
    /// </summary>
    private void LoadPlayerPrefs()
    {
        _prefsData.Clear();

#if UNITY_EDITOR_WIN
        // Windowsではレジストリからキーを読み込む
        // パス: HKEY_CURRENT_USER\Software\Unity\UnityEditor\[companyName]\[productName]
        string registryKeyPath = $@"Software\Unity\UnityEditor\{PlayerSettings.companyName}\{PlayerSettings.productName}";
        try
        {
            using (var key = Registry.CurrentUser.OpenSubKey(registryKeyPath))
            {
                if (key == null)
                {
                    // このプロジェクトでまだPlayerPrefsが保存されていない場合
                    Repaint();
                    return;
                }

                foreach (string valueName in key.GetValueNames())
                {
                    // Unityはキーの末尾にハッシュ値を追加するため、それを除去する
                    string actualKey = System.Text.RegularExpressions.Regex.Replace(valueName, @"_h\d+$", "");
                    
                    if (_prefsData.Any(p => p.Key == actualKey)) continue;

                    var data = new PlayerPrefData { Key = actualKey };
                    var valueKind = key.GetValueKind(valueName);

                    // レジストリの型情報からPlayerPrefsの型を推測する
                    if (valueKind == RegistryValueKind.DWord) // int型
                    {
                        data.Type = PrefType.Int;
                        data.Value = PlayerPrefs.GetInt(actualKey);
                    }
                    else if (valueKind == RegistryValueKind.String) // string型
                    {
                        data.Type = PrefType.String;
                        data.Value = PlayerPrefs.GetString(actualKey);
                    }
                    else // それ以外 (多くはfloat型がBinaryとして保存される)
                    {
                        data.Type = PrefType.Float;
                        data.Value = PlayerPrefs.GetFloat(actualKey);
                    }
                    
                    data.StringValue = data.Value.ToString();
                    _prefsData.Add(data);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"レジストリからのPlayerPrefs読み込みに失敗しました: {ex.Message}");
        }
#else
        // Windows以外のプラットフォームでは、キーの自動検出が困難なため何もしない。
        // 新規追加機能を使って手動で操作することは可能。
#endif

        // 表示順を安定させるためにキーでソート
        _prefsData = _prefsData.OrderBy(p => p.Key).ToList();
        Repaint(); // ウィンドウを再描画
    }
}