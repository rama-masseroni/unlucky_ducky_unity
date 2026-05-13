using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BombExplosionAreaTests
{
    [Test]
    public void GetCells_WithRadiusOne_ReturnsThreeByThreeAreaCenteredOnBomb()
    {
        Type areaType = Type.GetType("BombExplosionArea, Assembly-CSharp");
        Assert.IsNotNull(areaType);

        MethodInfo getCellsMethod = areaType.GetMethod("GetCells", BindingFlags.Public | BindingFlags.Static);
        Assert.IsNotNull(getCellsMethod);

        IEnumerable cells = (IEnumerable)getCellsMethod.Invoke(null, new object[] { new Vector3Int(2, 2, 0), 1 });

        int count = 0;
        bool containsBottomLeft = false;
        bool containsCenter = false;
        bool containsTopRight = false;

        foreach (object cellObject in cells)
        {
            Vector3Int cell = (Vector3Int)cellObject;
            count++;
            containsBottomLeft |= cell == new Vector3Int(1, 1, 0);
            containsCenter |= cell == new Vector3Int(2, 2, 0);
            containsTopRight |= cell == new Vector3Int(3, 3, 0);
        }

        Assert.AreEqual(9, count);
        Assert.IsTrue(containsBottomLeft);
        Assert.IsTrue(containsCenter);
        Assert.IsTrue(containsTopRight);
    }
}
