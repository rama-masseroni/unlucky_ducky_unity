using System;
using UnityEngine;
using UnityEngine.InputSystem.Controls;


public interface PlaceableInterface
{
    string Name { get; set; }
    int AvailableUnits { get; set; }

    string Rule { get; set; }



}

[CreateAssetMenu(fileName = "PlaceableDefinition", menuName = "Scriptable Objects/PlaceableDefinition")]
public class PlaceableDefinition: ScriptableObject
{

}