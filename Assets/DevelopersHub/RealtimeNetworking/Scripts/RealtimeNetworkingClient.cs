
namespace DevelopersHub.RealtimeNetworking.Client{
    using UnityEngine;
    using DevelopersHub.RealtimeNetworking.Common;

    public class RealtimeNetworkingClient : MonoBehaviour
    {        
        [SerializeField] private Transform ObjectToMove;

        private bool _IsConnected = false;

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

            try{
                var packet = new Packet();
                packet.Write((int)PacketType.Vector3);
                packet.Write(ObjectToMove.localPosition);
                packet.Write(ObjectToMove.localRotation.eulerAngles);
                Sender.UDP_Send(packet);
            } catch (System.Exception e)
            {
                Debug.Log("Error sending packet: " + e.Message);
                TryConnecting();
            }
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