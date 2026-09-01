using UnityEngine;

namespace DataBase
{
    public enum DatabaseConnectAction
    {
        AccountApply,
        AccountCertification,
        AccountCreate,

        AccountLogin,
        AccountLogout,

        AccountResetPassword,

        DatabaseUpdate,
        DatabaseGetData

    }

    public static class AccessConnectURL
    {
        private static string account_apply_url = "http://34.29.233.30/unity-api/account-apply.php";
        private static string account_certification_url = "http://34.29.233.30/unity-api/account-certification.php";
        private static string account_manage_url = "http://34.29.233.30/unity-api/account-manage.php";
        private static string account_login_url = "http://34.29.233.30/unity-api/account-login.php";
        private static string account_logout_url = "http://34.29.233.30/unity-api/account-logout.php";
        private static string database_update_url = "http://34.29.233.30/unity-api/database-update.php";
        private static string database_getdata_url = "http://34.29.233.30/unity-api/database-getdata.php";

        public static string ConnectURL(DatabaseConnectAction purpose)
        {
            return purpose switch
            {
                DatabaseConnectAction.AccountApply => account_apply_url,
                DatabaseConnectAction.AccountCertification => account_certification_url,
                DatabaseConnectAction.AccountCreate => account_manage_url,
                DatabaseConnectAction.AccountResetPassword => account_manage_url,
                DatabaseConnectAction.AccountLogin => account_login_url,
                DatabaseConnectAction.AccountLogout => account_logout_url,
                DatabaseConnectAction.DatabaseUpdate => database_update_url,
                DatabaseConnectAction.DatabaseGetData => database_getdata_url,


                _ => throw new System.ArgumentOutOfRangeException(nameof(purpose))
            };

        }
    }

    public static class AccountConnectJson
    {
        private class Access_AccountApply
        {
            public string user_email = "";
            public Access_AccountApply(string email) => user_email = email;
        }
        public static string Json_AccountApply(string email)
        {
            Access_AccountApply access = new Access_AccountApply(email);
            return JsonUtility.ToJson(access);
        }

        private class Access_AccountCertification
        {
            public string user_email = "";
            public string auth_code = "";
            public Access_AccountCertification(string email, string code)
            {

                user_email = email;
                auth_code = code;
            }
        }
        public static string Json_AccountCertification(string email, string code)
        {
            Access_AccountCertification access = new Access_AccountCertification(email, code);
            return JsonUtility.ToJson(access);
        }

        private class Access_AccountCreate
        {
            public string user_email = "";
            public string user_name = "";
            public string password = "";
            public Access_AccountCreate(string email, string name, string inputPassword)
            {

                user_email = email;
                user_name = name;
                password = inputPassword;
            }
        }
        public static string Json_AccountCreate(string email, string name, string inputPassword)
        {
            Access_AccountCreate access = new Access_AccountCreate(email, name, inputPassword);
            return JsonUtility.ToJson(access);
        }

        private class Access_AccountLogin
        {
            public string account = "";
            public string password = "";
            public Access_AccountLogin(string emailOrId, string inputPassword)
            {
                account = emailOrId;
                password = inputPassword;
            }
        }
        public static string Json_AccountLogin(string account, string inputPassword)
        {
            Access_AccountLogin access = new Access_AccountLogin(account, inputPassword);
            return JsonUtility.ToJson(access);
        }

        private class Access_AccountLogout
        {
            public string security_code = "";
            public Access_AccountLogout(string code)
            {
                security_code = code;
            }
        }
        public static string Json_AccountLogout(string code)
        {
            Access_AccountLogout access = new Access_AccountLogout(code);
            return JsonUtility.ToJson(access);
        }

    }

    public static class DatabaseConnectJson
    {
        [System.Serializable]
        public enum AllowedPurpose { Character, Bag }
        [System.Serializable]
        private class CharacterData
        {
            public string character_name;
            public CharacterStatus character_status;

            public CharacterData(CharacterType name, CharacterStatus status)
            {
                character_name = name.ToString();
                character_status = status;
            }
        }
        [System.Serializable]
        private class BagData
        {
            public PlayerItem[] change_items;
            public BagData(PlayerItem[] changeItems)
            {
                change_items = changeItems;
            }
        }
        [System.Serializable]
        private class UpdateData_Bag
        {
            public string access_code;
            public string security_code;
            public string purpose;
            public BagData update_data;

            public UpdateData_Bag(string aCode, string sCode, AllowedPurpose allowedPurpose, BagData newData)
            {

                access_code = aCode;
                security_code = sCode;
                purpose = allowedPurpose.ToString();
                update_data = newData;
            }

        }
        [System.Serializable]
        private class UpdateData_Character
        {
            public string access_code;
            public string security_code;
            public string purpose;
            public CharacterData update_data;

            public UpdateData_Character(string aCode, string sCode, AllowedPurpose allowedPurpose, CharacterData newData)
            {

                access_code = aCode;
                security_code = sCode;
                purpose = allowedPurpose.ToString();
                update_data = newData;
            }

        }

        public static string Json_UpdateData_Character(string aCode, string sCode, CharacterType name, CharacterStatus status)
        {

            UpdateData_Character updateData = new UpdateData_Character(aCode, sCode, AllowedPurpose.Character, new CharacterData(name, status));
            return JsonUtility.ToJson(updateData);

        }

        public static string Json_UpdateData_Bag(string aCode, string sCode, PlayerItem[] changeItems)
        {

            UpdateData_Bag updateData = new UpdateData_Bag(aCode, sCode, AllowedPurpose.Bag, new BagData(changeItems));

            return JsonUtility.ToJson(updateData);

        }

        public class GetData
        {
            public string access_code;
            public string security_code;
            public string purpose;
            public GetData(string aCode, string sCode, AllowedPurpose allowedPurpose)
            {

                access_code = aCode;
                security_code = sCode;
                purpose = allowedPurpose.ToString();
            }

        }

        public static string Json_GetData(string aCode, string sCode, AllowedPurpose allowedPurpose)
        {
            GetData getData = new(aCode, sCode, allowedPurpose);
            return JsonUtility.ToJson(getData);
        }




    }

    [System.Serializable]
    public class LoginData
    {
        public string user_id;
        public string security_code;
        public string access_code;


    }

    [System.Serializable]
    public class AllCharacterData
    {
        public CharacterStatus Adventurer;
        public CharacterStatus Magician;
        public CharacterStatus Thief;
        public CharacterStatus Warrior;
    }


    [System.Serializable]
    public class GetPlayerData<T>
    {
        public bool success;
        public string message;
        public T get_data;
        public string access_code;
    }


}