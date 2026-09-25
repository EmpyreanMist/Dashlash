using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Dashlash.World.Encounters;

public class AmbientWildernessZoneTests
{
    private AmbientWildernessZone zone;

    [SetUp]
    public void SetUp()
    {
        zone = new GameObject().AddComponent<AmbientWildernessZone>();
        zone.allowedRoles.Add(new EncounterRole { Name = "TestRole" });
        zone.allowedSpecies.Add(new Species { Name = "TestSpecies" });
        zone.roleWeights[zone.allowedRoles[0]] = 1f;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(zone.gameObject);
    }

    [Test]
    public void SelectRoleByWeight_ReturnsCorrectRole()
    {
        // Arrange
        zone.roleWeights[zone.allowedRoles[0]] = 100f;

        // Act
        EncounterRole result = zone.SelectRoleByWeight();

        // Assert
        Assert.AreEqual(zone.allowedRoles[0], result);
    }

    [Test]
    public void IsValidSpawnPosition_ReturnsFalseForUnsafeTerrain()
    {
        // Arrange
        Vector3 unsafePos = new Vector3(0, -100, 0); // Below terrain

        // Act
        bool result = zone.IsValidSpawnPosition(new EncounterParticipant(null, null, unsafePos, zone));

        // Assert
        Assert.IsFalse(result);
    }

    [Test]
    public void SpawnPopulation_RespectsBounds()
    {
        // Arrange
        zone.minPopulation = 2;
        zone.maxPopulation = 5;

        // Act
        zone.SpawnPopulation();

        // Assert
        Assert.GreaterOrEqual(zone.activeParticipants.Count, 2);
        Assert.LessOrEqual(zone.activeParticipants.Count, 5);
    }

    [Test]
    public void HandleRespawns_RespectsDesiredPopulation()
    {
        // Arrange
        zone.desiredPopulation = 3;
        zone.activeParticipants.Clear();

        // Act
        zone.HandleRespawns();

        // Assert
        Assert.AreEqual(3, zone.activeParticipants.Count);
    }
}