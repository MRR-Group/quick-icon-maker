using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class QuickIconMaker : EditorWindow
{
    [System.Serializable]
    public class IconPreset
    {
        public Vector2 rotation;
        public float rotationZ;
        public Vector3 offset;
        public float distance;
        public float lightIntensity;
        public float lightRotationY;
        public Color bgColor;
    }

    private GameObject targetPrefab;
    private PreviewRenderUtility previewUtility;
    
    private Vector2 dragRotation = new Vector2(125, -20);
    private float rotationZ = 0f;
    private Vector3 cameraOffset = Vector3.zero;
    private float distance = 5f;
    private float lightIntensity = 1.4f;
    private float lightRotationY = 40f;
    private Color bgColor = new Color(0, 0, 0, 0);

    private string presetName = "Preset Name";
    private bool isSaving = false;
	private static readonly string PresetPath = Path.Combine("Assets", "Settings", "QuickIconMaker");
    
	[MenuItem("Assets/Tools/Take Screenshot", false, 0)]
    public static void OpenWindow()
    {
        var window = GetWindow<QuickIconMaker>("Quick Icon Maker");
        window.targetPrefab = Selection.activeGameObject;
    }

    private void OnEnable()
    {
        InitPreview();
    }

    private void OnDisable()
    {
        if (previewUtility != null)
        {
            previewUtility.Cleanup();
        }
    }

    private void InitPreview()
    {
        previewUtility ??= new PreviewRenderUtility();
     
        var cameraData = previewUtility.camera.GetComponent<UniversalAdditionalCameraData>() 
                         ?? previewUtility.camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
        
        previewUtility.camera.nearClipPlane = 0.01f;
        previewUtility.camera.farClipPlane = 1000f;
    }

    public void CreateGUI()
    {
        var root = rootVisualElement;

        var presetBox = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row, 
                backgroundColor = new Color(0.25f, 0.25f, 0.25f), 
                paddingTop = 5, 
                paddingBottom = 5, 
                paddingLeft = 5, 
                paddingRight = 5
            }
        };
        
        var nameField = new TextField("Name")
        {
            value = presetName, 
            style = { flexGrow = 1 }
        };
        
        nameField.RegisterValueChangedCallback(evt => presetName = evt.newValue);
        
        var saveBtn = new Button(SaveCurrentPreset)
        {
            text = "Save", 
            style = { width = 50 }
        };

        var loadBtn = new Button(ShowPresetMenu)
        {
            text = "Load \u25be", 
            style = { width = 50 }
        };

        var helpBtn = new Button(ShowHelpDialog)
        {
            text = "?", 
            style =
            {
                width = 25, 
                backgroundColor = new Color(0.3f, 0.3f, 0.5f), 
                color = Color.white
            }
        };
        
        presetBox.Add(nameField); 
        presetBox.Add(saveBtn); 
        presetBox.Add(loadBtn); 
        presetBox.Add(helpBtn);
        root.Add(presetBox);

        root.Add(new IMGUIContainer(OnPreviewGUI) { style = { flexGrow = 1 } });

        var lightPanel = new VisualElement
        {
            style =
            {
                paddingLeft = 10, 
                paddingRight = 10, 
                backgroundColor = new Color(0.18f, 0.18f, 0.18f)
            }
        };

        var intensitySlider = new Slider("Light Power", 0, 5) { value = lightIntensity };
        intensitySlider.RegisterValueChangedCallback(evt => { lightIntensity = evt.newValue; Repaint(); });

        var rotSlider = new Slider("Light Dir", 0, 360) { value = lightRotationY };
        rotSlider.RegisterValueChangedCallback(evt => { lightRotationY = evt.newValue; Repaint(); });

        lightPanel.Add(intensitySlider); 
        lightPanel.Add(rotSlider);
        root.Add(lightPanel);

        var footer = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row, 
                paddingTop = 5, 
                paddingBottom = 5, 
                paddingLeft = 5, 
                paddingRight = 5,
                backgroundColor = new Color(0.12f, 0.12f, 0.12f)
            }
        };

        var colorField = new ColorField("BG") { value = bgColor };
        colorField.RegisterValueChangedCallback(evt => bgColor = evt.newValue);

        var centerBtn = new Button(() => 
        { 
            cameraOffset = Vector3.zero; 
            dragRotation = new Vector2(125, -20); 
            rotationZ = 0f; 
            Repaint(); 
        }) 
        { 
            text = "RESET" 
        };

        var generateBtn = new Button(SaveIcon)
        {
            text = "GENERATE", 
            style =
            {
                flexGrow = 1, 
                backgroundColor = new Color(0.15f, 0.4f, 0.15f), 
                color = Color.white
            }
        };

        footer.Add(colorField); 
        footer.Add(centerBtn); 
        footer.Add(generateBtn);
        root.Add(footer);
    }

    private void ShowHelpDialog()
    {
        var helpMessage = "ROTATION:\n" +
                          "- LPM Drag: Free Rotation\n" +
                          "- Alt + LMB: Rotate X axis only\n" +
                          "- Alt + RMB: Rotate Y axis only\n" +
                          "- Alt + Ctrl + LMB: Rotate Z axis only\n\n" +
                          "CAMERA MOVEMENT:\n" +
                          "- Shift + LMB: Free Pan\n" +
                          "- Ctrl + LMB: Move Up/Down\n" +
                          "- Ctrl + RMB: Move Left/Right\n" +
                          "- Scroll / Shift + Arrows: Zoom\n" +
                          "- Arrow Keys: Precise Pan";

        EditorUtility.DisplayDialog("Quick Icon Maker", helpMessage, "Close");
    }

    private void OnPreviewGUI()
    {
        if (targetPrefab == null || isSaving)
        {
            return;
        }

        if (previewUtility == null)
        {
            InitPreview();
        }
        
        var rect = GUILayoutUtility.GetRect(10, 10, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        HandleInput(rect);

        if (Event.current.type == EventType.Repaint)
        {
            previewUtility.BeginPreview(rect, GUIStyle.none);
            SetupScene();
            previewUtility.camera.Render();
            
            var result = previewUtility.EndPreview();

            if (result != null)
            {
                GUI.DrawTexture(rect, result, ScaleMode.StretchToFill, false);
            }
        }
    }

    private void HandleInput(Rect rect)
    {
        var e = Event.current;

        if (!rect.Contains(e.mousePosition) && e.type != EventType.KeyDown)
        {
            return;
        }

        switch (e.type)
        {
            case EventType.MouseDrag:
                HandleMouseDrag(e);
                break;
            case EventType.KeyDown:
                HandleKeyDown(e);
                break;
            case EventType.ScrollWheel:
                HandleScroll(e);
                break;
        }
    }

    private void HandleMouseDrag(Event e)
    {
        var moveSpeed = 0.01f * (distance / 5f);

        if (e.alt)
        {
            if (e.control && e.button == 0)
            {
                rotationZ += e.delta.x;
            }
            else if (e.button == 0)
            {
                dragRotation.y += e.delta.y;
            }
            else if (e.button == 1)
            {
                dragRotation.x += e.delta.x;
            }
        }
        else if (e.shift && e.button == 0)
        {
            cameraOffset.x -= e.delta.x * moveSpeed;
            cameraOffset.y += e.delta.y * moveSpeed;
        }
        else if (e.control && e.button == 0)
        {
            cameraOffset.y += e.delta.y * moveSpeed;
        }
        else if (e.control && e.button == 1)
        {
            cameraOffset.x -= e.delta.x * moveSpeed;
        }
        else if (e.button == 0)
        {
            dragRotation.x += e.delta.x;
            dragRotation.y += e.delta.y;
        }

        e.Use();
        Repaint();
    }

    private void HandleKeyDown(Event e)
    {
        var step = e.shift ? 0.5f : 0.2f;

        if (e.keyCode == KeyCode.UpArrow)
        {
            if (e.shift)
            {
                distance -= step;
            }
            else
            {
                cameraOffset.y += step;
            }
        }

        if (e.keyCode == KeyCode.DownArrow)
        {
            if (e.shift)
            {
                distance += step;
            }
            else
            {
                cameraOffset.y -= step;
            }
        }

        if (e.keyCode == KeyCode.LeftArrow)
        {
            cameraOffset.x -= step;
        }

        if (e.keyCode == KeyCode.RightArrow)
        {
            cameraOffset.x += step;
        }

        e.Use();
        Repaint();
    }

    private void HandleScroll(Event e)
    {
        distance = Mathf.Clamp(distance + e.delta.y * 0.3f, 0.1f, 5000f);
        e.Use();
        Repaint();
    }

    private void SetupScene()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.black;

        previewUtility.camera.backgroundColor = bgColor;
        previewUtility.camera.clearFlags = CameraClearFlags.SolidColor;
        previewUtility.camera.transform.position = cameraOffset - Vector3.forward * distance;
        previewUtility.camera.transform.rotation = Quaternion.identity;
        
        if (previewUtility.lights[0] != null)
        {
            previewUtility.lights[0].intensity = lightIntensity;
            previewUtility.lights[0].enabled = true;
            previewUtility.lights[0].transform.rotation = Quaternion.Euler(40, lightRotationY, 0);
        }

        foreach (var obj in previewUtility.camera.scene.GetRootGameObjects())
        {
            if (obj.GetComponent<Camera>() == null && obj.GetComponent<Light>() == null)
            {
                DestroyImmediate(obj);
            }
        }

        var instance = previewUtility.InstantiatePrefabInScene(targetPrefab);
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.Euler(dragRotation.y, -dragRotation.x, rotationZ);
    }

    private void SaveCurrentPreset()
    {
        if (!Directory.Exists(PresetPath))
        {
            Directory.CreateDirectory(PresetPath);
        }

        var settings = new IconPreset
        {
            rotation = dragRotation, 
            rotationZ = rotationZ, 
            offset = cameraOffset, 
            distance = distance, 
            lightIntensity = lightIntensity, 
            lightRotationY = lightRotationY, 
            bgColor = bgColor
        }; 

        File.WriteAllText($"{PresetPath}/{presetName}.json", JsonUtility.ToJson(settings)); 
        AssetDatabase.Refresh();
    }

    private void ShowPresetMenu()
    {
        var menu = new GenericMenu();

        if (!Directory.Exists(PresetPath))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(PresetPath, "*.json"))
        {
            var name = Path.GetFileNameWithoutExtension(file); 
            menu.AddItem(new GUIContent(name), false, () => LoadPreset(file));
        }

        menu.ShowAsContext();
    }

    private void LoadPreset(string path)
    {
        var s = JsonUtility.FromJson<IconPreset>(File.ReadAllText(path));

        dragRotation = s.rotation; 
        rotationZ = s.rotationZ; 
        cameraOffset = s.offset; 
        distance = s.distance;
        lightIntensity = s.lightIntensity; 
        lightRotationY = s.lightRotationY; 
        bgColor = s.bgColor; 
        presetName = Path.GetFileNameWithoutExtension(path);

        Repaint();
    }

    private void SaveIcon()
    {
        isSaving = true;

        var path = EditorUtility.SaveFilePanel("Save Icon", "Assets", targetPrefab.name + "_Icon.png", "png");

        if (string.IsNullOrEmpty(path))
        {
            isSaving = false;
            return;
        }

        var rt = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        previewUtility.camera.targetTexture = rt;
        SetupScene();

        GL.sRGBWrite = true;
        previewUtility.camera.Render();
        GL.sRGBWrite = false;

        RenderTexture.active = rt;
        var tex = new Texture2D(512, 512, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, 512, 512), 0, 0); 
        tex.Apply();

        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.Refresh();

        var rel = "Assets" + path.Substring(Application.dataPath.Length);
        var imp = AssetImporter.GetAtPath(rel) as TextureImporter;

        if (imp != null)
        {
            imp.textureType = TextureImporterType.Sprite; 
            imp.alphaIsTransparency = true; 
            imp.SaveAndReimport();
        }

        RenderTexture.active = null;
        previewUtility.camera.targetTexture = null;
        DestroyImmediate(rt);
        isSaving = false;
        
        Close();
    }
}