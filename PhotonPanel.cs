using GorillaNetworking;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhotonPanel : MonoBehaviour
{
    public PhotonNetworkController PhotonManager;
    private string roomToJoin = "";
    private string nameToSet = "";

    void Start()
    {
        if (PhotonManager == null)
        {
            PhotonManager = PhotonNetworkController.instance;
        }
    }

    void OnGUI()
    {
        GUI.color = Color.blue;
        GUI.Box(new Rect(10, 10, 300, 450), "PHOTON PANEL");

        if (GUI.Button(new Rect(20, 40, 280, 40), "JOIN PUBLIC"))
        {
            if (PhotonNetworkController.instance != null)
            {
                if (PhotonNetworkController.instance.currentJoinTrigger == null)
                {
                    PhotonNetworkController.instance.currentJoinTrigger = PhotonNetworkController.instance.privateTrigger;
                }
                PhotonNetworkController.instance.AttemptToJoinPublicRoom(PhotonNetworkController.instance.currentJoinTrigger);
            }
        }

        if (GUI.Button(new Rect(20, 90, 280, 40), "DISCONNECT"))
        {
            if (PhotonNetworkController.instance != null)
            {
                PhotonNetworkController.instance.AttemptDisconnect();
            }
        }

        if (GUI.Button(new Rect(20, 140, 280, 40), "ANTI AFK"))
        {
            if (PhotonManager != null)
            {
                PhotonManager.disableAFKKick = true;
            }
            else if (PhotonNetworkController.instance != null)
            {
                PhotonNetworkController.instance.disableAFKKick = true;
            }
        }

        GUI.Label(new Rect(20, 190, 280, 20), "Room Name:");
        roomToJoin = GUI.TextField(new Rect(20, 210, 280, 30), roomToJoin);

        if (GUI.Button(new Rect(20, 250, 280, 40), "JOIN PRIVATE"))
        {
            if (PhotonNetworkController.instance != null && !string.IsNullOrEmpty(roomToJoin))
            {
                PhotonNetworkController.instance.AttemptToJoinSpecificRoom(roomToJoin.ToUpper());
            }
        }

        if (GUI.Button(new Rect(20, 300, 135, 40), "DESTROY SELF"))
        {
            PhotonNetwork.DestroyPlayerObjects(PhotonNetwork.LocalPlayer);
        }

        if (GUI.Button(new Rect(165, 300, 135, 40), "SET MASTER"))
        {
            PhotonNetwork.SetMasterClient(PhotonNetwork.LocalPlayer);
        }

        GUI.Label(new Rect(20, 350, 280, 20), "New Name:");
        nameToSet = GUI.TextField(new Rect(20, 370, 280, 30), nameToSet);

        if (GUI.Button(new Rect(20, 410, 280, 40), "SET NAME"))
        {
            if (!string.IsNullOrEmpty(nameToSet))
            {
                PhotonNetwork.LocalPlayer.NickName = nameToSet;
                PlayerPrefs.SetString("playerName", nameToSet);
                PlayerPrefs.Save();
            }
        }
    }
}
