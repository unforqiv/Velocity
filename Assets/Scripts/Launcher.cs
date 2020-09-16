using Photon.Pun;
using UnityEngine;

namespace Com.Josh.Velocity
{
	public class Launcher : MonoBehaviourPunCallbacks
	{
		public void Awake()
		{
			PhotonNetwork.AutomaticallySyncScene = true;
			Connect();
		}

		public override void OnConnectedToMaster()
		{
			UnityEngine.Debug.Log("Connected");
			PhotonNetwork.JoinLobby();
			base.OnConnectedToMaster();
		}

		public override void OnJoinedLobby()
		{
			UnityEngine.Debug.Log("Joined");
			base.OnJoinedLobby();
		}

		public override void OnJoinedRoom()
		{
			StartGame();
			base.OnJoinedRoom();
		}

		public override void OnJoinRandomFailed(short returnCode, string message)
		{
			Create();
			base.OnJoinRandomFailed(returnCode, message);
		}

		public void Connect()
		{
			UnityEngine.Debug.Log("Trying to Connect...");
			PhotonNetwork.GameVersion = "0.0.1";
			PhotonNetwork.ConnectUsingSettings();
		}

		public void Join()
		{
			PhotonNetwork.JoinRandomRoom();
		}

		public void Create()
		{
			PhotonNetwork.CreateRoom("");
		}

		public void StartGame()
		{
			if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
			{
				PhotonNetwork.LoadLevel(1);
			}
		}
	}
}
