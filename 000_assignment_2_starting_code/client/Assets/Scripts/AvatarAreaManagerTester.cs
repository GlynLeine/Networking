﻿using System.Collections.Generic;
using UnityEngine;

/**
 * This is a test class only there to demonstrate how you can add/remove/etc avatars.
 * It is accessed from the TestUI.
 * 
 * In the final product, adding, moving, removing etc your avatars should be controlled 
 * completely by both the server and the ChatLobbyClient.
 */
public class AvatarAreaManagerTester : MonoBehaviour
{
    public float spawnRange = 10;
    public float spawnMinAngle = 0;
    public float spawnMaxAngle = 180;
    private uint _lastAvatarId = 1;

    private AvatarAreaManager _avatarAreaManager;

    private void Start()
    {
        _avatarAreaManager = FindObjectOfType<AvatarAreaManager>();
        _avatarAreaManager.OnAvatarAreaClicked += MoveRandomAvatarToPosition;
    }

    /**
     * Demonstrates how to create an Avatar with some id.
     */
    public void AddRandomAvatar()
    {
        uint avatarId = _lastAvatarId++;
        AvatarView avatarView = _avatarAreaManager.AddAvatarView(avatarId);
        avatarView.transform.localPosition = getRandomPosition();

        //set a random skin
        avatarView.SetSkin(Random.Range(0, 1000));
    }

    /**
     * Demonstrates how to move an Avatar with some id.
     */
    public void MoveRandomAvatar()
    {
        MoveRandomAvatarToPosition(getRandomPosition());
    }

    public void MoveRandomAvatarToPosition(Vector3 pPosition)
    {
        uint randomAvatarId = getRandomAvatorId();
        if (randomAvatarId == 0) return;

        AvatarView avatarView = _avatarAreaManager.GetAvatarView(randomAvatarId);
        avatarView.Move(pPosition);
    }

    public void SkinRandomAvatar()
    {
        uint randomAvatarId = getRandomAvatorId();
        if (randomAvatarId == 0) return;

        AvatarView avatarView = _avatarAreaManager.GetAvatarView(randomAvatarId);
        avatarView.SetSkin (Random.Range(0,100));
    }

    public void RemoveRandomAvatar()
    {
        uint randomAvatarId = getRandomAvatorId();
        if (randomAvatarId == 0) return;

        _avatarAreaManager.RemoveAvatarView(randomAvatarId);
    }

    public void SaySomethingThroughRandomAvatar()
    {
        uint randomAvatarId = getRandomAvatorId();
        if (randomAvatarId == 0) return;

        string[] randomText =
        {
            "Wazzaaaap?!",
            "Helloooo!",
            "Wanna play a game?",
            "Who are you?",
            "Where is everyone?",
            "BEHIND YOU!",
            "That's a pretty GREAT axe you got there!",
            "Yo momma so easy I rolled a 1 and still hit it!",
            "Oh no u didn't!",
            "How many <color=\"blue\">half-elves</color> does it take to screw in a light bulb? Just one. Turns out, they're actually good for something!"
        };

        _avatarAreaManager.GetAvatarView(randomAvatarId).Say(randomText[Random.Range(0, randomText.Length)]);
    }

    /**
     * Returns a position somewhere in town.
     */
    private Vector3 getRandomPosition()
    {
        //set a random position
        float randomAngle = Random.Range(spawnMinAngle, spawnMaxAngle) * Mathf.Deg2Rad;
        float randomDistance = Random.Range(0, spawnRange);
        return new Vector3(Mathf.Cos(randomAngle), 0, Mathf.Sin(randomAngle)) * randomDistance;
    }

    /**
     * Returns a list of all valid avatars so you can pick one you know that exists.
     */
    private uint getRandomAvatorId()
    {
        List<uint> allAvatarIds = _avatarAreaManager.GetAllAvatarIds();
        if (allAvatarIds.Count == 0) return 0;

        uint randomAvatarId = allAvatarIds[Random.Range(0, allAvatarIds.Count)];
        return randomAvatarId;

    }
}