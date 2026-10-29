using System;
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MediaUpdateChecks
{
    static bool running; static double started; static int checks;
    static MediaUpdateChecks()
    {
        EditorApplication.update += () => {
            if(!running)return; EditorApplication.QueuePlayerLoopUpdate();
            if(EditorApplication.isPaused)EditorApplication.isPaused=false;
            if(EditorApplication.timeSinceStartup-started>60)EditorApplication.Exit(1);
        };
        EditorApplication.playModeStateChanged += s => {
            if(s!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("MediaChecks",false))return;
            SessionState.SetBool("MediaChecks",false);running=true;started=EditorApplication.timeSinceStartup;
            new GameObject("MediaChecks").AddComponent<CoinTossController>().StartCoroutine(Safe(Suite()));
        };
    }
    static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;Debug.Log("MEDIA PASS: "+message);}
    public static void Run()
    {
        try
        {
            var rulePath=File.Exists("Assets/MediaValidation/Rule.unity")?"Assets/MediaValidation/Rule.unity":"Assets/Scenes/Rule.unity";
            EditorSceneManager.OpenScene(rulePath);
            var rules=UnityEngine.Object.FindAnyObjectByType<RulePanelController>();
            Check(rules!=null,"Rule scene has controller");
            var pages=new[]{rules.image1,rules.image2,rules.image3,rules.image4}.Concat(rules.additionalPages).ToArray();
            Check(pages.Length==7,"Rule scene serializes exactly seven pages");
            for(int i=0;i<7;i++)Check(pages[i]==Page(i+1),"Rule page "+(i+1)+" is in correct order");
            var titlePath=File.Exists("Assets/MediaValidation/title.unity")?"Assets/MediaValidation/title.unity":"Assets/Scenes/title.unity";
            EditorSceneManager.OpenScene(titlePath);
            var audio=UnityEngine.Object.FindAnyObjectByType<AudioManager>();
            Check(audio!=null&&audio.bgmClip==Clip(),"title scene references completed title theme");
            Check(Mathf.Abs(Clip().length-75f)<.1f&&Clip().channels==2,"completed WAV imports as 75-second stereo clip");
            var card=AssetDatabase.LoadAssetAtPath<CardData>("Assets/Data/Card/sengakoukankai.asset");
            Check(card.artwork==AssetDatabase.LoadAssetAtPath<Sprite>("Assets/image/Sengakoukan.png"),"line-art exchange card references full replacement sprite");
            Check(card.artwork.rect.size==new Vector2(3000,4000),"replacement card is uncropped");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            SessionState.SetBool("MediaChecks",true);EditorApplication.EnterPlaymode();
        }
        catch(Exception e){File.WriteAllText("media-checks-result.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static Sprite Page(int n)=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/image/RuleSlide/How_To_Play"+n+".png");
    static AudioClip Clip()=>AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/TitleTheme.wav");
    static IEnumerator Safe(IEnumerator suite)
    {
        while(true){bool more;object value;try{more=suite.MoveNext();value=more?suite.Current:null;}catch(Exception e){File.WriteAllText("media-checks-result.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);yield break;}if(!more)break;yield return value;}
        File.WriteAllText("media-checks-result.txt",checks+" checks passed");EditorApplication.Exit(0);
    }
    static IEnumerator Suite()
    {
        var camera=new GameObject("Camera").AddComponent<Camera>();var target=new RenderTexture(1280,800,24);camera.targetTexture=target;
        var root=new GameObject("Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=5;
        var controller=new GameObject("Rules").AddComponent<RulePanelController>();controller.carousel=controller.gameObject.AddComponent<CarouselController>();
        var image=new GameObject("Page",typeof(RectTransform),typeof(Image));image.transform.SetParent(root.transform,false);controller.descImage=image.GetComponent<Image>();
        controller.image1=Page(1);controller.image2=Page(2);controller.image3=Page(3);controller.image4=Page(4);controller.additionalPages=new[]{Page(5),Page(6),Page(7)};
        yield return null;yield return null;
        Check(controller.carousel.Count==7,"runtime carousel includes seven pages");
        for(int i=1;i<=7;i++)
        {
            Check(controller.descImage.sprite==Page(i),"runtime next displays page "+i);
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
            var png=new Texture2D(1280,800,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,1280,800),0,0);png.Apply();
            Directory.CreateDirectory("../../outputs");File.WriteAllBytes("../../outputs/tutorial-page-"+i+".png",png.EncodeToPNG());UnityEngine.Object.Destroy(png);RenderTexture.active=null;
            controller.OnNext();yield return null;
        }
        Check(controller.carousel.Index==0,"last page wraps to first");controller.OnPrev();Check(controller.descImage.sprite==Page(7),"previous from first shows seventh");
        var source=new GameObject("CompletedTheme").AddComponent<AudioSource>();source.gameObject.AddComponent<AudioListener>();source.clip=Clip();source.loop=true;source.volume=0;source.Play();
        yield return new WaitForSeconds(.2f);
        Check(source.clip.loadState==AudioDataLoadState.Loaded,"completed title track loads successfully");
        Check(source.isPlaying&&source.timeSamples>0&&source.loop,"completed title track plays and advances with looping");source.Stop();
    }
}
