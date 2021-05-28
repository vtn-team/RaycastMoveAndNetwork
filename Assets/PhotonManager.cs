using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

public class PhotonManager : MonoBehaviourPunCallbacks
{
    public enum PhotonState
    {
        INIT,
        CONNECTED,
        IN_LOBBY,
        IN_GAME,
        DISCONNECTED,
        WAITING,
    }

    #region Singleton
    static PhotonManager _instance = null;

    public static bool IsEmpty
    {
        get { return _instance == null; }
    }

    public static PhotonManager Instance
    {
        get
        {
            if (_instance == null)
            {
                System.Type type = typeof(PhotonManager);
                _instance = GameObject.FindObjectOfType(type) as PhotonManager;
            }

            return _instance;
        }
    }
    #endregion

    const int MAX_PLAYER_IN_ROOM = 10;
    private List<RoomInfo> CachedRoomList = null;
    PhotonState State;

    static public bool IsOnline { get { return _instance && _instance.State == PhotonState.IN_GAME; } }

    public struct UserParam
    {
        public string Name;
    };

    UserParam _mySelf;
    UserParam[] _player = new UserParam[MAX_PLAYER_IN_ROOM];

    public UserParam Me { get => _mySelf; }
    public UserParam GetPlayer(int id)
    {
        return _player[id];
    }

    private void Start()
    {
        Connect();
    }

    /// <summary>
    /// 接続開始(オフライン時は使用しない)
    /// </summary>
    public void Connect()
    {
        Debug.Log("Connect");

        //FPS調整
        PhotonNetwork.SendRate = 60;
        PhotonNetwork.SerializationRate = 60;

        PhotonNetwork.NickName = "test";
        PhotonNetwork.ConnectUsingSettings();

        State = PhotonState.WAITING;
    }

    public void LeaveRoom()
    {
        Debug.Log("LeaveRoom");
        PhotonNetwork.LeaveRoom();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("OnConnectedToMaster");
        State = PhotonState.CONNECTED;
    }

    private void Update()
    {
#if DEBUG
        if (Input.GetKeyDown(KeyCode.N))
        {
            CachedRoomList.ForEach(info =>
            {
                Debug.Log(info.CustomProperties["UserName"]);
                Debug.Log(info.Name);
            });
            Debug.Log(PhotonNetwork.CountOfPlayers);
        }
#endif

        switch (State)
        {
            //接続開始
            case PhotonState.CONNECTED:
                JoinLobby();
                State = PhotonState.WAITING;
                break;

            //ロビーで部屋選択
            case PhotonState.IN_LOBBY:
                {
                    //ルームリストもらうまで間があるので待機する
                    if (CachedRoomList == null) break;

                    //いまのロビーにいる人のルームリストをもらい、もらったルームからマッチング相手を探す
                    if (CheckAndJoinRoom())
                    {
                        State = PhotonState.WAITING;
                    }
                    else
                    {
                        CreateRoom();
                        State = PhotonState.IN_GAME;
                    }
                }
                break;
                
            //ゲーム中
            case PhotonState.IN_GAME:
                break;
        }
    }

    void JoinLobby()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("OnJoinedLobby:" + PhotonNetwork.CurrentLobby.Name);

        RoomOptions roomOptions = new RoomOptions();

        //カスタムプロパティ
        ExitGames.Client.Photon.Hashtable prop = new ExitGames.Client.Photon.Hashtable();
        prop["GUID"] = Guid.NewGuid().ToString();
        prop["Name"] = "test";
        PhotonNetwork.SetPlayerCustomProperties(prop);

        _mySelf.Name = "test";
        State = PhotonState.IN_LOBBY;
    }

    bool CheckAndJoinRoom()
    {
        Debug.Log("CheckAndJoinRoom");

        var list = CachedRoomList.Where(info =>
        {
            //入れない部屋は除外
            if (info.PlayerCount >= MAX_PLAYER_IN_ROOM) return false;

            return true;
        }).ToList();

        if (list.Count > 0)
        {
            Debug.Log("部屋があったので適当にはいる:" + list.Count);
            PhotonNetwork.JoinRoom(list[UnityEngine.Random.Range(0, list.Count)].Name);
            return true;
        }
        return false;
    }

    void CreateRoom()
    {
        Debug.Log("CreateRoom");
        RoomOptions roomOptions = new RoomOptions();

        //カスタムプロパティ
        ExitGames.Client.Photon.Hashtable roomProp = new ExitGames.Client.Photon.Hashtable();
        roomProp["UserName"] = "test";
        roomProp["GameState"] = 0;

        roomOptions.IsVisible = true;
        roomOptions.MaxPlayers = MAX_PLAYER_IN_ROOM;
        roomOptions.CustomRoomProperties = roomProp;
        roomOptions.CustomRoomPropertiesForLobby = new string[] { "UserName", "GameState" };
        PhotonNetwork.CreateRoom(Guid.NewGuid().ToString(), roomOptions, TypedLobby.Default);
    }

    public override void OnLeftRoom()
    {
        Debug.Log("OnLeftRoom");
        base.OnLeftRoom();
        PhotonNetwork.Disconnect();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("OnJoinedRoom");
        if (PhotonNetwork.IsMasterClient)
        {

        }

        //プレイヤー作成
        PhotonNetwork.Instantiate("Sphere", new Vector3(UnityEngine.Random.Range(-40, 40), 1, UnityEngine.Random.Range(-40, 40)), Quaternion.identity);

        UpdateUserStatus();
        State = PhotonState.IN_GAME;
    }

    void UpdateUserStatus()
    {
        Debug.Log("UpdateUserStatus");
        Photon.Realtime.Room room = PhotonNetwork.CurrentRoom;
        if (room == null)
        {
            return;
        }
        
        int i = 0;
        foreach (var pl in room.Players.Values)
        {
            if (pl.CustomProperties["GUID"] == null) continue;
            if (pl.CustomProperties["UserName"] == null) continue;

            _player[i].Name = pl.CustomProperties["UserName"].ToString();
        }
    }

    //PUN2でのルーム取得
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log("OnRoomListUpdate");
        base.OnRoomListUpdate(roomList);

        CachedRoomList = roomList;
    }
}