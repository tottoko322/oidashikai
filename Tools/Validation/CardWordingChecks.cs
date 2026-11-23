using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CardWordingChecks
{
    [Serializable] class Catalog { public Entry[] cards; }
    [Serializable] class Entry { public string name, reading, effect, description, currentAsset; public int attack, defense; }
    static int checks;
    static CardWordingChecks(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("WordingChecks",false)){SessionState.SetBool("WordingChecks",false);CheckAll();}};}
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        foreach(var guid in AssetDatabase.FindAssets("t:CardData",new[]{"Assets/Data/Card"}))AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid),ImportAssetOptions.ForceUpdate);
        SessionState.SetBool("WordingChecks",true);EditorApplication.EnterPlaymode();
    }
    static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;Debug.Log("WORDING PASS: "+message);}
    static void CheckAll()
    {
        try
        {
            string path=File.Exists("Docs/CardTextCatalog.json")?"Docs/CardTextCatalog.json":"../oidashikai/Docs/CardTextCatalog.json";
            var catalog=JsonUtility.FromJson<Catalog>(File.ReadAllText(path));Check(catalog.cards.Length==24,"all 24 source card texts are catalogued");
            var go=new GameObject("DictionaryFixture");go.SetActive(false);var dic=go.AddComponent<DicManager>();
            var root=new GameObject("Canvas",typeof(RectTransform),typeof(Canvas));var canvas=root.GetComponent<Canvas>();
            var camera=new GameObject("Camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.02f,.07f,.15f);
            var target=new RenderTexture(1280,800,24);camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=5;
            var panel=new GameObject("Description",typeof(RectTransform),typeof(Image));panel.transform.SetParent(root.transform,false);var panelRect=panel.GetComponent<RectTransform>();panelRect.sizeDelta=new Vector2(307,301);panel.GetComponent<Image>().color=new Color(.035f,.12f,.20f);
            var label=new GameObject("Text",typeof(RectTransform),typeof(TextMeshProUGUI));label.transform.SetParent(panel.transform,false);var text=label.GetComponent<TextMeshProUGUI>();text.fontSize=26;
            dic.descTextFB=text;dic.descriptionFB=panel;typeof(DicManager).GetMethod("Awake",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(dic,null);
            panel.SetActive(true);var list=new List<Transform>();
            foreach(var entry in catalog.cards.Where(c=>!string.IsNullOrEmpty(c.currentAsset)))
            {
                var card=AssetDatabase.LoadAssetAtPath<CardData>("Assets/Data/Card/"+entry.currentAsset+".asset");
                Check(card.displayName==entry.name&&card.effectDescription==entry.effect&&card.encyclopediaDescription==entry.name+"\n\n"+entry.description,entry.name+" matches source wording");
                Check(card.attack==entry.attack&&card.defense==entry.defense,entry.name+" stats remain unchanged");
                var view=new GameObject(entry.name).AddComponent<CardView>();view.Bind(card);list.Add(view.transform);
                DicManager.ShowDescription(card);Canvas.ForceUpdateCanvases();text.ForceMeshUpdate();
                Check(text.text.Contains(entry.effect)&&text.text.Contains(entry.description),entry.name+" shows effect and introduction");
                Check(GameFonts.Japanese.HasCharacters(text.text.Replace("\n",""),out uint[] missing,true,true),entry.name+" has all Japanese glyphs");
                Check(!text.isTextOverflowing,entry.name+" fits the dictionary description panel");
                if(entry.currentAsset=="bushitu")
                {
                    panelRect.sizeDelta=new Vector2(570,560);text.fontSizeMax=36;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
                    var png=new Texture2D(1280,800,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,1280,800),0,0);png.Apply();Directory.CreateDirectory("../../outputs");File.WriteAllBytes("../../outputs/unified-card-text-preview.png",png.EncodeToPNG());RenderTexture.active=null;
                    panelRect.sizeDelta=new Vector2(307,301);text.fontSizeMax=26;
                }
            }
            Check(list.Count==13,"only the existing 13 cards are playable");
            dic.SortBy(list,"alphabet");
            Check(list[0].name=="いうビーム"&&list.FindIndex(t=>t.name=="QGJ")<list.FindIndex(t=>t.name=="局会"),"alphabetical dictionary uses Japanese readings for English names");
            Check(list.Any(t=>t.name=="射的")&&list.Any(t=>t.name=="BBQ"),"old festival and camp data keep their IDs with new names");
            Check(BattleHistory.ReasonName(DiscardReason.Effect)=="コウカ","history uses unified effect terminology");
            File.WriteAllText("wording-checks-result.txt",checks+" checks passed");EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText("wording-checks-result.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
