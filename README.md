# 🌐 Signer Wiki
Everything about signer can be found in detail [here](https://github.com/Plenty-DeFi/plenty-bridge-signer/wiki/).

## 📝 About

Plenty bridge signer -  Verifies each bridge & un-bridge transaction and maintains a 1:1 peg of all the tokens on both the chains.

Signers run both an EVM node and a Tezos node. The Ethereum node allows them to watch the deposit contract for assets waiting to be bridged, and prepare transactions out of the same contract for un-bridging. By running a Tezos node, signers can interact with the Quorum Contract during bridging, and watch the Minter Contract during un-bridging.

Signer do not actually broadcast transactions on the Tezos or EVM blockchain. Instead, it sign .e tokens minting instructions (resp. original asset release transactions), then store them on IPFS for users to pick them up and present them to the Quorum Contract (resp. Deposit Contract). Signers are identified by users using IPNS. Signers can also use relay nodes.

Signer infrastructure look as below

<img width="387" alt="image" src="https://user-images.githubusercontent.com/57598532/182734850-18820607-44e1-49e6-b4ca-221384e5bb24.png">

## 💻 Setup

To run application from source code - please follow [setup instructions](https://github.com/Plenty-DeFi/plenty-bridge-signer/wiki/Setup)

## ▶️ Current quorum members
* Baking Bad
* Codecrafting Labs
* Madfish
* Tezos Ukraine
* MIDL.dev
* Integro Labs, LLC
* Tezsure
