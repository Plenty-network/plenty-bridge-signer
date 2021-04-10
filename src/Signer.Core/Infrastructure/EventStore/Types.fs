namespace Signer.EventStore

[<CLIMutable>]
type Erc20ParametersDto =
    { amount: string
      owner: string
      erc20: string
      blockHash: string
      logIndex: bigint }

[<CLIMutable>]
type Erc721ParametersDto =
    { tokenId: string
      owner: string
      erc721: string
      blockHash: string
      logIndex: bigint }

[<CLIMutable>]
type QuorumDto =
    { quorumContract: string
      minterContract: string
      chainId: string }

[<CLIMutable>]
type ErcMintDto<'T> =
    { level: string
      transactionHash: string
      parameters: 'T
      signerAddress: string
      signature: string
      quorum: QuorumDto }

[<CLIMutable>]
type ErcUnwrapDto<'T> =
    { level: string
      parameters: 'T
      signerAddress: string
      signature: string
      lockingContract: string }

[<CLIMutable>]
type Erc20UnwrapParametersDto =
    { amount: string
      owner: string
      erc20: string
      operationId: string }

[<CLIMutable>]
type Erc721UnwrapParametersDto =
    { tokenId: string
      owner: string
      erc721: string
      operationId: string }

