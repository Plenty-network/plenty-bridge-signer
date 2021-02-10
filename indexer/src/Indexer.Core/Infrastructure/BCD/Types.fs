namespace Indexer.Infrastructure.BCD
open FSharp.Data

type StatsResponse = JsonProvider<"""[
  {
    "network": "carthagenet",
    "hash": "BKwqyWqy9rSNzsoyFp4fuBDaEAd5433f5zAZhRS28w4UUuVAhxN",
    "level": 919518,
    "predecessor": "BL2rpyAyNPnKyBszjfmo2Z9c93LHzQiHYnXFJnJfomGWmHypFFd",
    "chain_id": "NetXjD3HPJJjmcd",
    "protocol": "PsCARTHAGazKbHtnKfLzQg3kms52kSRpgnDY982a9oYsSXRLQEb",
    "timestamp": "2020-12-20T03:28:11Z"
  }
]""">

type StorageResponse = JsonProvider<"""{
  "prim": "pair",
  "type": "namedtuple",
  "children": [
    {
      "prim": "address",
      "type": "address",
      "name": "admin",
      "value": "tz1S792fHX5rvs6GYP49S1U58isZkp2bNmn6"
    },
    {
      "prim": "big_map",
      "type": "big_map",
      "name": "metadata",
      "value": 46471
    },
    {
      "prim": "map",
      "type": "map",
      "name": "signers",
      "children": [
        {
          "prim": "key",
          "type": "key",
          "name": "0024080112203b7e495ee69372f4705df0c93a9b7731e6caa7208a7352b4d34566274ea15d69",
          "value": "sppk7a8xPov96ZwVh7mKi6nkkQS8r8ycYHDp7YahhnF3q1Xb3AQmBpL"
        }
      ]
    },
    {
      "prim": "nat",
      "type": "nat",
      "name": "threshold",
      "value": "1"
    }
  ]
}""">
