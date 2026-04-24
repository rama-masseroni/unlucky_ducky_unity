using System;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "Script_Solid_Block", menuName = "Scriptable Objects/Script_Solid_Block")]
public class Script_Solid_Block : ScriptableObject
{
    public TileBase[] tiles;

    public Boolean isDestructuble;

    [Header("Rigidbody2D Settings")]
    [Tooltip("Enable adding a Rigidbody2D to the spawned block.")]
    public bool useRigidbody2D = true;

    [Tooltip("Type of the Rigidbody2D. Use Static to remain planted in place.")]
    public RigidbodyType2D bodyType = RigidbodyType2D.Static;

    [Tooltip("Mass of the body (only used for Dynamic bodies).")]
    public float mass = 1f;

    [Tooltip("Gravity scale applied to the body.")]
    public float gravityScale = 0f;

    [Tooltip("Linear drag applied to the body.")]
    public float linearDrag = 0f;

    [Tooltip("Angular drag applied to the body.")]
    public float angularDrag = 0.05f;

    [Tooltip("Constraints applied to the Rigidbody2D (freeze position/rotation).")]
    public RigidbodyConstraints2D constraints = RigidbodyConstraints2D.FreezeAll;

    [Tooltip("If false, the Rigidbody2D will be disabled (not simulated).")]
    public bool simulated = true;

}
