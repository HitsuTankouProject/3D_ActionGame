using UnityEngine;
using System.Collections.Generic;

public class Goal : MonoBehaviour
{
    private InGame _inGame => InGame.Instance;
    private List<Character> _allCharacters => _inGame.allPlayerCharacters;
    private List<Character> allCharactersInGoal = new();

    private bool IsEndGame()
    {
        allCharactersInGoal.RemoveAll(character => character == null);
        if(_allCharacters == null|| _allCharacters.Count <= 0) return false;
        foreach (Character character in allCharactersInGoal)
        {
            if (!_allCharacters.Contains(character))
            {
                Debug.LogError("Character doesn't add in allPlayerCharacters Before :" + character);
                return false;
            }
        }
        return  _allCharacters.Count == allCharactersInGoal.Count;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.TryGetComponent<Character>(out Character character)) return;

        allCharactersInGoal.Add(character);
        if (IsEndGame())
        {

        }


    }
    private void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.TryGetComponent<Character>(out Character character)) return;
        if (allCharactersInGoal.Contains(character)) allCharactersInGoal.Remove(character);
        else Debug.LogWarning("Character didn't Add Before it ge inside" + character);

    }
}
