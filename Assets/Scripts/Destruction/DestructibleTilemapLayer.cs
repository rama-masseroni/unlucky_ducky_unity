using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class DestructibleTilemapLayer : MonoBehaviour
{
    public Tilemap Tilemap => GetComponent<Tilemap>();
}
