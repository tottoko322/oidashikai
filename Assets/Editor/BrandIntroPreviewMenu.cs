using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class BrandIntroPreviewMenu
{
    const string Pending="Qpic.BrandIntro.PreviewPending";
    static BrandIntroPreviewMenu()
    {
        EditorApplication.playModeStateChanged+=state=>{
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Pending,false))return;
            SessionState.SetBool(Pending,false);
            EditorApplication.delayCall+=()=>{ if(EditorApplication.isPlaying)BrandIntroUI.Replay(); };
        };
    }
    [MenuItem("Qpic/ロゴアニメーションを再生")]
    public static void Play()
    {
        if(SceneManager.GetActiveScene().name.ToLowerInvariant()!="title")
        { Debug.LogWarning("[BrandIntro] Assets/Scenes/title.unity を開いてから実行してください。"); return; }
        if(EditorApplication.isPlaying){BrandIntroUI.Replay();return;}
        SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
    }
}
