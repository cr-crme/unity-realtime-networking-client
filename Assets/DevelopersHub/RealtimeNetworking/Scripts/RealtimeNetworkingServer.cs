namespace DevelopersHub.RealtimeNetworking.Server{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using DevelopersHub.RealtimeNetworking.Common;
    using DevelopersHub.RealtimeNetworking.Server;
        
    public class RealtimeNetworkingServer : MonoBehaviour
    {
        [SerializeField] private Transform ObjectToMove;
        [SerializeField] private CsvWriter ObjectToSave;

        // Start is called before the first frame update
        void Start()
        {
            RealtimeNetworking.Initialize();

            RealtimeNetworking.OnClientConnected += ClientConnected;
            RealtimeNetworking.OnClientDisconnected += ClientDisconnected;
            RealtimeNetworking.OnPacketReceived += PacketReceived;
            
            Debug.Log("Server started");
        }

        void FixedUpdate(){
            Threading.UpdateMain();
        }


        void ClientConnected(int id, string ip)
        {
            Debug.Log("Client connected: " + id + " " + ip);
        }

        void ClientDisconnected(int id, string ip)
        {
            Debug.Log("Client disconnected: " + id + " " + ip);
        }

        void PacketReceived(int id, Packet packet)
        {
            var packetType = packet.ReadInt();
            switch ((PacketType)packetType)
            {
                case PacketType.Vector3:
                    var timestamp = packet.ReadFloat();
                    var position = packet.ReadVector3();
                    var rotation = packet.ReadVector3();
                    ObjectToMove.position = new Vector3(position.X, position.Y, position.Z);
                    ObjectToMove.rotation = Quaternion.Euler(new Vector3(rotation.X, rotation.Y, rotation.Z));
                    ObjectToSave.AddData(timestamp, position);
                    break;
                default:
                    Debug.Log("Unknown packet type.");
                    break;
            }
        }
    }
}
