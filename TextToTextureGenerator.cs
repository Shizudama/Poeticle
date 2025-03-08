#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro;

public class TextToTextureGenerator : EditorWindow
{
    private string inputText = "Hello, Unity!";
    private TMP_FontAsset fontAsset;
    private int fontSize = 600;
    private Color textColor = Color.white;
    private Vector2 textureSize = new Vector2(1024, 128);
    private string savePath = "Assets/TextTextures/";
    private TextAlignmentOptions textAlignment = TextAlignmentOptions.Center;
    
    [MenuItem("Tools/Text To Texture Generator")]
    public static void ShowWindow()
    {
        GetWindow<TextToTextureGenerator>("Text To Texture");
    }
    
    private void OnGUI()
    {
        // UI for text input and settings
        inputText = EditorGUILayout.TextField("Text", inputText);
        fontAsset = (TMP_FontAsset)EditorGUILayout.ObjectField("Font Asset", fontAsset, typeof(TMP_FontAsset), false);
        fontSize = EditorGUILayout.IntField("Font Size", fontSize);
        textColor = EditorGUILayout.ColorField("Text Color", textColor);
        textureSize = EditorGUILayout.Vector2Field("Texture Size", textureSize);
        
        // テキストアライメントのオプション
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Text Alignment");
        if (GUILayout.Toggle(textAlignment == TextAlignmentOptions.Left, "Left", EditorStyles.miniButtonLeft))
            textAlignment = TextAlignmentOptions.Left;
        if (GUILayout.Toggle(textAlignment == TextAlignmentOptions.Center, "Center", EditorStyles.miniButtonMid))
            textAlignment = TextAlignmentOptions.Center;
        if (GUILayout.Toggle(textAlignment == TextAlignmentOptions.Right, "Right", EditorStyles.miniButtonRight))
            textAlignment = TextAlignmentOptions.Right;
        EditorGUILayout.EndHorizontal();
        
        savePath = EditorGUILayout.TextField("Save Path", savePath);
        
        if (GUILayout.Button("Generate Texture"))
        {
            GenerateTextTexture();
        }
    }
    
    private void GenerateTextTexture()
    {
        // Create temporary game objects
        GameObject tmpObj = new GameObject("TMP_TextureGenerator");
        TextMeshPro textMesh = tmpObj.AddComponent<TextMeshPro>();
        
        // Setup TextMeshPro
        textMesh.font = fontAsset;
        textMesh.text = inputText;
        textMesh.fontSize = fontSize;
        textMesh.color = textColor;
        textMesh.alignment = textAlignment;
        textMesh.enableWordWrapping = false;
        textMesh.overflowMode = TextOverflowModes.Overflow;
        textMesh.horizontalAlignment = HorizontalAlignmentOptions.Center;
        textMesh.verticalAlignment = VerticalAlignmentOptions.Middle;
        textMesh.rectTransform.sizeDelta = textureSize;
        
        // 重要: RectTransformを中央に配置
        textMesh.rectTransform.anchoredPosition = Vector2.zero;
        textMesh.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        textMesh.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        textMesh.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        
        // Setup camera to render text
        GameObject cameraObj = new GameObject("RenderCamera");
        Camera renderCamera = cameraObj.AddComponent<Camera>();
        renderCamera.orthographic = true;
        renderCamera.orthographicSize = textureSize.y / 2;
        renderCamera.clearFlags = CameraClearFlags.SolidColor;
        renderCamera.backgroundColor = new Color(0, 0, 0, 0); // Transparent background
        
        // カメラ位置を調整 - テキストの中央を見るように配置
        renderCamera.transform.position = new Vector3(0, 0, -10);
        
        // Create render texture
        RenderTexture renderTexture = new RenderTexture((int)textureSize.x, (int)textureSize.y, 24);
        renderCamera.targetTexture = renderTexture;
        
        // Render to texture
        renderCamera.Render();
        
        // Read pixels from render texture
        RenderTexture.active = renderTexture;
        Texture2D texture2D = new Texture2D((int)textureSize.x, (int)textureSize.y, TextureFormat.RGBA32, false);
        texture2D.ReadPixels(new Rect(0, 0, textureSize.x, textureSize.y), 0, 0);
        texture2D.Apply();
        RenderTexture.active = null;
        
        // Save texture to file
        System.IO.Directory.CreateDirectory(savePath);
        string fileName = savePath + "Text_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
        byte[] bytes = texture2D.EncodeToPNG();
        System.IO.File.WriteAllBytes(fileName, bytes);
        AssetDatabase.Refresh();
        
        // Cleanup
        DestroyImmediate(tmpObj);
        DestroyImmediate(cameraObj);
        renderTexture.Release();
        
        Debug.Log("Texture saved to " + fileName);
        
        // Select the generated texture
        TextureImporter importer = AssetImporter.GetAtPath(fileName) as TextureImporter;
        if (importer != null)
        {
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Texture2D>(fileName);
    }
}
#endif