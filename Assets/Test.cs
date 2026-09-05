using UnityEngine;
using DataBase;
using TMPro;
using Cysharp.Threading.Tasks;

public class Test : MonoBehaviour
{
    private NetworkManager _networkManager => GameManager.Instance.networkManager;

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
        if (success) Debug.Log("Account Apply Success");
        else Debug.LogError("Account Apply Failed");
    }
    public async void AccountCertification()
    {
        Debug.Log(AccountConnectJson.Json_AccountCertification(email, auth_code.text));

        bool success = await _networkManager.RequestAccountCertification(email, auth_code.text);
        if (success) Debug.Log("Account Apply Success");
        else Debug.LogError("Account Apply Failed");
    }
    public async void AccountCreate()
    {
        bool success = await _networkManager.RequestAccountCreate(email, user_name.text, user_password.text);
        if (success) Debug.Log("Account Apply Success");
        else Debug.LogError("Account Apply Failed");
    }


    [Header("Account LogIn / LogOut")]
    public TMP_InputField user_account;
    public TMP_InputField password;

    public async void AccountLogin()
    {
        bool success = await _networkManager.RequestAccountLogin(user_account.text, password.text);
        if (success) Debug.Log("Account Apply Success");
        else Debug.LogError("Account Apply Failed");
    }

    public async void AccountLogOut()
    {
        bool success = await _networkManager.RequestAccountOut();
        if (success) Debug.Log("Account Apply Success");
        else Debug.LogError("Account Apply Failed");
    }


    public async void GetBagData()
    {
        PlayerBag bag = await _networkManager.GotData_Bag();
        string result = JsonUtility.ToJson(bag, true);
        Debug.Log(result);


        //foreach (var item in bag.items)
        //{
        //    Debug.Log(item.item_code);
        //    Debug.Log(item.item_numbers);
        //}


    }

    public AllCharacterData allCharacterData;

    public async void GetAllCharacterData()
    {
        allCharacterData = await _networkManager.GotData_AllCharacter();

        Debug.Log(allCharacterData.Adventurer.AllStatus());

    }

    public PlayerBag playerBag;

    public async void UpdateBag()
    {
        //string ss = DatabaseConnectJson.Json_UpdateData_Bag("aCode", "sCode", playerBag.items);
        //Debug.Log(ss);


        bool success = await _networkManager.RequestUpdateBagData(playerBag.items);
        if (success) Debug.Log("Account Apply Success");
        else Debug.LogError("Account Apply Failed");
    }

    public CharacterType character;
    public CharacterStatus characterStatus;

    public async void UpdateCharacter()
    {
        //string ss = DatabaseConnectJson.Json_UpdateData_Character("aCode", "sCode", character, characterStatus);
        //Debug.Log(ss);


        bool success = await _networkManager.RequestUpdateCharacterData(character, characterStatus);
        if (success) Debug.Log("Account Apply Success");
        else Debug.LogError("Account Apply Failed");
    }

}
