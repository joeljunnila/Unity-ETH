# Unity-ETH: Ethereum-Integrated Multiplayer Project

Unity-ETH is a Unity project that combines multiplayer gaming capabilities using Unity's Netcode for GameObjects with Ethereum blockchain integration. This project enables players to interact within a multiplayer environment while facilitating Ethereum transactions directly from the Unity application.

## Features

- **Multiplayer Support**: Utilizes Unity's Netcode for GameObjects to provide a seamless multiplayer experience
- **Ethereum Integration**: Allows users to connect their Ethereum wallets and perform transactions within the game
- **User-to-User Transactions**: Enables direct Ethereum transactions between players

## Getting Started

### Prerequisites

- Unity 6000.0.41f1 or later
- .NET 6 SDK: Required for Ethereum integration functionalities

### Installation

1. Clone the repository:
   ```
   git clone https://github.com/ETH-Unity/UnityNethereum
   ```

2. Open the project:
   - Launch Unity Hub and open the cloned project

3. Install Dependencies:

   #### Netcode for GameObjects
   - Follow Unity's official guide to install the Netcode package: [Install Netcode for GameObjects](https://docs-multiplayer.unity3d.com/netcode/current/installation/index.html)

   #### Nethereum Library
   Via OpenUPM:
   - Open Edit > Project Settings > Package Manager
   - Add a new Scoped Registry:
     - Name: `package.openupm.com`
     - URL: `https://package.openupm.com`
     - Scope(s): `com.nethereum.unity` `com.reown`
   - Click Save or Apply
   - Open Window > Package Manager
   - Click the + button and select "Add package by name..."
   - Enter `com.nethereum.unity` and click Add
   - Enter `com.unity.multiplayer.playmode` and click Add
   - Enter `com.reown.core` and click Add
   - Enter `com.reown.appkit.unity` and click Add
   - Enter `com.reown.sign` and click Add

   Manual Installation:
   - Download the latest Nethereum Unity package from the official repository
   - Import the package into your Unity project

## Configuration

### Config File Setup

- The project includes a `configKeys.json` file in the Resources folder
- Fill in the required fields:
  - networkID
  - rpcUrl
  - door device's private key
  - temperature sensor device's private key
  - deployed smart contract addresses

### Network Manager Setup

- A NetworkManager component is configured to manage multiplayer sessions
- Review and adjust its settings to match your project's requirements

## Usage

### Loading the Scene

- Navigate to the Room folder in Unity and start the BasicRoom-scene and start the game

### Starting a Multiplayer Session

- Use the in-game UI to host or join multiplayer sessions

### Connect Web3 Wallet

- Use your Metamask, TrustWallet or WalletConnect to connect your wallet via login button, or private key in the "Enter Private Key..." UI input field

### Performing Ethereum Transactions

- When the wallet is connected, interact with the in-game UI to initiate Ethereum transactions
- The project includes functionalities to send Ether and messages to other players and interact with smart contracts

### Room Access Controlling

- There is three rooms in the scene with different role accesses, physical, digital and admin room. Only admins can control other accounts accesses to the rooms and service role can access any room by default. Admins and service role has access to the admin room, where they can see all the logs of actions happened in the scene (as access granted, door opened etc.) If someone outside of the game has access and interacts with the smart contract, he spawns in the physical room as a NPC (simulating real world person and room). 

### Controls

- Movement: WASD
- Interact: "E"
- Admin panel: "Q" - Opens UI buttons for Access control

## Project Structure

- **Assets/Blockchain**: Contains scripts related to blockchain and ABI files
- **Assets/Network**: Contains scripts related to multiplayer networking using Netcode for GameObjects
- **Assets/Player**: Contains files related to the player, movement and camera
- **Assets/Room**: Contains all the files for the room scene, door and fan functionalities included

## Contributing

Contributions are welcome! Please fork the repository and submit a pull request with your enhancements or bug fixes.

## License

This project is licensed under the MIT License. See the LICENSE file for details.
