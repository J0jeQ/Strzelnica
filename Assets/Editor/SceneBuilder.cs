using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Jump;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using Unity.XR.CoreUtils;
using TMPro;

// Menu "Tools/Zbuduj scenę gry" – buduje całą grę (budkę jak na jarmarku) w otwartej scenie.
// Wszystko trafia pod jeden obiekt "Gra", więc można to uruchamiać wielokrotnie.
public static class SceneBuilder
{
    [MenuItem("Tools/Zbuduj scenę gry")]
    public static void Build()
    {
        // W trybie Play zmiany w scenie i tak znikną po zatrzymaniu gry
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Zbuduj scenę gry", "Najpierw zatrzymaj Play, potem zbuduj scenę.", "OK");
            return;
        }
        // 1. Tag "Floor" (jeśli go nie ma)
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tags = tagManager.FindProperty("tags");
        bool hasFloor = false;
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == "Floor") hasFloor = true;
        if (!hasFloor)
        {
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = "Floor";
            tagManager.ApplyModifiedProperties();
        }

        // 2. Sprzątanie: usuń poprzednią wersję gry i niepotrzebne obiekty z szablonu VR
        string[] toRemove = { "Gra", "Interactables", "UI", "Environment", "Teleport Area Setup" };
        foreach (GameObject go in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            if (System.Array.IndexOf(toRemove, go.name) >= 0) Undo.DestroyObjectImmediate(go);

        // 3. Gracz (XR Origin) stoi na środku świata i patrzy w stronę +Z
        XROrigin origin = Object.FindAnyObjectByType<XROrigin>();
        if (origin != null)
        {
            Undo.RecordObject(origin.transform, "Ustaw gracza");
            origin.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            // Bez gogli (symulator) nikt nie podnosi kamery – ustawiamy wysokość oczu 1.6 m.
            // Na prawdziwych goglach XR Origin i tak nadpisze to prawdziwą wysokością.
            Transform cameraOffset = origin.CameraFloorOffsetObject.transform;
            Undo.RecordObject(cameraOffset, "Wysokość kamery");
            cameraOffset.localPosition = new Vector3(0f, 1.6f, 0f);
            // Ukryj dymki z podpowiedziami (Move/Grab/UI Press) na kontrolerach – zasłaniały tarcze –
            // i podświetlenie końcówki kontrolera z szablonu (nie ma ustawionego motywu i sypało błędami).
            foreach (Transform t in origin.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Affordance Callouts") || t.name == "Poke Point Affordances")
                {
                    Undo.RecordObject(t.gameObject, "Ukryj podpowiedzi");
                    t.gameObject.SetActive(false);
                }
            // Bez sztucznego ruchu (mniej choroby symulatorowej): gracz stoi przy ladzie.
            // Wyłączamy chodzenie gałką (DynamicMoveProvider dziedziczy po ContinuousMoveProvider),
            // skok, płynny obrót i teleport. Zostaje obrót skokowy (SnapTurnProvider).
            foreach (MonoBehaviour m in origin.GetComponentsInChildren<MonoBehaviour>(true))
                if (m is ContinuousMoveProvider || m is JumpProvider || m is ContinuousTurnProvider || m is TeleportationProvider)
                {
                    Undo.RecordObject(m, "Wyłącz sztuczny ruch");
                    m.enabled = false;
                }
        }
        GameObject root = new GameObject("Gra");
        root.AddComponent<DesktopThrow>(); // rzucanie klawiszem F do testów bez gogli
        Transform r = root.transform;

        // 4. Materiały (tekstury rysuje TextureMaker.cs i zapisuje w Assets/Textures)
        Texture2D planksTex = TextureMaker.Planks();
        Material grass = TexMat(TextureMaker.Grass(), new Vector2(40f, 40f), 0.1f);
        Material floorPlanks = TexMat(planksTex, new Vector2(4.6f, 3.8f), 0.25f);
        Material counterTop = TexMat(planksTex, new Vector2(4.7f, 0.5f), 0.45f);
        Material wood = TexMat(planksTex, new Vector2(0.25f, 2f), 0.3f);
        Material counterFront = TexMat(TextureMaker.CounterPanel(), new Vector2(7.5f, 1f), 0.4f);
        Material stripes = TexMat(TextureMaker.Stripes(), new Vector2(7f, 1f), 0.2f);
        Material starWall = TexMat(TextureMaker.Stars(), new Vector2(4f, 1.67f), 0.2f);
        Material redPaint = Mat(new Color(0.65f, 0.08f, 0.1f), 0.5f);
        Material white = Mat(new Color(0.95f, 0.93f, 0.88f), 0.5f);
        Material gold = Mat(new Color(1f, 0.75f, 0.15f), 0.8f, 0.8f, 0.4f);

        // 5. Ziemia i podłoga budki (obie z tagiem Floor = pudło)
        Make(PrimitiveType.Plane, "Trawa", r, new Vector3(0f, 0f, 10f), new Vector3(8f, 1f, 8f), grass).tag = "Floor";
        Make(PrimitiveType.Cube, "Deski", r, new Vector3(0f, 0.01f, 4.7f), new Vector3(9.2f, 0.02f, 7.6f), floorPlanks).tag = "Floor";

        // 6. Budka: lada, ściany, słupki i markiza w paski
        Make(PrimitiveType.Cube, "Lada", r, new Vector3(0f, 0.475f, 0.75f), new Vector3(9.2f, 0.95f, 0.4f), counterFront);
        Make(PrimitiveType.Cube, "Blat", r, new Vector3(0f, 0.98f, 0.72f), new Vector3(9.4f, 0.06f, 0.55f), counterTop);
        Make(PrimitiveType.Cube, "Sciana tylna", r, new Vector3(0f, 2f, 8.6f), new Vector3(9.6f, 4f, 0.2f), starWall);
        Make(PrimitiveType.Cube, "Sciana lewa", r, new Vector3(-4.7f, 2f, 4.75f), new Vector3(0.2f, 4f, 7.5f), stripes);
        Make(PrimitiveType.Cube, "Sciana prawa", r, new Vector3(4.7f, 2f, 4.75f), new Vector3(0.2f, 4f, 7.5f), stripes);
        Make(PrimitiveType.Cube, "Slupek lewy", r, new Vector3(-4.7f, 2.1f, 0.8f), new Vector3(0.25f, 4.2f, 0.25f), wood);
        Make(PrimitiveType.Cube, "Slupek prawy", r, new Vector3(4.7f, 2.1f, 0.8f), new Vector3(0.25f, 4.2f, 0.25f), wood);
        for (int i = 0; i < 10; i++)
        {
            GameObject stripe = Make(PrimitiveType.Cube, "Markiza " + (i + 1), r, new Vector3(-4.23f + i * 0.94f, 4.1f, 0.7f),
                                     new Vector3(0.94f, 0.05f, 1.3f), i % 2 == 0 ? redPaint : white);
            stripe.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);
        }

        // 7. Żarówki na górze ścian i ciepłe światło w budce
        Material bulbA = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = new Color(1f, 0.9f, 0.5f) };
        Material bulbB = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = new Color(1f, 0.5f, 0.2f) };
        for (int i = 0; i < 13; i++) Bulb(r, new Vector3(-4.2f + i * 0.7f, 4.15f, 8.45f), i % 2 == 0 ? bulbA : bulbB);
        for (int i = 0; i < 8; i++)
        {
            Bulb(r, new Vector3(-4.6f, 4.15f, 1.4f + i), i % 2 == 0 ? bulbA : bulbB);
            Bulb(r, new Vector3(4.6f, 4.15f, 1.4f + i), i % 2 == 0 ? bulbB : bulbA);
        }
        MakeLight(r, new Vector3(-2f, 3.6f, 4f));
        MakeLight(r, new Vector3(2f, 3.6f, 4f));
        MakeLight(r, new Vector3(0f, 2.5f, 1.5f)); // oświetla ladę z piłkami

        // Słońce: ciepłe popołudniowe światło zza pleców gracza (oświetla wnętrze budki)
        Light sun = RenderSettings.sun;
        if (sun != null)
        {
            Undo.RecordObject(sun.transform, "Słońce");
            Undo.RecordObject(sun, "Słońce");
            sun.transform.rotation = Quaternion.Euler(40f, 20f, 0f);
            sun.color = new Color(1f, 0.88f, 0.7f);
            sun.intensity = 1.1f;
        }

        // Dekoracje festynu dookoła (osobny plik: FestivalBuilder.cs)
        FestivalBuilder.Build(r);

        // 8. Cztery kolorowe piłki na blacie
        Color[] ballColors = { new Color(1f, 0.5f, 0.1f), new Color(0.2f, 0.55f, 1f), new Color(0.3f, 0.85f, 0.35f), new Color(0.9f, 0.3f, 0.8f) };
        for (int i = 0; i < 4; i++)
        {
            GameObject ball = Make(PrimitiveType.Sphere, "Pilka " + (i + 1), r, new Vector3(-0.45f + i * 0.3f, 1.07f, 0.7f),
                                   Vector3.one * 0.11f, Mat(ballColors[i], 0.75f));
            Rigidbody rb = ball.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            XRGrabInteractable grab = ball.AddComponent<XRGrabInteractable>();
            grab.throwOnDetach = true;
            ball.AddComponent<Ball>();
        }

        // 9. Tarcze z pierścieniami: 4 zwykłe (czerwone) i 1 złota (mniejsza, ruchoma)
        float[] xs = { -2.4f, -0.8f, 0.8f, 2.4f };
        float[] ys = { 1.2f, 1.6f, 1.6f, 1.2f };
        for (int i = 0; i < 4; i++)
            MakeTarget("Tarcza " + (i + 1), r, new Vector3(xs[i], ys[i], 6.6f), 0.6f, redPaint, white, 10, 0f);
        MakeTarget("Zlota tarcza", r, new Vector3(0f, 2f, 6.4f), 0.35f, gold, white, 30, 1.5f);

        // 10. Tablica w złotej ramie na tylnej ścianie
        Make(PrimitiveType.Cube, "Rama tablicy", r, new Vector3(0f, 3.25f, 8.49f), new Vector3(3.95f, 1.85f, 0.03f), gold);
        Make(PrimitiveType.Cube, "Tlo tablicy", r, new Vector3(0f, 3.25f, 8.47f), new Vector3(3.75f, 1.65f, 0.03f), Mat(new Color(0.03f, 0.03f, 0.05f), 0.6f));
        GameObject canvasGo = new GameObject("Tablica", typeof(RectTransform));
        canvasGo.transform.SetParent(r, false);
        canvasGo.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.localPosition = new Vector3(0f, 3.25f, 8.44f);
        canvasRect.sizeDelta = new Vector2(600f, 260f);
        canvasRect.localScale = Vector3.one * 0.0062f;

        // 11. GameManager z podpiętymi tekstami
        GameManager gm = new GameObject("GameManager").AddComponent<GameManager>();
        gm.transform.SetParent(r, false);
        gm.pointsText = MakeText("Punkty", canvasGo.transform, new Vector2(-190f, 92f), new Vector2(220f, 60f), new Color(1f, 0.85f, 0.2f));
        gm.timeText = MakeText("Czas", canvasGo.transform, new Vector2(0f, 92f), new Vector2(160f, 60f), Color.white);
        gm.streakText = MakeText("Seria", canvasGo.transform, new Vector2(190f, 92f), new Vector2(220f, 60f), new Color(1f, 0.55f, 0.25f));
        gm.messageText = MakeText("Komunikat", canvasGo.transform, new Vector2(0f, -35f), new Vector2(570f, 170f), Color.white);
        gm.messageText.fontSizeMax = 42f;
        gm.gameObject.AddComponent<ScreenshotHotkey>(); // F12 w trakcie gry = zrzut ekranu

        // 12. Symulator XR do testów bez gogli (tag EditorOnly = nie trafi do buildu na gogle)
        foreach (string guid in AssetDatabase.FindAssets("XR Interaction Simulator t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith("/XR Interaction Simulator.prefab")) continue;
            GameObject sim = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), r);
            sim.tag = "EditorOnly";
            break;
        }
        if (r.Find("XR Interaction Simulator") == null)
            Debug.LogWarning("Brak symulatora XR – zaimportuj sample 'XR Interaction Simulator' w Package Managerze.");

        Undo.RegisterCreatedObjectUndo(root, "Zbuduj scenę gry");
        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log("Scena gry zbudowana. Zapisz scenę (Ctrl/Cmd+S).");
    }

    // Materiał URP/Lit: kolor, połysk, metaliczność i świecenie (glow > 0).
    public static Material Mat(Color color, float smooth, float metal = 0f, float glow = 0f)
    {
        Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.color = color;
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Metallic", metal);
        if (glow > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color * glow);
        }
        return m;
    }

    // Materiał z teksturą; tiling = ile razy tekstura powtarza się na ścianie obiektu.
    public static Material TexMat(Texture2D texture, Vector2 tiling, float smooth)
    {
        Material m = Mat(Color.white, smooth);
        m.mainTexture = texture;
        m.mainTextureScale = tiling;
        return m;
    }

    // Tworzy prymityw z podanym materiałem.
    public static GameObject Make(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    // Świecąca żarówka (bez kolizji – piłka przez nią przeleci).
    static void Bulb(Transform parent, Vector3 pos, Material mat)
    {
        GameObject b = Make(PrimitiveType.Sphere, "Zarowka", parent, pos, Vector3.one * 0.16f, mat);
        Object.DestroyImmediate(b.GetComponent<Collider>());
    }

    // Ciepłe światło punktowe w budce.
    static void MakeLight(Transform parent, Vector3 pos)
    {
        Light light = new GameObject("Swiatlo").AddComponent<Light>();
        light.transform.SetParent(parent, false);
        light.transform.localPosition = pos;
        light.type = LightType.Point;
        light.color = new Color(1f, 0.85f, 0.65f);
        light.range = 8f;
        light.intensity = 3f;
    }

    // Tarcza = płaski cylinder obrócony przodem do gracza + dwa pierścienie (tylko wygląd).
    static void MakeTarget(string name, Transform parent, Vector3 pos, float size, Material outer, Material ring, int points, float amplitude)
    {
        GameObject t = Make(PrimitiveType.Cylinder, name, parent, pos, new Vector3(size, 0.03f, size), outer);
        t.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Object.DestroyImmediate(t.GetComponent<CapsuleCollider>()); // kapsuła byłaby kulą – zamieniamy
        t.AddComponent<MeshCollider>().convex = true;
        // pierścienie lekko wysunięte w stronę gracza (lokalna oś -Y cylindra)
        Ring(t.transform, 0.66f, -0.3f, ring);
        Ring(t.transform, 0.33f, -0.6f, outer);
        Target target = t.AddComponent<Target>();
        target.points = points;
        target.moveAmplitude = amplitude;
    }

    static void Ring(Transform target, float scale, float offset, Material mat)
    {
        GameObject ring = Make(PrimitiveType.Cylinder, "Pierscien", target, new Vector3(0f, offset, 0f), new Vector3(scale, 1f, scale), mat);
        Object.DestroyImmediate(ring.GetComponent<Collider>());
    }

    // Tworzy tekst TextMeshPro na tablicy.
    static TextMeshProUGUI MakeText(string name, Transform canvas, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(canvas, false);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.rectTransform.anchoredPosition = pos;
        text.rectTransform.sizeDelta = size;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 34f;
        text.enableAutoSizing = true; // za długi napis zmniejszy się zamiast łamać
        text.fontSizeMin = 16f;
        text.fontSizeMax = 34f;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.text = name;
        return text;
    }
}
