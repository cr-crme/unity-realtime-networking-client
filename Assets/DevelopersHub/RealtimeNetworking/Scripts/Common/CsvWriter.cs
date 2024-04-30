using System.Collections.Generic;
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

        public class DataEntry
        {
            public float Timestamp { get; }
            public System.Numerics.Vector3 Position { get; }

            public DataEntry(float timestamp, System.Numerics.Vector3 position)
            {
                Timestamp = timestamp;
                Position = position;
            }

            public override string ToString()
            {
                return $"{Timestamp},{Position.X},{Position.Y},{Position.Z}";
            }
        }

        void Start()
        {
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
            fileWriter.WriteLine("Frame,X,Y,Z");

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

        void AddData(DataEntry data)
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