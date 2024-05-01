
namespace DevelopersHub.RealtimeNetworking.Client{
    using UnityEngine;
    using DevelopersHub.RealtimeNetworking.Common;

    public class RealtimeNetworkingClient : MonoBehaviour
    {        
        [SerializeField] private Transform ObjectToMove;

        private bool _IsConnected = false;
        private float _TimeStamp = 0.0f; 

        // Start is called before the first frame update
        void Start()
        {
            RealtimeNetworking.OnDisconnectedFromServer += TryConnecting;
            RealtimeNetworking.OnPacketReceived += OnPacketReceived;

            TryConnecting();
        }

        void FixedUpdate()
        {
            if (!_IsConnected) return;

            try
            {
                var packet = new Packet();
                packet.Write((int)PacketType.CsvWriterDataEntry);
                packet.Write(_TimeStamp);
                packet.Write(ObjectToMove.localPosition);
                packet.Write(ObjectToMove.localRotation.eulerAngles);
                Sender.TCP_Send(packet);
            } catch (System.Exception e)
            {
                Debug.Log("Error sending packet: " + e.Message);
                TryConnecting();
            }

            _TimeStamp += Time.fixedDeltaTime;
        }

        void OnConnectionResult(bool success)
        {
            _IsConnected = success;

            if (_IsConnected)
            {
                Debug.Log("Connected to server");
                RealtimeNetworking.OnConnectingToServerResult -= OnConnectionResult;
            }
        }

        void TryConnecting()
        {
            _IsConnected = false;
            _TimeStamp = 0.0f;
            RealtimeNetworking.OnConnectingToServerResult += OnConnectionResult;

            StartCoroutine(TryConnectingCoroutine());
        }

        System.Collections.IEnumerator TryConnectingCoroutine()
        {
            // Just make sure it is disconnected
            RealtimeNetworking.Disconnect();

            while (!_IsConnected)
            {
                Debug.Log("Trying to Connect...");
                RealtimeNetworking.Connect();
                // Pause for a second before trying to reconnect
                yield return new WaitForSeconds(1);
            }

        }

        void OnPacketReceived(Packet packet)
        {
            Debug.Log("Packet received: " + packet.ReadString());
        }

        // Update is called once per frame
        void Update()
        {
            
        }
    }
}