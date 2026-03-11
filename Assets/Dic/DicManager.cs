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
    public static GameObject cardView;
    public static bool allLoaded=false;
    AsyncOperationHandle<IList<CardData>> handle;
    private void Awake()
    {
        cardView=cardViewFB;
        if(!cardView)return;
        cardView.SetActive(false);
    }
    void Start()
    {
        handle=Addressables.LoadAssetsAsync<CardData>(label, addressable =>
        {
            Debug.Log("loaded"+addressable.displayName);
            addCard(addressable);
        });
        handle.Completed += (AsyncOperationHandle<IList<CardData>> obj) =>
        {
            //when complete loading all assets
            if (obj.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log("All Data Loaded");
                allLoaded=true;
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
            }
        }
    }

    void OnDestroy()
    {
        Addressables.Release(handle);
        Debug.Log("All Data Released");
    }

    public static void ShowCards(Image src)
    {
        if(!cardView.activeSelf)cardView.SetActive(true);
        Image img=cardView.GetComponent<Image>();
        img.sprite=src.sprite;
    }


}
