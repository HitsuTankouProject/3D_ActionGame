using Cysharp.Threading.Tasks;
using System.Threading;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.UI;


public class ColorBar : Bar
{
    public Image colorMark;
    private float cycleDuration = 10.0f;
    private const float barMaximumValue = 100.0f;

    private async UniTask ChangeColor(CancellationToken token)
    {
        float hue = 1.0f / 6.0f;

        while (!token.IsCancellationRequested)
        {
            hue = Mathf.Repeat(hue - Time.deltaTime / cycleDuration, 1.0f);

            Color changeColor = Color.HSVToRGB(hue, 1.0f, 1.0f);
            if (usingDisPlay == UsingDisPlay.Image) image_Front.color = changeColor;
            else if (usingDisPlay == UsingDisPlay.SpriteRenderer) sr_Front.color = changeColor;
            colorMark.color = changeColor;

            await UniTask.Yield( PlayerLoopTiming.Update, cancellationToken: token);
        }
    }


    public override async UniTask ChangeValueTo(float value)
    {

        float nowValue = barMaximumValue * currentValue;
        float targetValue = nowValue + value;
        bool isIncrease = targetValue > nowValue;

        if (isIncrease)
        {
            // 最大値を超えるたびに、次の色レベルへ繰り越す。
            while (targetValue > barMaximumValue)
            {
                // 現在のバーを最大値まで増加させる。
                await base.ChangeValueTo(1.0f);

                targetValue -= barMaximumValue;

                // 次の色レベルのバーを0から開始する。
                SetFrontBarValueImmediately(0.0f);
            }
        }
        else
        {
            // 0を下回るたびに、次の色喪失レベルへ繰り越す。
            while (targetValue < 0.0f)
            {
                // 現在のバーを0まで減少させる。
                await base.ChangeValueTo(0.0f);

                targetValue += barMaximumValue;

                SetFrontBarValueImmediately(1.0f);
            }
        }

        float normalizedTargetValue = Mathf.Clamp01(targetValue / barMaximumValue);
        await base.ChangeValueTo(normalizedTargetValue);


    }

    private void Start()
    {
        ChangeColor(this.GetCancellationTokenOnDestroy()).Forget();
    }


    public bool increase = false;
    public bool decrease = false;

    private void Update()
    {

        if (increase && decrease)
        {
            increase = false;
            decrease = false;
        }
        else if (increase && !decrease)
        {
            ChangeValueTo(30.0f).Forget();
            increase = false;
        }
        else if (!increase && decrease)
        {
            ChangeValueTo(-30.0f).Forget();
            decrease = false;
        }

    }

}
