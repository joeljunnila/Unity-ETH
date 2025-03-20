mergeInto(LibraryManager.library, {
    ConnectMetaMask: function () {
        if (typeof window.ethereum !== 'undefined') {
            window.ethereum.request({ method: 'eth_requestAccounts' }).then(function(accounts) {
                var account = accounts[0];
                SendMessage('MetaMaskManager', 'ReceiveAccount', account);
            }).catch(function(error) {
                SendMessage('MetaMaskManager', 'ReceiveAccount', 'Error: ' + error.message);
            });
        } else {
            SendMessage('MetaMaskManager', 'ReceiveAccount', 'MetaMask Not Found');
        }
    }
});