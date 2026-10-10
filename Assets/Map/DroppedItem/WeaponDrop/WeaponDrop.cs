using UnityEngine;

public class WeaponDrop : DroppedItem
{
    [SerializeField] private MeshRenderer renderer;
    private const string weaponDropIconTag = "_weaponIcon";

    private const string emissionLevelTag = "_emissionLevel";
    private const string emissionColorTag = "_emissionColor";
    private readonly string[] allEmissionColorCode = new string[5]
    {
        "#CFCFCF", "#37FF00", "#0055FF", "#BA00FF", "#FFD600"
    };

    public WeaponStatus weaponStatus;/* {  get; private set; }*/

    private bool CheckShaderProperty()
    {
        return renderer != null
            && renderer.material.HasProperty(weaponDropIconTag)
            && renderer.material.HasProperty(emissionLevelTag)
            && renderer.material.HasProperty(emissionColorTag);
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
        renderer.material.SetColor(emissionColorTag, emissionColor);
        renderer.material.SetTexture(weaponDropIconTag, weaponStatus.weaponIcon.texture);
    }

    protected override void Start()
    {
        base.Start();
        if (!CheckShaderProperty())
        {
            Debug.LogError("Wrong Shader");
            return;
        }
        Color emissionColor = GetEmissionColor();
        renderer.material.SetColor(emissionColorTag, emissionColor);
        renderer.material.SetTexture(weaponDropIconTag, weaponStatus.weaponIcon.texture);
    }

    protected override void ItemReaction(Collider other)
    {

    }
}
