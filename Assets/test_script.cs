using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DevelopersHub.RealtimeNetworking.Client;

public class test_script : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        RealtimeNetworking.OnConnectingToServerResult += coucou;
        RealtimeNetworking.Connect();
    }

    void coucou(bool success)
    {
        Debug.Log("Is connected? " + success);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
