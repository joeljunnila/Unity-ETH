using UnityEngine;
using Reown.AppKit.Unity;

public class AppKitInitializer : MonoBehaviour
{
    [Header("Reown-projektin ID")]
    public string projectId = "0a5c17102708f045f859aa25e8434fa4"; // Korvaa omalla

    private async void Awake()
    {
        var config = new AppKitConfig
        {
            projectId = projectId,
            metadata = new Metadata(
                name: "UnityLoginDemo",
                description: "MetaMask login with Reown",
                url: "https://yourgame.example",
                iconUrl: "https://yourgame.example/icon.png"
            )
        };

        await AppKit.InitializeAsync(config);
    }
}