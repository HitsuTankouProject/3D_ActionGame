using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;




public class Map : MonoBehaviour
{
    [Header("All Patrol Spots")]
    public List<Transform> allMonsterCanPatrolSpots = new();
    [Header("All Gate")]
    public List<Gate> allGate = new();
    [Header("All Treasure Box")]
    public List<TreasureBox> allTreasureBox = new();



}
