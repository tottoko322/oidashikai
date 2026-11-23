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
public static class BattleHistoryChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static int checks; static bool running; static double started;
    static BattleHistoryChecks()
    {
        EditorApplication.update += () => { if (!running) return; EditorApplication.QueuePlayerLoopUpdate(); if(EditorApplication.isPaused)EditorApplication.isPaused=false; if(EditorApplication.timeSinceStartup-started>90)EditorApplication.Exit(1); };
        EditorApplication.playModeStateChanged += state => {
            if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("HistoryChecks",false))return;
            SessionState.SetBool("HistoryChecks",false);running=true;started=EditorApplication.timeSinceStartup;
            new GameObject("HistoryTests").AddComponent<CoinTossController>().StartCoroutine(Safe(Suite()));
        };
    }
    public static void Run(){EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool("HistoryChecks",true);EditorApplication.EnterPlaymode();}
    static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;Debug.Log("HISTORY PASS: "+message);}
    static object Invoke(object o,string n,params object[] args)=>o.GetType().GetMethod(n,Private).Invoke(o,args);
    static void Set(object o,string n,object value)=>o.GetType().GetField(n,Private).SetValue(o,value);
    static T Component<T>(string name) where T:Component{var go=new GameObject(name);go.SetActive(false);return go.AddComponent<T>();}
    static CardData Card(string name,EffectType type=EffectType.None,int attack=3,int defense=2)
    {var c=ScriptableObject.CreateInstance<CardData>();c.displayName=name;c.attack=attack;c.defense=defense;c.effect=ScriptableObject.CreateInstance<EffectData>();c.effect.type=type;c.effect.valueA=1;return c;}
    static BattleStateMachine Battle()
    {
        var b=Component<BattleStateMachine>("FixtureBattle");b.config=ScriptableObject.CreateInstance<GameConfig>();b.config.maxHand=8;b.config.deckLimit=20;b.config.sameCardLimit=2;
        b.handManager=Component<HandManager>("Hand");b.handManager.config=b.config;b.handManager.handRoot=new GameObject("HandRoot",typeof(RectTransform)).GetComponent<RectTransform>();b.handManager.handRoot.gameObject.SetActive(false);
        b.handManager.cardPrefab=new GameObject("CardPrefab",typeof(RectTransform)).AddComponent<CardView>();b.handManager.cardPrefab.gameObject.SetActive(false);
        b.handLayout=Component<HandLayoutController>("Layout");b.handManager.handLayout=b.handLayout;
        b.costManager=Component<CostManager>("Cost");b.costManager.Init(0,5);b.deckManager=Component<DeckManager>("Deck");
        b.cardDealAnimator=Component<CardDealAnimator>("Deal");b.cardDealAnimator.hand=b.handManager;b.cardDealAnimator.deck=b.deckManager;b.cardDealAnimator.layout=b.handLayout;
        Set(b,"<PlayerHP>k__BackingField",10);Set(b,"<EnemyHP>k__BackingField",10);Set(b,"<BattleReady>k__BackingField",true);
        var deck=ScriptableObject.CreateInstance<DeckDefinition>();for(int i=0;i<10;i++)deck.entries.Add(new DeckEntry{card=Card("SECRET_"+i),count=2});
        b.deckManager.SetupFromDefinition(deck);Enemy(b).Setup(deck,b.config);
        typeof(BattleStateMachine).GetField("<I>k__BackingField",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,b);
        return b;
    }
    static EnemyBattleState Enemy(BattleStateMachine b)=>(EnemyBattleState)b.GetType().GetField("enemyState",Private).GetValue(b);
    static void Drain(IEnumerator routine,BattleStateMachine b=null)
    {
        int count=0;while(routine.MoveNext())
        {
            if(++count>1000)throw new Exception("Coroutine waiting indefinitely");
            if(b!=null&&b.IsWaitingForDiscardSelect&&b.handManager.handViews.Count>0)b.SelectDiscardCard(b.handManager.handViews[0]);
            if(b!=null&&b.IsWaitingForDefense&&b.handManager.handViews.Count>0)b.SelectDefenseCard(b.handManager.handViews[0]);
            if(routine.Current is IEnumerator nested)Drain(nested,b);
        }
    }
    static IEnumerator Safe(IEnumerator suite)
    {
        while(true){bool more;object value;try{more=suite.MoveNext();value=more?suite.Current:null;}catch(Exception e){File.WriteAllText("history-checks-result.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);yield break;}if(!more)break;yield return value;}
        File.WriteAllText("history-checks-result.txt",checks+" checks passed");EditorApplication.Exit(0);
    }
    static IEnumerator Suite()
    {
        var b=Battle();var card=Card("Copy");
        b.History.BeginTurn(BattleSide.Player);
        var first=b.handManager.AddCard(card);var second=b.handManager.AddCard(card);
        Drain((IEnumerator)Invoke(b,"CoConsumeHandCard",first,DiscardReason.Discard,""));Drain((IEnumerator)Invoke(b,"CoConsumeHandCard",second,DiscardReason.Discard,""));
        Check(b.History.Discards.Count==2&&b.handManager.handViews.Count==0,"two copies of the same CardData are counted as two physical discards");
        Check(b.History.Discards.All(x=>x.Turn==1&&x.Side==BattleSide.Player&&x.Reason==DiscardReason.Discard),"discard records keep turn, owner and reason");
        var attack=Card("Attack");var used=b.handManager.AddCard(attack);
        // Dragging temporarily removes the view from handViews before commitment.
        b.handManager.BeginDrag(used);
        var playerAction=(IEnumerator)Invoke(b,"CoResolvePlayerCard",used,DropZoneType.Enemy);playerAction.MoveNext();(playerAction as IDisposable)?.Dispose();
        Check(b.History.Discards.Count==3&&b.History.Discards.Last().Reason==DiscardReason.Attack,"accepted drag attack reaches the pile even when removed from hand during dragging");
        b=Battle();var effectCard=b.handManager.AddCard(Card("Heal",EffectType.Heal));
        var effect=(IEnumerator)Invoke(b,"CoResolvePlayerCard",effectCard,DropZoneType.Effect);effect.MoveNext();(effect as IDisposable)?.Dispose();
        Check(b.History.Discards.Count==1&&b.History.Discards[0].Reason==DiscardReason.Effect,"player effect card goes to its own pile");
        b=Battle();b.handManager.AddCard(Card("Defense",defense:4));Drain((IEnumerator)Invoke(b,"CoDefendPlayer",3),b);
        Check(b.History.Discards.Count==1&&b.History.Discards[0].Reason==DiscardReason.Defense&&b.PlayerHP==10,"player defense consumes and records exactly one card");
        b=Battle();Enemy(b).hand.Add(Card("EnemyDefense",defense:4));Drain((IEnumerator)Invoke(b,"CoDefendCpu",3),b);
        Check(b.History.DiscardCount(BattleSide.Enemy)==1&&b.History.Discards[0].Reason==DiscardReason.Defense,"CPU defense reaches the enemy pile");
        b=Battle();Enemy(b).hand.Add(Card("CpuAttack"));b.turnSystem.SetFirst(TurnOwner.Enemy);Set(b,"cpuWasFirst",true);Set(b,"firstEnemyTurnStarted",false);
        var cpu=(IEnumerator)Invoke(b,"CoEnemyAction");cpu.MoveNext();(cpu as IDisposable)?.Dispose();
        Check(b.History.DiscardCount(BattleSide.Enemy)==1&&b.History.Discards[0].Reason==DiscardReason.Attack,"CPU accepted attack reaches enemy pile");
        b=Battle();var discardEffect=Card("DiscardEffect",EffectType.DiscardAttackMinusOneDamage);Enemy(b).hand.Add(Card("EnemyDiscard"));Drain((IEnumerator)Invoke(b,"CoApplyCardEffect",discardEffect,true),b);
        Check(b.History.DiscardCount(BattleSide.Enemy)==1&&b.History.Discards[0].Reason==DiscardReason.Discard,"CPU effect-selected discard reaches enemy pile");
        b=Battle();b.handManager.AddCard(Card("PlayerDiscard"));Drain((IEnumerator)Invoke(b,"CoApplyCardEffect",discardEffect,false),b);
        Check(b.History.DiscardCount(BattleSide.Player)==1&&b.History.Discards[0].Reason==DiscardReason.Discard,"player effect-selected discard reaches player pile");
        b=Battle();Invoke(b,"DrawCpuCard");
        Check(b.History.Logs.Count==1&&!b.History.Logs[0].Message.Contains("SECRET"),"CPU draw logs a count without disclosing hidden identity");
        while(Enemy(b).hand.Count<8)Enemy(b).hand.Add(card);
        Invoke(b,"DrawCpuCard");Check(b.History.Logs.Count==1,"skipped full-hand draw creates no fictitious log");
        Drain(b.cardDealAnimator.Draw(1),b);Check(b.History.Logs.Count==2,"actual player draw is logged once");
        var blocked=Card("TooExpensive");blocked.cost=99;var view=b.handManager.AddCard(blocked);int before=b.History.Logs.Count;
        Check(!b.TryPlayCard(view,DropZoneType.Enemy)&&b.History.Logs.Count==before&&b.History.Discards.Count==0,"rejected action does not create a discard or action log");
        Invoke(b,"ApplyDamage",true,3);Invoke(b,"HealSide",true,1);
        Check(b.History.Logs.Any(x=>x.Message.Contains("10 → 7"))&&b.History.Logs.Any(x=>x.Message.Contains("7 → 8")),"logs include actual damage and healing HP changes");
        var costCard=Card("Cost",EffectType.AddCost);b.costManager.Init(5,5);Drain((IEnumerator)Invoke(b,"CoApplyCardEffect",costCard,false),b);
        Check(b.History.Logs.Last().Message.Contains("5 → 5"),"capped cost effect records the actual unchanged value");
        b.NotifyDeckEmpty(true);b.NotifyDeckEmpty(true);
        Check(b.History.Logs.Count(x=>x.Message.StartsWith("WIN"))==1,"duplicate result checks create only one outcome");
        Check(new BattleHistory().Discards.Count==0&&new BattleHistory().Logs.Count==0,"new match has no old history or graveyard");

        b=Battle();b.History.BeginTurn(BattleSide.Player);
        var bushitu=AssetDatabase.LoadAssetAtPath<CardData>("Assets/Data/Card/bushitu.asset");
        var kouza=AssetDatabase.LoadAssetAtPath<CardData>("Assets/Data/Card/kouza.asset");
        b.History.MoveToDiscard(BattleSide.Player,bushitu,DiscardReason.Effect);b.History.MoveToDiscard(BattleSide.Enemy,kouza,DiscardReason.Defense,"DEF 3");
        for(int i=0;i<24;i++)b.History.Record(BattleSide.System,"スクロール確認 "+i);
        var camera=new GameObject("PreviewCamera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.02f,.07f,.15f);
        var target=new RenderTexture(1280,800,24);camera.targetTexture=target;
        var root=new GameObject("Canvas",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=5;
        var ui=new GameObject("HistoryUI").AddComponent<BattleHistoryUI>();ui.Initialize(b,canvas);Set(b,"<HistoryUI>k__BackingField",ui);
        yield return null;yield return null;
        bool wasLocked=InputLockManager.I!=null&&InputLockManager.I.IsLocked;float oldScale=Time.timeScale;
        ui.OpenLog();yield return null;
        Check(b.IsHistoryOpen&&Time.timeScale==oldScale,"history opens without pausing the simulation");
        var modalCard=b.handManager.AddCard(Card("ModalCard"));
        Check(!b.TryPlayCard(modalCard,DropZoneType.Enemy)&&!b.TrySelectDefenseByDrop(modalCard),"battle actions are rejected behind the history modal");
        Capture(camera,target,"battle-log-preview");
        var scroll=ui.GetType().GetField("scroll",Private).GetValue(ui) as ScrollRect;
        Check(scroll.content.rect.height>scroll.viewport.rect.height,"long logs have scrollable content");
        ui.OpenDiscards();yield return null;Capture(camera,target,"discard-pile-preview");
        Check(ui.IsOpen&&scroll.content.GetComponentsInChildren<Button>().Length==1,"player graveyard shows only player cards");
        ui.SelectPile(BattleSide.Enemy);yield return null;Check(scroll.content.GetComponentsInChildren<Button>().Length==1,"enemy graveyard is browsable separately");
        scroll.content.GetComponentsInChildren<Button>()[0].onClick.Invoke();yield return null;
        Check(ui.IsInspecting,"discarded card opens a readable detail view");Capture(camera,target,"discard-inspection-preview");
        ui.CloseDetails();Check(ui.IsOpen&&!ui.IsInspecting,"closing inspection returns to the graveyard");
        b.History.MoveToDiscard(BattleSide.Enemy,kouza,DiscardReason.Effect);yield return null;
        Check(scroll.content.GetComponentsInChildren<Button>().Length==2,"open graveyard updates after a new public card use");
        Time.timeScale=0f;ui.Close();ui.OpenLog();Check(ui.IsOpen,"history stays accessible after the result stops time");Time.timeScale=oldScale;ui.Close();
        Check(!b.IsHistoryOpen&&(InputLockManager.I!=null&&InputLockManager.I.IsLocked)==wasLocked,"closing history preserves turn input locks");
        for(int i=0;i<3;i++){ui.OpenLog();ui.Close();}
        Check(root.GetComponentsInChildren<BattleHistoryUI>().Length==0&&root.GetComponentsInChildren<Canvas>(true).Length==2,"reopening does not stack overlays");
        UnityEngine.Object.Destroy(ui.gameObject);yield return null;yield return null;
        Check(root.transform.childCount==0,"history UI removes its buttons and overlay on teardown");
    }
    static void Capture(Camera camera,RenderTexture target,string name)
    {
        Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
        var texture=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();
        Directory.CreateDirectory("../../outputs");File.WriteAllBytes("../../outputs/"+name+".png",texture.EncodeToPNG());UnityEngine.Object.Destroy(texture);RenderTexture.active=null;
    }
}
