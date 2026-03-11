using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class DicManager : MonoBehaviour
{
    public string label="cards";
    public GameObject card;
    public Transform content;
    public GameObject cardViewFB;
    public List<Transform> cardList;
    public static GameObject cardView;
    public static bool allLoaded=false;
    public GameObject sortOptionUI;
    AsyncOperationHandle<IList<CardData>> handle;
    private void Awake()
    {
        cardView=cardViewFB;
        if(!cardView)return;
        cardView.SetActive(false);
        if(!sortOptionUI)return;
        sortOptionUI.SetActive(false);
    }
    void Start()
    {
        handle=Addressables.LoadAssetsAsync<CardData>(label, addressable =>
        {
            Debug.Log("loaded : "+addressable.displayName);
            addCard(addressable);
        });
        handle.Completed += (AsyncOperationHandle<IList<CardData>> obj) =>
        {
            //when complete loading all assets
            if (obj.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log("All Data Loaded");
                allLoaded=true;
                Sorting("alphabet"); //default value
            }
        };
    }

    public void addCard(CardData data)
    {
        if(data){
            GameObject obj=Instantiate(card,content);
            if(obj.GetComponent<CardView>() is CardView view)
            {
                view.Bind(data);
                obj.name=data.displayName;
            }
        }
    }

    void OnDestroy()
    {
        Addressables.Release(handle);
        Debug.Log("All Data Released");
    }
    public void ToggleSortingUI()
    {
        sortOptionUI.SetActive(!sortOptionUI.activeSelf);
    }

    public static void ShowCards(Image src)
    {
        if(!cardView.activeSelf)cardView.SetActive(true);
        Image img=cardView.GetComponent<Image>();
        img.sprite=src.sprite;
    }

    public List<Transform> GetContentChildren()
    {
        if(!(cardList.Count<=0))return cardList;
        List<Transform> list = new List<Transform>();
        for(int i = 0; i < content.childCount; i++)
        {
            list.Add(content.GetChild(i));
        }
        return list;
    }
        public void SetContentChildren(List<Transform> sortedList)
    {
        foreach(Transform elem in sortedList)
        {
            elem.transform.SetAsLastSibling();
        }
    }
    public List<Transform> SortBy(List<Transform> list,string orderRule)
    {
        if(list.Count<=0)return list;
        switch (orderRule){
        case "alphabet":
            list.Sort((e1,e2)=>string.Compare(e1.name,e2.name));
            return list;
        case "atk":
            list.Sort((e1,e2)=>int.Parse(getAtk(e1)).CompareTo(int.Parse(getAtk(e2))));
            return list;
        case "def":
            list.Sort((e1,e2)=>int.Parse(getDef(e1)).CompareTo(int.Parse(getDef(e2))));
            return list;
        default:
            return list;
        }
    }
    private string getAtk(Transform trans)
    {
        return trans.GetComponent<CardView>().atkText.text;
    }
    private string getDef(Transform trans)
    {
        return trans.GetComponent<CardView>().defText.text;
    }
    public void Sorting(string orderRule)
    {
        cardList=GetContentChildren();
        SetContentChildren(SortBy(cardList,orderRule));
    }
}
