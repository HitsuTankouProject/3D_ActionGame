using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PickWeaponButton : MonoBehaviour
{
    private Lobby _lobby => Lobby.Instance;
    [SerializeField] private WeaponStatus weaponStatus;
    private Button button = null;

    public void SetupWeaponPick(WeaponStatus weapon)
    {
        if(button == null) button = GetComponent<Button>();
        if(weapon == null)
        {
            button.image.sprite = null;
            return;
        }
        weaponStatus = weapon;
        button.image.sprite = weaponStatus.weaponIcon;
    }

    public void Button_PickWeapon()
    {
        if (_lobby == null || weaponStatus == null) return;
        _lobby.ChangeWeapon(weaponStatus);
    }

}
