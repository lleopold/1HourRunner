#if KWS_BUILTIN && KWS_DEBUG

using KWS;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

public class DemoGUI : MonoBehaviour
{
    public bool UseEditorCamera;
    public bool UseOptimisationMode = false;
    public bool UseForceEffector    = false;
    public bool UseSourceEffector   = false;
    public bool UseInteraction      = false;
    
    public GameObject FlowPrefab;
    public GameObject ForcePrefab;
    public GameObject DynamicObject;

    public Terrain[]         Terrains;
    public PostProcessVolume Volume;
   
    Camera            cam;
    GameObject        selected;
    Plane             dragPlane; 
    Vector3           offset;
    
    private bool _drawState;
    
    public void NextScene()
    {
        int current = SceneManager.GetActiveScene().buildIndex;
        int total   = SceneManager.sceneCountInBuildSettings;

        int next = (current + 1) % total; // зациклено: после последней вернётся в первую
        SceneManager.LoadScene(next);
    }

    public void PreviousScene()
    {
        int current = SceneManager.GetActiveScene().buildIndex;
        int total   = SceneManager.sceneCountInBuildSettings;

        int prev = (current - 1 + total) % total; // зациклено: до первой вернётся в последнюю
        SceneManager.LoadScene(prev);
    }

    void Start()
    {
        cam              = GetComponent<Camera>();
        _drawState       = true;
    }

    void Update()
    {
        
        if (Input.GetKeyDown(KeyCode.Alpha2)) NextScene();
        if (Input.GetKeyDown(KeyCode.Alpha1)) PreviousScene();
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Vector3 forwardXZ = cam.transform.forward;
            forwardXZ.y = 0;
            forwardXZ.Normalize();

            Vector3 pos = cam.transform.position 
                        + forwardXZ  * Random.Range(5, 15f) 
                        + Vector3.up * Random.Range(5, 15f); 

            var instance = Instantiate(DynamicObject, pos, Random.rotation);
            instance.transform.localScale *= Random.Range(1, 5);
        }
        if (Input.GetKeyDown(KeyCode.R)) OptimizeModeSwitch();
        
        if (UseInteraction || UseSourceEffector || UseForceEffector)
        {


            if (Input.GetMouseButtonDown(0))
            {

                if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out var hit, 500f))
                {

                    if (hit.collider.GetComponent<KWS_DynamicWavesSimulationEffector>() != null)
                    {
                        selected  = hit.collider.gameObject;
                        dragPlane = new Plane(Vector3.up, selected.transform.position);

                        float enter;
                        dragPlane.Raycast(cam.ScreenPointToRay(Input.mousePosition), out enter);
                        offset = selected.transform.position - cam.ScreenPointToRay(Input.mousePosition).GetPoint(enter);
                    }
                }
            }

            if (Input.GetMouseButton(0) && selected != null)
            {
                float enter;
                if (dragPlane.Raycast(cam.ScreenPointToRay(Input.mousePosition), out enter))
                {
                    Vector3 hitPoint = cam.ScreenPointToRay(Input.mousePosition).GetPoint(enter);
                    selected.transform.position = hitPoint + offset;
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                selected = null;
            }
        }
    }

    void OptimizeModeSwitch()
    {
        _drawState = !_drawState;

        foreach (var terrain in Terrains)
        {
            terrain.drawTreesAndFoliage = _drawState;
            terrain.drawTreesAndFoliage = _drawState;
        }

        if (Volume.profile.TryGetSettings(out AmbientOcclusion ao))
        {
            ao.active = _drawState;
        }
    }
    
    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize         = 24;
        style.normal.textColor = Color.white;

        DrawOutlineLabel(new Rect(20, 20, 400, 30), "Press 1/2 to switch scenes", style, Color.white, Color.black);

        if (UseEditorCamera)
        {
            DrawOutlineLabel(new Rect(20, 70, 800, 200),
                             "Camera Controls:\n"     +
                             "WASD - move\n"          +
                             "Mouse - look around\n"  +
                             "Shift - fast move\n"    +
                             "Q / E - move down/up\n" +
                             "Mouse Scroll - zoom\n",
                             style, Color.white, Color.black);
            
            DrawOutlineLabel(new Rect(20, 260, 800, 200),
                             "Space - Create Dynamic Object\n" +
                             "R - Enable/Disable Vegetation",
                             style, Color.white, Color.black);
        }

       
        
        if (UseInteraction)
        {
            GUI.Label(new Rect(500, 20, 1500, 50), "Left mouse button - grab and move rocks, flow sources, force objects", style);
        }

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 20;

       
        if (UseOptimisationMode)
        {
            if (GUI.Button(new Rect(20, 70, 180, 35), _drawState ? "Disable Vegetation" : "Enable Vegetation", buttonStyle))
            {
                OptimizeModeSwitch();
            }
        }

        if (UseSourceEffector)
        {

            if (GUI.Button(new Rect(20, 120, 180, 35), "Add Flow Source", buttonStyle))
            {
                var instance = Instantiate(FlowPrefab);
                var offset   = Random.insideUnitCircle * 10f;
                instance.transform.position += new Vector3(offset.x, 0, offset.y);
            }
        }

        if (UseForceEffector)
        {
            if (GUI.Button(new Rect(20, 170, 180, 35), "Add Force Object", buttonStyle))
            {
                var instance = Instantiate(ForcePrefab);
                var offset   = Random.insideUnitCircle * 10f;
                instance.transform.position += new Vector3(offset.x, 0, offset.y);
            }
        }

    }
    
    void DrawOutlineLabel(Rect rect, string text, GUIStyle style, Color textColor, Color outlineColor, int thickness = 2)
    {
        // сохраним оригинальный цвет
        var backup = style.normal.textColor;

        // рисуем несколько копий для обводки
        style.normal.textColor = outlineColor;
        for (int x = -thickness; x <= thickness; x++)
        {
            for (int y = -thickness; y <= thickness; y++)
            {
                if (x == 0 && y == 0) continue;
                GUI.Label(new Rect(rect.x + x, rect.y + y, rect.width, rect.height), text, style);
            }
        }

        // рисуем основной текст
        style.normal.textColor = textColor;
        GUI.Label(rect, text, style);

        // вернём цвет
        style.normal.textColor = backup;
    }
    
    
}

#endif