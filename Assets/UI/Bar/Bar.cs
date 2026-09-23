using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public abstract class Bar : MonoBehaviour
{
    [Header("Image or SpriteRenderer")]
    public Component barFillFront;
    public Component barFillBack;
    protected enum UsingDisPlay { None, Image, SpriteRenderer };
    protected UsingDisPlay usingDisPlay = UsingDisPlay.None;

    protected Image image_Front;
    protected Image image_Back;

    protected SpriteRenderer sr_Front;
    protected SpriteRenderer sr_Back;

    protected virtual void Awake()
    {
        if (barFillFront is Image imageFront && barFillBack is Image imageBack)
        {
            image_Front = imageFront;
            image_Back = imageBack;
            usingDisPlay = UsingDisPlay.Image;
        }
        else if (barFillFront is SpriteRenderer rendererFront && barFillBack is SpriteRenderer rendererBack)
        {
            sr_Front = rendererFront;
            sr_Back = rendererBack;
            usingDisPlay = UsingDisPlay.SpriteRenderer;
        }


        if (usingDisPlay == UsingDisPlay.None)
        {
            Debug.LogError($"{gameObject.name} + Bar Using Non Image or SpriteRenderer");
            return;
        }
    }

    protected const float maxValue = 1.0f;
    protected const float minValue = 0.0f;
    protected float currentValue
    {
        get
        {
            if (barFillFront is Image image) return image.fillAmount;
            else if (barFillFront is SpriteRenderer spriteRenderer) return spriteRenderer.size.x;
            return 0.0f;
        }
    }

    protected SortedList<float, Color> barColorPairs = new SortedList<float, Color>();

    protected virtual void ChangeBarColor()
    {
        if (barColorPairs.Count == 0) return;

        Color changeColor = Color.white;
        for (int i = 0; i < barColorPairs.Count; i++)
        {
            if (currentValue >= barColorPairs.Keys[i])
            {
                changeColor = barColorPairs.Values[i];
                break;
            }
        }

        if(usingDisPlay == UsingDisPlay.Image) image_Front.color = changeColor;
        else if(usingDisPlay == UsingDisPlay.SpriteRenderer) sr_Front.color = changeColor;

    }
    protected const float changeDuration = 0.5f;

    public virtual async UniTask ChangeValueTo(float value)
    {
        bool isIncrease = value > currentValue;

        float startValue = currentValue;
        float targetValue = Mathf.Clamp(value, minValue, maxValue);

        float elapsedTime = 0f;
        while (elapsedTime < changeDuration)
        {

            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / changeDuration);

            if (usingDisPlay == UsingDisPlay.Image) image_Front.fillAmount = Mathf.Lerp(startValue, targetValue, progress);
            else if (usingDisPlay == UsingDisPlay.SpriteRenderer) sr_Front.size = new Vector2(Mathf.Lerp(startValue, targetValue, progress), sr_Front.size.y);
            ChangeBarColor();
            await UniTask.Yield();
        }

        if (usingDisPlay == UsingDisPlay.Image) image_Front.fillAmount = targetValue;
        else if (usingDisPlay == UsingDisPlay.SpriteRenderer) sr_Front.size = new Vector2(targetValue, sr_Front.size.y);


    }

    public void SetFrontBarValueImmediately(float value)
    {
        value = Mathf.Clamp01(value);

        switch (usingDisPlay)
        {
            case UsingDisPlay.Image: image_Front.fillAmount = value; break;

            case UsingDisPlay.SpriteRenderer:

                Vector2 newSize = sr_Front.size;
                newSize.x = value;
                sr_Front.size = newSize;
                break;

        }
    }
}
