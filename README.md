Unity-ETH: Ethereum-Integrated Multiplayer Project

Unity-ETH is a Unity project that combines multiplayer gaming capabilities using Unity's Netcode for GameObjects with Ethereum blockchain integration. This project enables players to interact within a multiplayer environment while facilitating Ethereum transactions directly from the Unity application.

Features:

- Multiplayer Support: Utilizes Unity's Netcode for GameObjects to provide a seamless multiplayer experience.

- Ethereum Integration: Allows users to connect their Ethereum wallets and perform transactions within the game.

- User-to-User Transactions: Enables direct Ethereum transactions between players.

Getting Started

Prerequisites:

- Unity 2022.3.0f1 or later: Ensure you have the appropriate Unity version installed.

- .NET 6 SDK: Required for Ethereum integration functionalities.

Installation:

- Clone the Repository:

- git clone https://github.com/joeljunnila/Unity-ETH.git

- Open the Project

- Launch Unity Hub and open the cloned project.

Install Dependencies:

- Netcode for GameObjects: Follow Unity's official guide to install the Netcode package. (docs-multiplayer.unity3d.com)

- Nethereum Library: This library facilitates Ethereum blockchain interactions.

Via OpenUPM:

- Open Edit > Project Settings > Package Manager.

- Add a new Scoped Registry

- Name: package.openupm.com

- URL: https://package.openupm.com

- Scope(s): com.nethereum.unity

- Click Save or Apply.

- Open Window > Package Manager.

- Click the + button and select Add package by name....

- Enter com.nethereum.unity and click Add.

- Manual Installation: Download the latest Nethereum Unity package from the official repository. Import the package into your Unity project.

Configuration:

Ethereum Wallet Connection:

- The project includes scripts to connect with MetaMask and other Ethereum wallets. Ensure the wallet provider's WebGL plugin is integrated into your project.

Network Manager Setup:

- A NetworkManager component is configured to manage multiplayer sessions. Review and adjust its settings to match your project's requirements.

Usage:

Starting a Multiplayer Session:

- Use the in-game UI to host or join multiplayer sessions. The NetworkManagerHUD component provides a basic interface for these actions.

Performing Ethereum Transactions:

- Interact with the in-game UI to initiate Ethereum transactions. The project includes functionalities to send Ether to other players and interact with smart contracts.

Project Structure

- Assets/Scripts/Networking: Contains scripts related to multiplayer networking using Netcode for GameObjects.

- Assets/Scripts/Ethereum: Holds scripts facilitating Ethereum blockchain interactions using Nethereum.

- Assets/Prefabs: Includes pre-configured GameObjects for players and UI elements.

Recent Updates:

March 27, 2025: Removed obsolete UI elements and fixed user-to-user smart contract scripts.

March 26, 2025: Fixed lighting issues and added functionality to specify amounts in Ether send button.

March 21, 2025: Added a button for sending transactions and implemented various bug fixes.

Contributing

Contributions are welcome! Please fork the repository and submit a pull request with your enhancements or bug fixes.

License

This project is licensed under the MIT License. See the LICENSE file for details.

For more information on integrating Ethereum with Unity, refer to the Nethereum Unity Documentation. For guidance on Unity's Netcode for GameObjects, consult the official Unity documentation.
