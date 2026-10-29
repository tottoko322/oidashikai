using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CatalogRefreshChecks
{
    static bool running;
    static double started;
    static int checks;
    static Camera camera;
    static RenderTexture target;
    static Canvas canvas;
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static CatalogRefreshChecks()
    {
        EditorApplication.update += () => {
            if (!running) return;
            EditorApplication.QueuePlayerLoopUpdate();
            if (EditorApplication.isPaused) EditorApplication.isPaused = false;
            if (EditorApplication.timeSinceStartup-started > 90) EditorApplication.Exit(1);
        };
        EditorApplication.playModeStateChanged += state => {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("CatalogChecks",false)) return;
            SessionState.SetBool("CatalogChecks",false);running=true;started=EditorApplication.timeSinceStartup;
            new GameObject("CatalogChecksHost").AddComponent<CoinTossController>().StartCoroutine(Safe(Suite()));
        };
    }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        foreach (var guid in AssetDatabase.FindAssets("t:CardData", new[]{"Assets/Data/Card"}))
            AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
        SessionState.SetBool("CatalogChecks",true);EditorApplication.EnterPlaymode();
    }
    static void Check(bool condition,string message)
    {
        if(!condition)throw new Exception(message);
        checks++;Debug.Log("CATALOG PASS: "+message);
    }
    static IEnumerator Safe(IEnumerator suite)
    {
        while(true)
        {
            bool more; object value;
            try {more=suite.MoveNext();value=more?suite.Current:null;}
            catch(Exception e){File.WriteAllText("catalog-checks-result.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);yield break;}
            if(!more)break;yield return value;
        }
        File.WriteAllText("catalog-checks-result.txt",checks+" checks passed");EditorApplication.Exit(0);
    }
    static GameObject UI(string name,Transform parent)
    {var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);return go;}
    static void Capture(string name)
    {
        Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
        var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
        Directory.CreateDirectory("../../outputs");File.WriteAllBytes("../../outputs/"+name+".png",image.EncodeToPNG());
        RenderTexture.active=null;UnityEngine.Object.Destroy(image);
    }
    static Sprite SpriteAt(string path)=>AssetDatabase.LoadAssetAtPath<Sprite>(path);
    static IEnumerator Suite()
    {
        camera=new GameObject("Camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.02f,.07f,.14f);
        target=new RenderTexture(1280,800,24);camera.targetTexture=target;
        var root=UI("Canvas",null);canvas=root.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=5;
        root.AddComponent<CanvasScaler>().referenceResolution=new Vector2(1280,800);root.AddComponent<GraphicRaycaster>();
        var font=GameFonts.Japanese;Check(font!=null,"supplied Japanese TTF creates a dynamic SDF");
        var cards=AssetDatabase.FindAssets("t:CardData",new[]{"Assets/Data/Card"}).Select(g=>AssetDatabase.LoadAssetAtPath<CardData>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
        Check(cards.Length==13,"all 13 cards are present");
        foreach(var card in cards)
        {
            Check(!string.IsNullOrWhiteSpace(card.encyclopediaDescription),card.name+" has a description");
            var text=card.encyclopediaDescription.Replace("\n","").Replace("\r","");
            Check(font.TryAddCharacters(text,out string missing),card.name+" Japanese glyphs exist: "+missing);
        }
        Check(GameFonts.Default.fallbackFontAssetTable.Contains(font),"display font has Japanese fallback");
        Check(SpriteAt("Assets/image/Kouza.png").rect.height==4000,"Kouza imports full new sprite");
        Check(SpriteAt("Assets/image/Bushitu (1).png").rect.height==4000,"Bushitu imports full new sprite");
        var bushitu=cards.Single(c=>c.name=="bushitu");var kouza=cards.Single(c=>c.name=="kouza");
        Check(bushitu.artwork==SpriteAt("Assets/image/Bushitu (1).png"),"Bushitu no longer uses placeholder artwork");
        Check(kouza.artwork==SpriteAt("Assets/image/Kouza.png"),"Kouza uses replacement artwork");

        var pageHost=new GameObject("Rules").AddComponent<RulePanelController>();
        pageHost.carousel=pageHost.gameObject.AddComponent<CarouselController>();
        pageHost.descImage=UI("OldPage",root.transform).AddComponent<Image>();
        pageHost.image1=SpriteAt("Assets/image/RuleSlide/How_To_Play1.png");pageHost.image2=SpriteAt("Assets/image/RuleSlide/How_To_Play2.png");
        var left=UI("Left",root.transform).AddComponent<Image>().gameObject.AddComponent<Button>();left.onClick.AddListener(pageHost.OnPrev);
        var right=UI("Right",root.transform).AddComponent<Image>().gameObject.AddComponent<Button>();right.onClick.AddListener(pageHost.OnNext);
        UI("Dismiss",root.transform).AddComponent<Image>().gameObject.AddComponent<Button>();
        yield return null;yield return null;
        Check(pageHost.carousel.Count==2,"only two supplied pages are navigable");
        Check(pageHost.descImage.sprite==pageHost.image1,"rules begin on page one");Capture("new-rules-preview");
        right.onClick.Invoke();Check(pageHost.descImage.sprite==pageHost.image2,"visible right arrow advances page");
        right.onClick.Invoke();Check(pageHost.carousel.Index==0,"no obsolete third or fourth page");
        left.onClick.Invoke();Check(pageHost.carousel.Index==1,"visible left arrow goes back");
        Check(right.transform.parent==pageHost.descImage.transform,"arrow hit areas follow fitted page");
        UnityEngine.Object.Destroy(pageHost.descImage.gameObject);UnityEngine.Object.Destroy(pageHost.gameObject);
        yield return null;

        var art=UI("CardArtwork",root.transform).AddComponent<Image>();art.preserveAspect=true;art.sprite=bushitu.artwork;
        art.rectTransform.sizeDelta=new Vector2(410,600);art.rectTransform.anchoredPosition=new Vector2(-340,0);
        var panel=UI("DescriptionPanel",root.transform).AddComponent<Image>();panel.color=new Color(.04f,.16f,.26f);
        panel.rectTransform.sizeDelta=new Vector2(570,330);panel.rectTransform.anchoredPosition=new Vector2(250,0);
        var prose=UI("Description",panel.transform).AddComponent<TextMeshProUGUI>();prose.rectTransform.anchorMin=Vector2.zero;prose.rectTransform.anchorMax=Vector2.one;prose.rectTransform.offsetMin=Vector2.one*24;prose.rectTransform.offsetMax=Vector2.one*-24;prose.fontSize=32;prose.color=new Color(.65f,.94f,1);prose.alignment=TextAlignmentOptions.TopLeft;
        DicManager.descText=prose;DicManager.ShowDescription(bushitu);yield return null;
        Check(prose.text==bushitu.encyclopediaDescription&&prose.font==font,"dictionary displays selected CardData prose in Japanese face");
        Capture("new-dictionary-preview");
        var view=UI("DictionaryCard",root.transform).AddComponent<CardView>();view.art=art;view.Bind(kouza);
        DicManager.cardView=art.gameObject;DicManager.allLoaded=true;
        var click=view.gameObject.AddComponent<CardOnClick>();click.btn=view.gameObject;click.OnButtonClick();
        Check(prose.text==kouza.encyclopediaDescription,"clicking a second dictionary card updates its description");
        UnityEngine.Object.Destroy(art.gameObject);UnityEngine.Object.Destroy(panel.gameObject);UnityEngine.Object.Destroy(view.gameObject);
        yield return null;

        var frame=UI("OpponentFrame",root.transform).AddComponent<Image>();
        var frames=AssetDatabase.LoadAllAssetsAtPath("Assets/image/Frame_Dead.png").OfType<Sprite>();frame.sprite=frames.Single(s=>s.name=="Frame_Dead_1");
        frame.rectTransform.sizeDelta=new Vector2(560,350);frame.rectTransform.anchoredPosition=new Vector2(0,225);
        var portrait=UI("Opponent",root.transform).AddComponent<Image>();portrait.preserveAspect=true;
        portrait.rectTransform.sizeDelta=Vector2.one*100;portrait.rectTransform.localScale=Vector3.one*3;portrait.rectTransform.anchoredPosition=new Vector2(0,225);
        var hit=new GameObject("Hit").AddComponent<CharacterHitVfx>();hit.targetImage=portrait;hit.targetRect=portrait.rectTransform;hit.Rebase();
        PortraitFrameMask.Enclose(hit,frame.rectTransform);
        Check(portrait.GetComponentInParent<Mask>()!=null,"opponent has a circular stencil clip");
        var first=portrait.transform.parent;PortraitFrameMask.Enclose(hit,frame.rectTransform);Check(first==portrait.transform.parent,"reapplying selection does not stack masks");
        foreach(var path in new[]{"Assets/image/nano.png","Assets/image/pico.png"})
        {
            portrait.sprite=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First();
            yield return null;Capture(path.Contains("nano")?"opponent-nano-preview":"opponent-pico-preview");
            var before=portrait.rectTransform.anchoredPosition;hit.Play();yield return new WaitForSeconds(.55f);
            Check(Vector2.Distance(before,portrait.rectTransform.anchoredPosition)<.01f,path+" hit animation returns inside frame");
        }
    }
}
