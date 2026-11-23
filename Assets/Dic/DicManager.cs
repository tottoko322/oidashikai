using System;
using System.Globalization;
using System.Collections.Generic;
using TMPro;
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
    public TMP_Text descTextFB;
    public GameObject descriptionFB;
    public static GameObject description;
    public static TMP_Text descText; 
    public List<Transform> cardList;
    public static GameObject cardView;
    public static bool allLoaded=false;
    public GameObject sortOptionUI;
    AsyncOperationHandle<IList<CardData>> handle;
    private void Awake()
    {
        allLoaded = false;
        cardList = new List<Transform>();
        description = descriptionFB;
        descText = descTextFB;
        if (descText != null)
        {
            GameFonts.ApplyJapanese(descText);
            descText.enableAutoSizing = true;
            descText.fontSizeMin = 14f;
            descText.fontSizeMax = 26f;
            descText.alignment = TextAlignmentOptions.TopLeft;
            descText.textWrappingMode = TextWrappingModes.Normal;
            descText.raycastTarget = false;
            var rect = descText.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(20f, 20f);
            rect.offsetMax = new Vector2(-20f, -20f);
        }
        cardView=cardViewFB;
        if (cardView != null) cardView.SetActive(false);
        if (sortOptionUI != null) sortOptionUI.SetActive(false);
        if (description != null) description.SetActive(false);
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
        if (handle.IsValid()) Addressables.Release(handle);
        allLoaded = false;
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
            list.Sort((e1,e2)=>CultureInfo.GetCultureInfo("ja-JP").CompareInfo.Compare(
                SortReading(e1), SortReading(e2), CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth));
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

    private static string SortReading(Transform trans)
    {
        var view = trans.GetComponent<CardView>();
        return view != null && view.Data != null && !string.IsNullOrEmpty(view.Data.sortReading)
            ? view.Data.sortReading : trans.name;
    }
    private string getDef(Transform trans)
    {
        return trans.GetComponent<CardView>().defText.text;
    }
    public void ToggleDesc()
    {
        description.SetActive(!description.activeSelf);
    }
    public void Sorting(string orderRule)
    {
        cardList=GetContentChildren();
        SetContentChildren(SortBy(cardList,orderRule));
    }
    public static void ChangeText(TMP_Text text)
    {
        descText.text=text.text;
    }

    public static void ShowDescription(CardData data)
    {
        if (descText == null || data == null) return;
        GameFonts.ApplyJapanese(descText);
        string introduction = data.encyclopediaDescription ?? "";
        int firstLine = introduction.IndexOf('\n');
        if (firstLine >= 0) introduction = introduction.Substring(firstLine).Trim();
        descText.text = string.IsNullOrEmpty(data.effectDescription) ? data.encyclopediaDescription
            : $"{data.displayName}\n\nコウカ\n{data.effectDescription}\n\nカード紹介\n{introduction}";
    }
}


