namespace DevelopersHub.RealtimeNetworking.Client
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public class RealtimeNetworking : MonoBehaviour
    {

        #region Events
        public static event NoCallback OnDisconnectedFromServer;
        public static event ActionCallback OnConnectingToServerResult;
        public static event PacketCallback OnPacketReceived;
        #endregion

        #region Callbacks
        public delegate void ActionCallback(bool successful);
        public delegate void NoCallback();
        public delegate void PacketCallback(Packet packet);
        #endregion

        private bool _initialized = false;
        private bool _connected = false; public static bool isConnected { get { return instance._connected; } }

        private static RealtimeNetworking _instance = null; public static RealtimeNetworking instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<RealtimeNetworking>();
                    if (_instance == null)
                    {
                        _instance = Client.instance.gameObject.AddComponent<RealtimeNetworking>();
                    }
                    _instance.Initialize();
                }
                return _instance;
            }
        }

        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }
            Application.runInBackground = true;
            _initialized = true;
        }

        private void OnEnable()
        {

        }

        private void OnDisable()
        {

        }

        private void Update()
        {
           
        }

        public static void Connect()
        {
            Client.instance.ConnectToServer();
        }

        public static void Disconnect()
        {
            Client.instance._Disconnect();
        }

        public void _Connection(bool result)
        {
            _connected = result;
            if (OnConnectingToServerResult != null)
            {
                OnConnectingToServerResult.Invoke(result);
            }
        }

        public void _Disconnected()
        {
            _connected = false;
            if (OnDisconnectedFromServer != null)
            {
                OnDisconnectedFromServer.Invoke();
            }
        }

        public void _ReceivePacket(Packet packet)
        {
            if (OnPacketReceived != null)
            {
                OnPacketReceived.Invoke(packet);
            }
        }

        private static void SendTCPDataInternal(Packet _packet)
        {
            if (_packet == null)
            {
                return;
            }
            _packet.SetID((int)Packet.ID.INTERNAL);
            _packet.WriteLength();
            Client.instance.tcp.SendData(_packet);
        }

        private static void SendUDPDataInternal(Packet _packet)
        {
            if (_packet == null)
            {
                return;
            }
            _packet.SetID((int)Packet.ID.INTERNAL);
            _packet.WriteLength();
            Client.instance.udp.SendData(_packet);
        }
    }
}