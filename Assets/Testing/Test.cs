using Cysharp.Threading.Tasks;
using Fusion;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TextCore.Text;


public class Test : MonoBehaviour, IDamageable
{
    
    public void TakeDamage(int damage)
    {

        Debug.Log($"TakeDamage called with damage: {damage}");
    }

}