using UnityEngine;
using DataBase;
using TMPro;
using Cysharp.Threading.Tasks;
using static UnityEngine.GraphicsBuffer;
using UnityEngine.TextCore.Text;

public class AccountInformate : MonoBehaviour
{
    private NetworkManager _networkManager => GameManager.Instance.networkManager;

    [Header("DeBug")]
    public TMP_Text debug;
    public TMP_Text adventurerData;
    public TMP_Text bagData;
    public TMP_Text accessTime;
    public void AccessTime(int turn) => WriteDebug(accessTime, $" {turn} / 1 0", Color.black);
    private void WriteDebug(TMP_Text target, string debugText, Color color)
    {
        target.text = debugText;
        target.color = color;
    }
    [Header("Account Apply")]
    public TMP_InputField user_email;
    public TMP_InputField auth_code;
    public TMP_InputField user_name;
    public TMP_InputField user_password;

    public string email;

    public async void AccountApply()
    {
        email = user_email.text;
        bool success = await _networkManager.RequestAccountApply(user_email.text);
        if (success)
        {
            WriteDebug(debug, " アカウント申請成功 [迷惑メールボックスに送る可能性があります] " , Color.black);
        }
        else WriteDebug(debug, "　アカウント申請失敗　", Color.red);
    }
    public async void AccountCertification()
    {
        Debug.Log(AccountConnectJson.Json_AccountCertification(email, auth_code.text));

        bool success = await _networkManager.RequestAccountCertification(email, auth_code.text);
        if (success) WriteDebug(debug , "　メール認証成功　", Color.black);
        else WriteDebug(debug, "　メール認証申請失敗　", Color.red);
    }
    public async void AccountCreate()
    {
        bool success = await _networkManager.RequestAccountCreate(email, user_name.text, user_password.text);
        if (success) WriteDebug(debug, "　アカウント作成成功　", Color.black);
        else WriteDebug(debug, "　アカウント作成失敗　", Color.red);
    }

    #region Account LogIn / LogOut

    [Header("Account LogIn / LogOut")]
    public TMP_InputField user_account;
    public TMP_InputField password;
    public TMP_Text logLogo;


    public async void AccountLogin()
    {
        bool success = await _networkManager.RequestAccountLogin(user_account.text, password.text);
        if (success)
        {
            WriteDebug(debug, "　アカウントログイン成功　", Color.black);
            logLogo.text = "ログイン";
            logLogo.color = Color.green;

            await GetBagData();
            GetCharacterData().Forget();
        }
        else WriteDebug(debug, "　アカウントログイン失敗　", Color.red);
    }

    public async void AccountLogOut()
    {
        bool success = await _networkManager.RequestAccountOut();
        if (success)
        {
            WriteDebug(debug, "　アカウントログアウト成功　", Color.black);
            logLogo.text = "ログアウト";
            logLogo.color = Color.red;

            WriteDebug(bagData, "", Color.black);
            WriteDebug(adventurerData, "", Color.black);


        }
        else WriteDebug(debug, "　アカウントログアウト失敗　", Color.red);
    }
    private async UniTask GetBagData()
    {
        PlayerBag bag = await _networkManager.GotData_Bag();
        string result = JsonUtility.ToJson(bag, true);

        string resultText = "";

        if (bag == null)
        {
            WriteDebug(debug, "　先にログインお願いします　", Color.red);
            return;
        }

        foreach (var item in bag.items)
        {
            resultText += $"アイテム：{item.item_code}, 数：{item.item_numbers}　\n";
        }

        WriteDebug(bagData, resultText, Color.black);
    }
    public void GetBagDatas() => GetBagData().Forget();


    private AllCharacterData allCharacterData;

    private async UniTask GetCharacterData()
    {
        allCharacterData = await _networkManager.GotData_AllCharacter();
        if(allCharacterData == null)
        {
            WriteDebug(debug, "　先にログインお願いします　", Color.red);
            return;
        }
        string resultText = $"Name : {character.ToString()} \n";
        string characterData = "";
        switch (character)
        {
            case CharacterType.Adventurer: characterData = allCharacterData.Adventurer.AllStatus(); break;
            case CharacterType.Magician: characterData = allCharacterData.Magician.AllStatus(); break;
            case CharacterType.Thief: characterData = allCharacterData.Thief.AllStatus(); break;
            case CharacterType.Warrior: characterData = allCharacterData.Warrior.AllStatus(); break;
        }

        resultText += characterData;
        WriteDebug(adventurerData, resultText, Color.black);

    }

    public void GetCharacterDatas() => GetCharacterData().Forget();

    public async void UpdateBag()
    {
        //string ss = DatabaseConnectJson.Json_UpdateData_Bag("aCode", "sCode", playerBag.items);
        //Debug.Log(ss);


        bool success = await _networkManager.RequestUpdateBagData(playerBag.items);
        if (success)
        {
            WriteDebug(debug, "　アイテム増えました。　", Color.black);
            GetBagData().Forget();
        }
        else WriteDebug(debug, "　アイテム増えること失敗しました　", Color.red);
    
    }

    #endregion

    #region Data Change
    [Header("Data Change")]
    public CharacterType character;
    public void ChangeCharacter(int index)
    {
        if (System.Enum.IsDefined(typeof(CharacterType), index)) character = (CharacterType)index;
    }

    private CharacterStatus characterStatus = new CharacterStatus(1, 0, 1, 1, 1, 1, 1, 1);
    public TMP_Text[] statusTexts;
    
    public PlayerBag playerBag;
    public void ChangeLevel(float value)
    {
        characterStatus.Lv = Mathf.Clamp((int)value, 1, 99);
        statusTexts[0].text = $"Lv {characterStatus.Lv}";
    }

    public void ChangeLevelPoint(float value)
    {
        characterStatus.LevelPoint = Mathf.Clamp((int)value, 0, 99);
        statusTexts[1].text = $"LP {characterStatus.LevelPoint}";
    }

    public void ChangeHpLevel(float value)
    {
        characterStatus.HpLv = Mathf.Clamp((int)value, 1, 99);
        statusTexts[2].text = $"Lv {characterStatus.HpLv}";
    }

    public void ChangeDefLevel(float value)
    {
        characterStatus.DefLv = Mathf.Clamp((int)value, 1, 99);
        statusTexts[3].text = $"Lv {characterStatus.DefLv}";
    }

    public void ChangeAtkLevel(float value)
    {
        characterStatus.AtkLv = Mathf.Clamp((int)value, 1, 99);
        statusTexts[4].text = $"Lv {characterStatus.AtkLv}";
    }

    public void ChangePassiveLevel(float value)
    {
        characterStatus.PassiveLv = Mathf.Clamp((int)value, 1, 99);
        statusTexts[5].text = $"Lv {characterStatus.PassiveLv}";
    }

    public void ChangeActiveLevel(float value)
    {
        characterStatus.ActiveLv = Mathf.Clamp((int)value, 1, 99);
        statusTexts[6].text = $"Lv {characterStatus.ActiveLv}";
    }

    public void ChangeUltLevel(float value)
    {
        characterStatus.UltLv = Mathf.Clamp((int)value, 1, 99);
        statusTexts[7].text = $"Lv {characterStatus.UltLv}";
    }

    public async void UpdateCharacter()
    {
        string ss = DatabaseConnectJson.Json_UpdateData_Character("aCode", "sCode", character, characterStatus);
        Debug.Log(ss);

        bool success = await _networkManager.RequestUpdateCharacterData(character, characterStatus);
        if (success)
        {
            WriteDebug(debug, "　キャラクターのステータス更新しました。　", Color.black);
            GetCharacterData().Forget(); ;
        }
        else WriteDebug(debug, "　　キャラクターのステータス更新が失敗しました　", Color.red);
    }
    #endregion
}