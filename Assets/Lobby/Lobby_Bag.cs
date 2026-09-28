using UnityEngine;
using UnityEngine.UI;

public class Lobby_Bag : MonoBehaviour
{
    //[SerializeField] private Lobby lobby;
    //[SerializeField] private WeaponType nowShowingWeaponType;
    //[SerializeField] private Button[] weaponButtons;


    //[Header("Camera")]
    //[SerializeField] private Animator cameraAnimator;
    //private const string turnRightTigger = "TurnRight";
    //private const string turnLeftTigger = "TurnLeft";
    //private const string turnReturnTigger = "Return";
    //private enum CameraShow { Left, Right };
    //private void CameraMove(CameraShow cameraShow)
    //{
    //    cameraAnimator.SetTrigger(cameraShow == CameraShow.Right ? turnRightTigger : turnLeftTigger);
    //}
    //private void CameraReturn()
    //{
    //    cameraAnimator.SetTrigger(turnReturnTigger);
    //}


    //[Header("Sword Icon")]
    //[SerializeField] private MeshRenderer sword;
    //private GameObject swordObject => sword.gameObject;
    //[Header("Shield Icon")]
    //[SerializeField] private MeshRenderer shield;
    //private GameObject shieldObject => shield.gameObject;

    //[SerializeField] private MeshRenderer nowShowingWeaponMeshRender;

    //private void CloseAllTheWeapon()
    //{
    //    swordObject.SetActive(false);
    //    shieldObject.SetActive(false);
    //}
    //private void OpenTargetWeapon()
    //{
    //    CloseAllTheWeapon();
    //    switch (nowShowingWeaponType)
    //    {
    //        case WeaponType.Sword:
    //            swordObject.SetActive(true);
    //            nowShowingWeaponMeshRender = sword;
    //            return;
    //        case WeaponType.Shield:
    //            shieldObject.SetActive(true);
    //            nowShowingWeaponMeshRender = shield;
    //            return;
    //        default:
    //            nowShowingWeaponMeshRender = null;
    //            return;
    //    }

    //}
    //private void ChangeButtonIcons()
    //{
    //    switch (nowShowingWeaponType)
    //    {
    //        case WeaponType.Sword: nowShowingWeaponItemRareIcon = swordRareIcon; break;
    //        case WeaponType.Shield: nowShowingWeaponItemRareIcon = shieldRareIcon; break;
    //        default: nowShowingWeaponItemRareIcon = default; return;
    //    }

    //    weaponButtons[0].image.sprite = nowShowingWeaponItemRareIcon.sp_rare_01;
    //    weaponButtons[1].image.sprite = nowShowingWeaponItemRareIcon.sp_rare_02;
    //    weaponButtons[2].image.sprite = nowShowingWeaponItemRareIcon.sp_rare_03;
    //    weaponButtons[3].image.sprite = nowShowingWeaponItemRareIcon.sp_rare_04;
    //    weaponButtons[4].image.sprite = nowShowingWeaponItemRareIcon.sp_rare_05;

    //}

    //private void ChangeWeapon(WeaponType weaponType)
    //{
    //    nowShowingWeaponType = weaponType;
    //    ChangeButtonIcons();
    //    OpenTargetWeapon();

    //}
    //public void OpenSwordBag()
    //{
    //    ChangeWeapon(WeaponType.Sword);
    //    CameraMove(CameraShow.Left);
    //}
    //public void OpenShieldBag()
    //{
    //    ChangeWeapon(WeaponType.Shield);
    //    CameraMove(CameraShow.Right);

    //}

    //private void Init()
    //{
    //    for (int i = 0; i < weaponButtons.Length; i++)
    //    {
    //        int rareIndex = i;
    //    }

    //}


    //private void Start()
    //{
    //    Init();
    //}

}
