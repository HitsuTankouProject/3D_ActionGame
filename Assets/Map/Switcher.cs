using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.ProBuilder.AutoUnwrapSettings;
public interface ISwitcher
{
    public Transform switchPart {  get; }
    public float switchTime { get;}

    public Vector3 openLocalPosition { get; }
    public Vector3 openLocalRotationAngle { get; }
    public Vector3 openLocalScale { get; }


    public Vector3 closeLocalPosition { get; }
    public Vector3 closeLocalRotationAngle { get; }
    public Vector3 closeLocalScale { get; }

}

public static class Switcher
{
    public enum SwitchStage { Close, Open }

    #region Switch In Position
    public static async UniTask SwitchInPosition(this ISwitcher target, SwitchStage switchStage)
    {
        Vector3 startPosition = switchStage == SwitchStage.Close ? target.openLocalPosition : target.closeLocalPosition;
        Vector3 endPosition = switchStage == SwitchStage.Close ? target.closeLocalPosition : target.openLocalPosition;
        float timer = 0;
        float switchTime = target.switchTime;
        if (switchTime <= 0f)
        {
            target.switchPart.localPosition = endPosition;
            return;
        }

        target.switchPart.localPosition = startPosition;
        while (switchTime > timer)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / switchTime);
            target.switchPart.localPosition = Vector3.Lerp(startPosition, endPosition, progress);

            await UniTask.Yield(PlayerLoopTiming.Update);

        }
        target.switchPart.localPosition = endPosition;
    }
    public static void SwitchInPositionOpen(this ISwitcher target) => SwitchInPosition(target, SwitchStage.Open).Forget();
    public static void SwitchInPositionClose(this ISwitcher target) => SwitchInPosition(target, SwitchStage.Close).Forget();
    #endregion

    #region  Switch In Rotation
    public static async UniTask SwitchInRotation(this ISwitcher target, SwitchStage switchStage)
    {
        Quaternion startRotation = Quaternion.Euler(
            switchStage == SwitchStage.Close ? target.openLocalRotationAngle : target.closeLocalRotationAngle);
        Quaternion endRotation = Quaternion.Euler(
            switchStage == SwitchStage.Close ? target.closeLocalRotationAngle : target.openLocalRotationAngle);
        float timer = 0;
        float switchTime = target.switchTime;

        if (switchTime <= 0f)
        {
            target.switchPart.localRotation = endRotation;
            return;
        }

        target.switchPart.localRotation = startRotation;
        while (switchTime > timer)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / switchTime);
            target.switchPart.localRotation = Quaternion.Slerp(startRotation, endRotation, progress);

            await UniTask.Yield(PlayerLoopTiming.Update);

        }
        target.switchPart.localRotation = endRotation;

    }
    public static void SwitchInRotationOpen(this ISwitcher target) => SwitchInRotation(target, SwitchStage.Open).Forget();
    public static void SwitchInRotationClose(this ISwitcher target) => SwitchInRotation(target, SwitchStage.Close).Forget();

    #endregion

    #region  Switch In Scale
    public static async UniTask SwitchInScale( this ISwitcher target, SwitchStage switchStage)
    {

        Vector3 startScale = switchStage == SwitchStage.Close ? target.openLocalScale : target.closeLocalScale;
        Vector3 endScale = switchStage == SwitchStage.Close ? target.closeLocalScale : target.openLocalScale;

        float timer = 0f;
        float switchTime = target.switchTime;

        if (switchTime <= 0f)
        {
            target.switchPart.localScale = endScale;
            return;
        }

        target.switchPart.localScale = startScale;
        while (timer < switchTime)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / switchTime);
            target.switchPart.localScale =
                Vector3.Lerp(startScale, endScale, progress);

            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        target.switchPart.localScale = endScale;
    }
    public static void SwitchInScaleOpen(this ISwitcher target) => SwitchInScale(target, SwitchStage.Open).Forget();
    public static void SwitchInScaleClose(this ISwitcher target) => SwitchInScale(target, SwitchStage.Close).Forget();

    #endregion

}
