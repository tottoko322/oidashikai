using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Deterministic uGUI animation: exact supplied logos, no video decoder or plugin.
public class BrandIntroUI : MonoBehaviour
{
    public const float Duration = 4.2f;
    private static bool played;
    private CanvasGroup opacity;
    private RectTransform qpic, impression, scan, ring;
    private CanvasGroup qGroup, iGroup, wordGroup, ampGroup;
    private Image qGhost, iGhost;
    private Image[] grid;
    private Image[] tracers;
    private Material qMaterial, iMaterial;
    private Sprite qSprite, iSprite;
    private float elapsed;
    private bool clockStarted;
    private GameObject previousSelection;
    public bool IsBuilt => opacity != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { played = false; SceneManager.sceneLoaded -= SceneLoaded; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bind() { SceneManager.sceneLoaded -= SceneLoaded; SceneManager.sceneLoaded += SceneLoaded; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CheckInitialScene()
    {
        // Also cover entering Play directly with an already-open title scene.
        for (int n=0;n<SceneManager.sceneCount;n++) SceneLoaded(SceneManager.GetSceneAt(n),LoadSceneMode.Single);
    }
    private static void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureForScene(scene);
    }

    public static void EnsureForScene(Scene scene)
    {
        if (played || !string.Equals(scene.name, "title", StringComparison.OrdinalIgnoreCase)) return;
        Create(scene);
    }

    private static BrandIntroUI Create(Scene scene)
    {
        var host = new GameObject("QpicImpressionIntro", typeof(RectTransform));
        SceneManager.MoveGameObjectToScene(host, scene);
        var intro = host.AddComponent<BrandIntroUI>();
        if (!intro.Build()) { Destroy(host); return null; }
        played = true;
        return intro;
    }

    public static bool Replay()
    {
        if (!Application.isPlaying) return false;
        var scene=SceneManager.GetActiveScene();
        if (!string.Equals(scene.name,"title",StringComparison.OrdinalIgnoreCase))
        { Debug.LogWarning("[BrandIntro] Open the title scene to preview the intro."); return false; }
        foreach(var intro in FindObjectsByType<BrandIntroUI>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if (intro.gameObject.scene!=scene || !intro.IsBuilt) continue;
            intro.elapsed=0; intro.clockStarted=false; intro.gameObject.SetActive(true); intro.RenderAt(0); played=true; return true;
        }
        return Create(scene)!=null;
    }

    public bool Build()
    {
        if (IsBuilt) return true;
        var q = Resources.Load<Texture2D>("BrandIntro/QpicMark");
        var i = Resources.Load<Texture2D>("BrandIntro/ImpressionMark");
        var shader = Resources.Load<Shader>("BrandIntro/LogoReveal");
        if (q == null || i == null || shader == null) { Debug.LogWarning("[BrandIntro] Missing logo resources; continue to title."); return false; }
        var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32760;
        var scaler = gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280,720); scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>(); opacity = gameObject.AddComponent<CanvasGroup>();
        if (EventSystem.current != null) { previousSelection=EventSystem.current.currentSelectedGameObject; EventSystem.current.SetSelectedGameObject(null); }
        var background = Box("Backdrop", transform, Vector2.zero, Vector2.zero, new Color(0.012f,0.025f,0.05f));
        background.rectTransform.anchorMin = Vector2.zero; background.rectTransform.anchorMax = Vector2.one;
        background.rectTransform.sizeDelta = Vector2.zero; background.raycastTarget = true;
        grid = new Image[31]; int index = 0;
        for (int x=-720;x<=720;x+=80) grid[index++]=Box("GridV",transform,new Vector2(x,0),new Vector2(1,1000),new Color(0.2f,0.65f,0.8f,0.05f));
        for (int y=-440;y<=440;y+=80) grid[index++]=Box("GridH",transform,new Vector2(0,y),new Vector2(1600,1),new Color(0.2f,0.65f,0.8f,0.05f));
        var ringObject = new GameObject("OrbitalCircuit", typeof(RectTransform)); ring = (RectTransform)ringObject.transform; ring.SetParent(transform,false);
        for (int n=0;n<48;n++)
        {
            float angle=n*7.5f*Mathf.Deg2Rad;
            var tick=Box("OrbitTick",ring,new Vector2(Mathf.Cos(angle)*290,Mathf.Sin(angle)*290),new Vector2(n%4==0?18:6,1),new Color(0.1f,0.8f,0.95f,0.17f));
            tick.rectTransform.localRotation=Quaternion.Euler(0,0,n*7.5f);
        }
        tracers = new Image[12];
        for (int n=0;n<tracers.Length;n++)
        {
            float y=-220+n*40;
            tracers[n]=Box("DataTrace",transform,new Vector2(n%2==0?-530:530,y),new Vector2(130,2),
                n%2==0?new Color(1f,0.8f,0.1f,0.6f):new Color(0.1f,0.8f,1f,0.6f));
        }
        qMaterial=new Material(shader); iMaterial=new Material(shader); iMaterial.SetFloat("_WhiteKey",1f);
        qSprite=Sprite.Create(q,new Rect(0,0,q.width,q.height),Vector2.one*0.5f,100,0,SpriteMeshType.FullRect);
        iSprite=Sprite.Create(i,new Rect(0,0,i.width,i.height),Vector2.one*0.5f,100,0,SpriteMeshType.FullRect);
        qpic=Logo("Qpic",new Vector2(-240,25),new Vector2(380,380),qSprite,qMaterial,new Color(1f,0.82f,0.05f),out qGroup,out qGhost);
        impression=Logo("Impression",new Vector2(240,25),new Vector2(350,280),iSprite,iMaterial,new Color(0.62f,0.95f,1f),out iGroup,out iGhost);
        var amp = Label("Ampersand","&",new Vector2(0,20),new Vector2(120,140),82,Color.white); ampGroup=amp.gameObject.AddComponent<CanvasGroup>();
        var words=Label("Wordmark","Qpic & impression!",new Vector2(0,-235),new Vector2(1100,74),42,Color.white);
        words.characterSpacing=4; wordGroup=words.gameObject.AddComponent<CanvasGroup>();
        Label("PresentedBy","PRESENTED BY",new Vector2(0,250),new Vector2(480,36),16,new Color(0.48f,0.72f,0.82f));
        Box("WordmarkRail",transform,new Vector2(0,-282),new Vector2(640,1),new Color(0.15f,0.7f,0.85f,0.35f));
        Label("SkipHint","くりっく / Space ですきっぷ",new Vector2(385,-326),new Vector2(450,30),16,new Color(0.42f,0.6f,0.7f));
        scan=Box("ScanBeam",transform,Vector2.zero,new Vector2(940,2),new Color(0.65f,1f,1f,0.75f)).rectTransform;
        RenderAt(0f); return true;
    }

    private RectTransform Logo(string name,Vector2 position,Vector2 size,Sprite sprite,Material material,Color color,out CanvasGroup group,out Image ghost)
    {
        var node=new GameObject(name,typeof(RectTransform),typeof(CanvasGroup)); var rect=(RectTransform)node.transform;
        rect.SetParent(transform,false); rect.anchoredPosition=position; rect.sizeDelta=size; group=node.GetComponent<CanvasGroup>();
        ghost=Box("SignalGhost",rect,new Vector2(8,0),size,new Color(0.1f,0.8f,1f,0.2f)); ghost.sprite=sprite; ghost.material=material; ghost.preserveAspect=true;
        var logo=Box("Logo",rect,Vector2.zero,size,color); logo.sprite=sprite; logo.material=material; logo.preserveAspect=true;
        return rect;
    }

    public void RenderAt(float time)
    {
        if (!IsBuilt) return;
        float boot=Mathf.SmoothStep(0,1,Mathf.Clamp01(time/0.4f));
        float reveal=Mathf.SmoothStep(0,1,Mathf.Clamp01((time-0.45f)/1.25f));
        opacity.alpha=time<3.5f?1f:1f-Mathf.SmoothStep(0,1,Mathf.Clamp01((time-3.5f)/0.7f));
        foreach(var line in grid) { var c=line.color; c.a=0.055f*boot; line.color=c; }
        qGroup.alpha=iGroup.alpha=boot;
        qMaterial.SetFloat("_Reveal",reveal); iMaterial.SetFloat("_Reveal",Mathf.Clamp01((reveal-0.07f)/0.93f));
        float signal=(1-reveal)*Mathf.Sin(time*72f)*9f;
        qpic.anchoredPosition=new Vector2(-240-(1-reveal)*38+signal,25);
        impression.anchoredPosition=new Vector2(240+(1-reveal)*38-signal,25);
        qGhost.rectTransform.anchoredPosition=new Vector2(signal*1.8f,2);
        iGhost.rectTransform.anchoredPosition=new Vector2(-signal*1.8f,-2);
        qGhost.color=iGhost.color=new Color(0.1f,0.85f,1f,(1-reveal)*0.35f);
        ampGroup.alpha=Mathf.SmoothStep(0,1,Mathf.Clamp01((time-1.4f)/0.5f));
        wordGroup.alpha=Mathf.SmoothStep(0,1,Mathf.Clamp01((time-1.8f)/0.5f));
        ring.localRotation=Quaternion.Euler(0,0,time*5f);
        scan.anchoredPosition=new Vector2(0,Mathf.Lerp(-190,225,reveal));
        scan.gameObject.SetActive(time>0.45f && time<1.7f);
        for(int n=0;n<tracers.Length;n++)
        {
            float travel=Mathf.Repeat(time*0.65f+n*0.17f,1f);
            tracers[n].rectTransform.anchoredPosition=new Vector2((n%2==0?-1:1)*Mathf.Lerp(720,460,travel),-220+n*40);
            var c=tracers[n].color; c.a=Mathf.Sin(travel*Mathf.PI)*0.45f*boot; tracers[n].color=c;
        }
    }

    private static Image Box(string name,Transform parent,Vector2 position,Vector2 size,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image)); var image=go.GetComponent<Image>();
        image.rectTransform.SetParent(parent,false); image.rectTransform.anchoredPosition=position; image.rectTransform.sizeDelta=size;
        image.color=color; image.raycastTarget=false; return image;
    }
    private TMP_Text Label(string name,string value,Vector2 position,Vector2 size,float fontSize,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform)); go.transform.SetParent(transform,false);
        var text=go.AddComponent<TextMeshProUGUI>(); GameFonts.Apply(text); text.text=value; text.fontSize=fontSize;
        text.alignment=TextAlignmentOptions.Center; text.color=color; text.raycastTarget=false;
        text.rectTransform.anchoredPosition=position; text.rectTransform.sizeDelta=size; return text;
    }
    private void Update()
    {
        if (!IsBuilt) return;
        if (!AdvanceClock(Time.unscaledDeltaTime)) return;
        if (elapsed>=Duration) Destroy(gameObject);
        else if (Input.GetMouseButtonDown(0)||Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.Escape)) Skip();
    }
    private bool AdvanceClock(float delta)
    {
        // Asset/shader loading can make the first unscaled delta longer than
        // the entire intro. Let its first frame appear before starting time.
        if (!clockStarted) { clockStarted=true; elapsed=0; RenderAt(0); return false; }
        elapsed+=Mathf.Clamp(delta,0f,0.1f);
        RenderAt(elapsed);
        return true;
    }
    public void Skip() { if (elapsed>0.7f) Destroy(gameObject); }
    private void OnDestroy()
    {
        if (EventSystem.current!=null && previousSelection!=null && previousSelection.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(previousSelection);
        if(qMaterial!=null)Destroy(qMaterial); if(iMaterial!=null)Destroy(iMaterial);
        if(qSprite!=null)Destroy(qSprite); if(iSprite!=null)Destroy(iSprite);
    }
}
