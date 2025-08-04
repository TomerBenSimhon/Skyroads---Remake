using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GridPlacementData", menuName = "Tools/Grid Placement Data")]
public class GridPlacementData : ScriptableObject
{
    public List<Vector3> occupiedPositions = new();
}