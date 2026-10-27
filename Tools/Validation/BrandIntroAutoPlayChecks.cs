using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Run only in the isolated validation project: saves a minimal title fixture.
[InitializeOnLoad]
public static class BrandIntroAutoPlayChecks
{
    static bool checking; static double entered, visibleSince=-1;
    static BrandIntroAutoPlayChecks()
    {
        EditorApplication.playModeStateChanged+=state=>{
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("BrandAutoPlayChecks",false))return;
            SessionState.SetBool("BrandAutoPlayChecks",false);checking=true;entered=EditorApplication.timeSinceStartup;
        };
        EditorApplication.update+=()=>{
            if(!checking)return;
            EditorApplication.QueuePlayerLoopUpdate();if(EditorApplication.isPaused)EditorApplication.isPaused=false;
            try
            {
                if(SceneManager.GetActiveScene().name!="title")throw new Exception("Play did not open title");
                var intros=UnityEngine.Object.FindObjectsByType<BrandIntroUI>(FindObjectsSortMode.None);
                if(intros.Length==1&&intros[0].IsBuilt)
                {
                    if(visibleSince<0)visibleSince=EditorApplication.timeSinceStartup;
                    if(EditorApplication.timeSinceStartup-visibleSince>0.6)
                    {
                        checking=false;File.WriteAllText("brand-auto-play-result.txt","PASS: title Play automatically created one visible intro and kept it visible; no replay/menu call");
                        Debug.Log("BRAND AUTO PLAY PASSED");EditorApplication.Exit(0);
                    }
                }
                else if(visibleSince>=0)throw new Exception("Intro vanished immediately after Play");
                if(EditorApplication.timeSinceStartup-entered>8)throw new Exception("No automatic intro appeared");
            }
            catch(Exception error)
            {
                checking=false;File.WriteAllText("brand-auto-play-result.txt",error.ToString());Debug.LogException(error);EditorApplication.Exit(1);
            }
        };
    }
    public static void Run()
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        new GameObject("TitleFlow").AddComponent<SceneFlowManager>();
        Directory.CreateDirectory("Assets/BrandIntroValidation");EditorSceneManager.SaveScene(scene,"Assets/BrandIntroValidation/title.unity");
        SessionState.SetBool("BrandAutoPlayChecks",true);EditorApplication.EnterPlaymode();
    }
}
