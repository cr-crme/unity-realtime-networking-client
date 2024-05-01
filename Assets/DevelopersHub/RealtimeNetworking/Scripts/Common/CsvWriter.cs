using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace DevelopersHub.RealtimeNetworking.Common
{
    public class CsvWriter : MonoBehaviour
    {
        [SerializeField] private InputField subjectNameInput;
        [SerializeField] private InputField trialNameInput;
        [SerializeField] private Button startButton;
        [SerializeField] private Button stopButton;

        string FilePath { get { return Path.Combine(Application.persistentDataPath, subjectNameInput.text, $"{trialNameInput.text}.csv"); } }
        private StreamWriter fileWriter;
        
        private bool isRecording = false;
        private List<string> dataQueue = new List<string>();
        private int frameCount = 0;
        private const int framesPerFlush = 100;
        private readonly object _lock = new object(); 

        public class PoseVectors
        {
            public System.Numerics.Vector3 Position { get; }
            public System.Numerics.Vector3 Rotation { get; }
            
            public PoseVectors(System.Numerics.Vector3 position, System.Numerics.Vector3 rotation)
            {
                Position = position;
                Rotation = rotation;
            }

        }

        public class DataEntry
        {
            public float Timestamp { get; }
            public List<PoseVectors> Poses { get; } = new List<PoseVectors>();

            public DataEntry(float timestamp)
            {
                Timestamp = timestamp;
            }

            public static string Header { 
                get {
                    return "Frame,Pos.X,Pos.Y,Pos.Z,Rot.X,Rot.Y,Rot.Z";
                }
            }

            public override string ToString()
            {
                var _out = $"{Timestamp:F6}";
                foreach (var item in Poses)
                {
                    _out += $",{item.Position.X:F6},{item.Position.Y:F6},{item.Position.Z:F6}";
                    _out += $",{item.Rotation.X:F6},{item.Rotation.Y:F6},{item.Rotation.Z:F6}";
                }
                return _out;
            }
        }

        void Start()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture; // Force the "." to be the decimal separator

            startButton.interactable = false;
            startButton.gameObject.SetActive(true);
            stopButton.interactable = false;
            stopButton.gameObject.SetActive(false);
        }

        public void ValidateDataPath()
        {
            if (string.IsNullOrEmpty(subjectNameInput.text) || string.IsNullOrEmpty(trialNameInput.text))
            {
                startButton.interactable = false;
                return;
            }

            // Do not allow recording if the file already exists
            if (File.Exists(FilePath))
            {
                startButton.interactable = false;
                return;
            }

            startButton.interactable = true;
        }

        public void StartRecording()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));

            fileWriter = new StreamWriter(FilePath);
            fileWriter.WriteLine(DataEntry.Header);

            startButton.interactable = false;
            startButton.gameObject.SetActive(false);
            stopButton.interactable = true;
            stopButton.gameObject.SetActive(true);

            isRecording = true;
        }

        public void StopRecording()
        {
            if (fileWriter != null)
            {
                FlushDataToFile();
                fileWriter.Close();
                fileWriter = null;
            }

            startButton.interactable = true;
            startButton.gameObject.SetActive(true);
            stopButton.interactable = false;
            stopButton.gameObject.SetActive(false);

            isRecording = false;
        }

        public void AddData(DataEntry data)
        {
            if (!isRecording)
            {
                return;
            }

            // Add data to the queue
            lock (_lock)
            {
                dataQueue.Add(data.ToString());

                // Flush data to file every framesPerFlush frames
                frameCount++;
                if (frameCount >= framesPerFlush)
                {
                    FlushDataToFile();
                }
            }
        }

        private void FlushDataToFile()
        {
            lock (_lock)
            {
                foreach (var data in dataQueue)
                {
                    fileWriter.WriteLine(data);
                }

                fileWriter.Flush();
                dataQueue.Clear();
                frameCount = 0;
            }

        }

        void OnDestroy()
        {
            StopRecording();
        }
    }
}