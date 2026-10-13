using UnityEngine;

public class WeaponDrop : DroppedItem
{
    private GameManager _gameManager => GameManager.Instance;
    public override ItemType type => ItemType.Weapon;


    private const string weaponDropIconTag = "_weaponIcon";

    private const string emissionColorTag = "_emissionColor";
    private const string emissionLevelTag = "_emissionLevel";

    private readonly string[] allEmissionColorCode = new string[5]
    {
        "#CFCFCF", "#37FF00", "#0055FF", "#BA00FF", "#FFD600"
    };

    public WeaponStatus weaponStatus;/* {  get; private set; }*/

    private bool CheckShaderProperty()
    {
        if (modelRenderer == null) return false;

        Material material = modelRenderer.sharedMaterial;

        return material != null
            && material.HasProperty(weaponDropIconTag)
            && material.HasProperty(emissionLevelTag)
            && material.HasProperty(emissionColorTag);
    }

    private Color GetEmissionColor()
    {
        if(weaponStatus == null) return Color.white;
        uint weaponRare = weaponStatus.rare;
        if(weaponRare> allEmissionColorCode.Length)
        {
            Debug.LogError("weaponRare> allEmissionColorCode.Length");
            return Color.white;
        }
        string colorCode = allEmissionColorCode[weaponRare - 1];
        if (!ColorUtility.TryParseHtmlString(colorCode, out Color emissionColor))
        {
            Debug.LogError("Color Code Error");
            return Color.white;
        }
        return emissionColor;
    }

    public void SetUpWeapon(WeaponStatus status)
    {
        weaponStatus = status;
        if (!CheckShaderProperty())
        {
            Debug.LogError("Wrong Shader");
            return;
        }
        Color emissionColor = GetEmissionColor();
        modelRenderer.material.SetColor(emissionColorTag, emissionColor);
        modelRenderer.material.SetTexture(weaponDropIconTag, weaponStatus.weaponIcon.texture);

        SetUp();
    }

    protected override void ItemReaction(Collider other)
    {
        if (!setUpDone) return;
        if (other.TryGetComponent<Character>(out Character character))
        {
            if (character != _gameManager.gamePlayer.controlling_Character) return;
            _gameManager.gamePlayer.AddToInGameBag(weaponStatus.item_code);
            ReturnToPool();

        }


    }
}
