# Unity-ETH: Ethereum-Integrated Multiplayer Project

Unity-ETH is a Unity project that combines multiplayer gaming capabilities using Unity's Netcode for GameObjects with Ethereum blockchain integration. This project enables players to interact within a multiplayer environment while facilitating Ethereum transactions directly from the Unity application.

### Features:

- Multiplayer Support: Utilizes Unity's Netcode for GameObjects to provide a seamless multiplayer experience.

- Ethereum Integration: Allows users to connect their Ethereum wallets and perform transactions within the game.

- User-to-User Transactions: Enables direct Ethereum transactions between players.

### Getting Started

Prerequisites:

- Unity 6000.0.41f1 or later: Ensure you have the appropriate Unity version installed.

- .NET 6 SDK: Required for Ethereum integration functionalities.

Installation:

- Clone the Repository:

- git clone https://github.com/ETH-Unity/UnityNethereum

- git clone https://github.com/ETH-Unity/EthNetwork

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

- Enter com.nethereum.multiplayer.playmode and Click Add.

- Manual Installation: Download the latest Nethereum Unity package from the official repository. Import the package into your Unity project.

### Configuration:

Config File Setup:

- The project includes a configKeys.json file in Resources folder. Fill those placeholders with your networkID, rpcUrl, door device's private key, temperature sensor device's private key and deployed smart contract addresses.

Network Manager Setup:

- A NetworkManager component is configured to manage multiplayer sessions. Review and adjust its settings to match your project's requirements.

### Usage:

Starting a Multiplayer Session:

- Use the in-game UI to host or join multiplayer sessions.

Connect Web3 Wallet:

- Use your private key in the "Enter Private Key..." UI input field for the web3 wallet connection.

Performing Ethereum Transactions:

- When the wallet is connected, interact with the in-game UI to initiate Ethereum transactions. The project includes functionalities to send Ether and message to other players and interact with smart contracts.

Buttons:
- Movement: WASD
  
- Interact: "E"
  
- Admin panel: "Q" - Opens UI buttons for Access control

Project Structure

- Assets/Blockchain: Contains scripts related to blockchain and ABI files.

- Assets/Network: Contains scripts related to multiplayer networking usig Netcode for GameObjects.

- Assets/Player: Contains files related to the player, movement and camera.

- Assets/Room: Contains all the files for the room scene, door and fan functionalities included.

### Contributing

Contributions are welcome! Please fork the repository and submit a pull request with your enhancements or bug fixes.

### License

This project is licensed under the MIT License. See the LICENSE file for details.

For more information on integrating Ethereum with Unity, refer to the Nethereum Unity Documentation. For guidance on Unity's Netcode for GameObjects, consult the official Unity documentation.
