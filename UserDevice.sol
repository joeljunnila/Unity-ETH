// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

contract UserDevice {
    mapping(address => bool) public accessList;
    address public owner;
    address public doorAccount;
    address[] private accessArray;

    event AccessGranted(address indexed user);
    event AccessRevoked(address indexed user);
    event DoorOpened(address indexed user);

    modifier onlyOwner() {
        require(msg.sender == owner, "Only owner can manage access");
        _;
    }

    modifier onlyDoor() {
        require(msg.sender == doorAccount, "Only the door can sign this action");
        _;
    }

    constructor(address _doorAccount) {
        owner = msg.sender;
        doorAccount = _doorAccount;
        accessList[msg.sender] = true;
        accessArray.push(msg.sender); // Add owner to access list
    }

    function grantAccess(address _user) public onlyOwner {
        require(!accessList[_user], "User already has access");
        accessList[_user] = true;
        accessArray.push(_user);
        emit AccessGranted(_user);
    }

    function revokeAccess(address _user) public onlyOwner {
        require(accessList[_user], "User does not have access");
        accessList[_user] = false;
        emit AccessRevoked(_user);
    }

    function getAccessList() public view returns (address[] memory) {
        return accessArray;
    }

    function canOpenDoor(address _user) public view returns (bool) {
        return accessList[_user];
    }

    function openDoor(address _user) public onlyDoor {
        require(accessList[_user], "Access Denied");
        emit DoorOpened(_user);
    }
}