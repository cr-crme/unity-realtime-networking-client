namespace DevelopersHub.RealtimeNetworking.Server{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using DevelopersHub.RealtimeNetworking.Common;
    using DevelopersHub.RealtimeNetworking.Server;
        
    public class RealtimeNetworkingServer : MonoBehaviour
    {
        [SerializeField] private List<Transform> _objectsToMove;
        [SerializeField] private CsvWriter _objectToSave;
        [SerializeField] private Text _ipAddressText;

        // Start is called before the first frame update
        void Start()
        {
            ShowIpAddress();

            RealtimeNetworking.Initialize();

            RealtimeNetworking.OnClientConnected += ClientConnected;
            RealtimeNetworking.OnClientDisconnected += ClientDisconnected;
            RealtimeNetworking.OnPacketReceived += PacketReceived;
            
            Debug.Log("Server started");
        }

        void FixedUpdate(){
            Threading.UpdateMain();
        }

        void OnDestroy()
        {
            RealtimeNetworking.Destroy();
        }

        void ShowIpAddress()
        {
            List<string> ipAddresses = Tools.FindCurrentIPs();
            if (ipAddresses.Count > 1)
            {
                _ipAddressText.text = "Trop d'adresses IP trouvées";
            } else if (ipAddresses.Count == 0)
            {
                _ipAddressText.text = "Aucune adresse IP trouvée";
            } else
            {
                _ipAddressText.text = ipAddresses[0];
            }

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
                case PacketType.CsvWriterDataEntry:
                    var timestamp = packet.ReadFloat();
                    var dataToWrite= new CsvWriter.DataEntry(timestamp);

                    foreach (var item in _objectsToMove)
                    {
                        var position = packet.ReadVector3();
                        var rotation = packet.ReadVector3();
                    
                        item.position = new Vector3(position.X, position.Y, position.Z);
                        item.rotation = Quaternion.Euler(new Vector3(rotation.X, rotation.Y, rotation.Z));

                        dataToWrite.poses.Add(new CsvWriter.PoseVectors(position, rotation));
                    }

                    _objectToSave.AddData(dataToWrite);

                    break;

                default:
                    Debug.Log("Unknown packet type.");
                    break;
            }
        }
    }
}
