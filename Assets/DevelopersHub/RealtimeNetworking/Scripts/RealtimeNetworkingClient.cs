
namespace DevelopersHub.RealtimeNetworking.Client
{
    using System.Collections;
    using System.Collections.Generic;
    using DevelopersHub.RealtimeNetworking.Common;
    using UnityEngine;

    public class RealtimeNetworkingClient : MonoBehaviour
    {        
        [SerializeField] private List<Transform> _objectsToMove;
        [SerializeField] private string _serverIp = "127.0.0.1";
        [SerializeField] private int _serverPort = 5555;

        private bool _isConnected = false;
        private float _timeStamp = 0.0f; 

        // Start is called before the first frame update
        void Start()
        {
            RealtimeNetworking.OnDisconnectedFromServer += TryConnecting;
            RealtimeNetworking.OnPacketReceived += OnPacketReceived;

            TryConnecting();
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        void FixedUpdate()
        {
            Threading.UpdateMain();

            if (!_isConnected) return;

            try
            {
                var packet = new Packet();
                packet.Write((int)PacketType.CsvWriterDataEntry);
                packet.Write(_timeStamp);
                foreach (var item in _objectsToMove)
                {
                    packet.Write(item.localPosition);
                    packet.Write(item.localRotation.eulerAngles);
                }
                Sender.TCP_Send(packet);

            } catch (System.Exception e)
            {
                Debug.Log("Connection lost");
                TryConnecting();
            }

            _timeStamp += Time.fixedDeltaTime;
        }

        void OnConnectionResult(bool success)
        {
            _isConnected = success;

            if (_isConnected)
            {
                Debug.Log("Connected to server");
                RealtimeNetworking.OnConnectingToServerResult -= OnConnectionResult;
            }
        }

        void TryConnecting()
        {
            _isConnected = false;
            _timeStamp = 0.0f;
            RealtimeNetworking.OnConnectingToServerResult += OnConnectionResult;

            StartCoroutine(TryConnectingCoroutine());
        }

        System.Collections.IEnumerator TryConnectingCoroutine()
        {
            // Just make sure it is disconnected
            RealtimeNetworking.Disconnect();

            while (!_isConnected)
            {
                Debug.Log("Trying to Connect...");
                RealtimeNetworking.Connect(_serverIp, _serverPort);
                // Pause for a second before trying to reconnect
                yield return new WaitForSeconds(1);
            }

        }

        void OnPacketReceived(Packet packet)
        {
            Debug.Log("Packet received: " + packet.ReadString());
        }

        
        private void OnApplicationQuit()
        {
            RealtimeNetworking.Disconnect();
        }
    }
}