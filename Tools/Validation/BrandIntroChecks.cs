using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class BrandIntroChecks
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static int checks; static bool running; static double started;
    static BrandIntroChecks()
    {
        EditorApplication.update+=()=>{
            if(!running)return;EditorApplication.QueuePlayerLoopUpdate();if(EditorApplication.isPaused)EditorApplication.isPaused=false;
            if(EditorApplication.timeSinceStartup-started>30)EditorApplication.Exit(1);
        };
        EditorApplication.playModeStateChanged+=state=>{
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("BrandChecks",false))return;
            SessionState.SetBool("BrandChecks",false);running=true;started=EditorApplication.timeSinceStartup;
            new GameObject("BrandChecksHost").AddComponent<CoinTossController>().StartCoroutine(Safe(Suite()));
        };
    }
    public static void Run(){SessionState.SetBool("BrandChecks",true);EditorApplication.EnterPlaymode();}
    static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;Debug.Log("BRAND PASS: "+message);}
    static IEnumerator Safe(IEnumerator suite)
    {
        var stack=new Stack<IEnumerator>();stack.Push(suite);
        while(stack.Count>0){bool moved;object current=null;
            try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}
            catch(Exception e){Time.timeScale=1;System.IO.File.WriteAllText("brand-checks-result.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);yield break;}
            if(!moved){stack.Pop();continue;}if(current is IEnumerator nested)stack.Push(nested);else yield return current;
        }
        System.IO.File.WriteAllText("brand-checks-result.txt","PASS "+checks);Debug.Log("ALL BRAND CHECKS PASSED: "+checks);EditorApplication.Exit(0);
    }
    static IEnumerator Suite()
    {
        var events=EventSystem.current!=null?EventSystem.current:new GameObject("Events").AddComponent<EventSystem>();
        var selected=new GameObject("TitleButton",typeof(RectTransform),typeof(Button));events.SetSelectedGameObject(selected);
        var host=new GameObject("Intro",typeof(RectTransform));var intro=host.AddComponent<BrandIntroUI>();
        Check(intro.Build()&&intro.IsBuilt,"Provided logos and shader load and build");
        Check(events.currentSelectedGameObject==null,"Intro clears keyboard focus from title buttons");
        Check(host.GetComponent<Canvas>().sortingOrder>32500,"Intro covers title UI");
        Check(host.transform.Find("Backdrop").GetComponent<Image>().raycastTarget,"Intro blocks clicks on the title underneath");
        var q=(Material)typeof(BrandIntroUI).GetField("qMaterial",Private).GetValue(intro);
        var i=(Material)typeof(BrandIntroUI).GetField("iMaterial",Private).GetValue(intro);
        Check(q!=null&&i!=null&&i.GetFloat("_WhiteKey")==1,"Impression white background is handled at rendering time");
        intro.RenderAt(0);Check(q.GetFloat("_Reveal")==0,"Logos start hidden");
        intro.RenderAt(1);Check(q.GetFloat("_Reveal")>0&&q.GetFloat("_Reveal")<1,"Scan progressively reveals the supplied shapes");
        intro.RenderAt(2.6f);Check(q.GetFloat("_Reveal")==1&&i.GetFloat("_Reveal")==1,"Both logos fully resolve before the hold");
        Check(host.transform.Find("Wordmark").GetComponent<CanvasGroup>().alpha==1,"Full organization name is readable during the hold");
        foreach(var label in host.GetComponentsInChildren<TMP_Text>(true))Check(GameFonts.Default.HasCharacters(label.text),"Intro label has every font glyph: "+label.name);
        intro.RenderAt(BrandIntroUI.Duration);Check(host.GetComponent<CanvasGroup>().alpha==0,"End fades to the title");
        intro.Skip();yield return null;Check(intro!=null,"Opening click cannot skip immediately");
        Time.timeScale=0;float before=(float)typeof(BrandIntroUI).GetField("elapsed",Private).GetValue(intro);
        yield return new WaitForSecondsRealtime(0.1f);
        Check((float)typeof(BrandIntroUI).GetField("elapsed",Private).GetValue(intro)>before,"Intro advances independently of gameplay time");Time.timeScale=1;
        typeof(BrandIntroUI).GetField("elapsed",Private).SetValue(intro,0.8f);intro.Skip();yield return null;
        Check(intro==null,"Skip removes the complete overlay");Check(events.currentSelectedGameObject==selected,"Title keyboard focus is restored after skipping");
        var staticFlags=BindingFlags.Static|BindingFlags.NonPublic;
        typeof(BrandIntroUI).GetMethod("ResetSession",staticFlags).Invoke(null,null);
        var handler=typeof(BrandIntroUI).GetMethod("SceneLoaded",staticFlags);
        var battle=SceneManager.CreateScene("BrandBattleFixture");handler.Invoke(null,new object[]{battle,LoadSceneMode.Single});
        Check(UnityEngine.Object.FindObjectsByType<BrandIntroUI>(FindObjectsSortMode.None).Length==0,"Non-title scenes do not trigger the intro");
        var title=SceneManager.CreateScene("title");handler.Invoke(null,new object[]{title,LoadSceneMode.Single});
        Check(UnityEngine.Object.FindObjectsByType<BrandIntroUI>(FindObjectsSortMode.None).Length==1,"First title entry creates the intro");
        handler.Invoke(null,new object[]{title,LoadSceneMode.Single});
        Check(UnityEngine.Object.FindObjectsByType<BrandIntroUI>(FindObjectsSortMode.None).Length==1,"Returning to title does not replay or duplicate it");
    }
}
