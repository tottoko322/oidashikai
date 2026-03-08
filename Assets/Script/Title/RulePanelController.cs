using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RulePanelController : MonoBehaviour
{
    public CarouselController carousel;
    public Image descImage;
    public Sprite image1;
    public Sprite image2;
    public Sprite image3;
    public Sprite image4;
    private Sprite[] imageSprites;
    private int descIndex=4;

    private void Start()
    {
        imageSprites=new Sprite[descIndex];
        if(image1&&image2&&image3&&image4){
            imageSprites[0]=image1;
            imageSprites[1]=image2;
            imageSprites[2]=image3;
            imageSprites[3]=image4;
        }
        carousel.Init(descIndex);
        Apply();
    }

    public void OnPrev() { carousel.Prev(); Apply(); }
    public void OnNext() { carousel.Next(); Apply(); }

    private void Apply()
    {
        descImage.sprite=imageSprites[carousel.Index];
    }
}
