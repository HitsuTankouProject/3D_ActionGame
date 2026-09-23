using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class HealthBar : Bar
{
    [Header("Health Bar")]
    public Component colorMark;
    private Image image_Mark;
    private SpriteRenderer sr_Mark;

    public bool test = false;
    public float health = 1.0f;

    protected override void Awake()
    {
        base.Awake();
        barColorPairs = new SortedList<float, Color>(
            Comparer<float>.Create((leftValue, rightValue) => rightValue.CompareTo(leftValue)))
        { { 0.6f, Color.green }, { 0.3f, Color.yellow }, { 0.0f, Color.red } };

        if (colorMark == null) return;

        if (usingDisPlay == UsingDisPlay.Image && colorMark is Image imageMark) image_Mark = imageMark;
        else if (usingDisPlay == UsingDisPlay.SpriteRenderer && colorMark is SpriteRenderer srMark) sr_Mark = srMark;

        else Debug.LogError($"{gameObject.name} + HealthBar.colorMark setting non Image or SpriteRenderer");
    }
    protected override void ChangeBarColor()
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



        if (usingDisPlay == UsingDisPlay.Image)
        {
            image_Front.color = changeColor;
            if (image_Mark != null) image_Mark.color = changeColor;
        }
        else if (usingDisPlay == UsingDisPlay.SpriteRenderer)
        {
            sr_Front.color = changeColor;
            if(sr_Mark != null) sr_Mark.color = changeColor;
        }

    }
    public async UniTask ChangeBackBar(float value)
    {

        if(currentValue > value) await ChangeValueTo(value);

        float elapsedTime = 0;
        float targetValue = Mathf.Clamp(value, minValue, maxValue);

        float startValue = -1;
        
        if (usingDisPlay == UsingDisPlay.Image) startValue = image_Back.fillAmount;
        else if (usingDisPlay == UsingDisPlay.SpriteRenderer) startValue = sr_Back.size.x;

        if(startValue == -1)
        {
            Debug.LogError($"{barFillBack.name} no image or Sprite renderer");
            return;
        }

        if(startValue == value) return;


        float timeLimit = changeDuration / 2;

        while (elapsedTime < timeLimit)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / timeLimit);

            if (usingDisPlay == UsingDisPlay.Image) image_Back.fillAmount = Mathf.Lerp(startValue, targetValue, progress);
            else if (usingDisPlay == UsingDisPlay.SpriteRenderer) sr_Back.size = new Vector2(Mathf.Lerp(startValue, targetValue, progress), sr_Back.size.y);

            await UniTask.Yield();
        }


        if (usingDisPlay == UsingDisPlay.Image) image_Back.fillAmount = targetValue;
        else if (usingDisPlay == UsingDisPlay.SpriteRenderer) sr_Back.size = new Vector2(targetValue, sr_Front.size.y);

    }
    //private void Update()
    //{
    //    if (test)
    //    {
    //        ChangeValueTo(health).Forget();
    //        test = false;
    //    }
    //}

}
