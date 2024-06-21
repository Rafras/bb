
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using BoomBit.HyperCasual;
using UnityEngine.Networking;
using System.Text;
using Coredian.Privacy;
public class k : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private async void OpenConsentManagement()
    {
        // Await consent management view without storing the result.
        // This code assumes that classes that use consent are subscribed to the ConsentStatusChanged event.
        await Core.GetService<IConsentService>().OpenConsentManagementAsync();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
