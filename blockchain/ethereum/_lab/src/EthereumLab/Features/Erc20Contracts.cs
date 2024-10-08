using System.Numerics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

namespace EthereumLab.Features;

public static class Erc20Contracts
{
    private const string Abi = @"
[
  {
    ""anonymous"": false,
    ""inputs"": [
      {
        ""indexed"": true,
        ""internalType"": ""address"",
        ""name"": ""owner"",
        ""type"": ""address""
      },
      {
        ""indexed"": true,
        ""internalType"": ""address"",
        ""name"": ""spender"",
        ""type"": ""address""
      },
      {
        ""indexed"": false,
        ""internalType"": ""uint256"",
        ""name"": ""value"",
        ""type"": ""uint256""
      }
    ],
    ""name"": ""Approval"",
    ""type"": ""event""
  },
  {
    ""anonymous"": false,
    ""inputs"": [
      {
        ""indexed"": true,
        ""internalType"": ""address"",
        ""name"": ""from"",
        ""type"": ""address""
      },
      {
        ""indexed"": true,
        ""internalType"": ""address"",
        ""name"": ""to"",
        ""type"": ""address""
      },
      {
        ""indexed"": false,
        ""internalType"": ""uint256"",
        ""name"": ""value"",
        ""type"": ""uint256""
      }
    ],
    ""name"": ""Transfer"",
    ""type"": ""event""
  },
  {
    ""inputs"": [
      {
        ""internalType"": ""address"",
        ""name"": ""owner"",
        ""type"": ""address""
      },
      {
        ""internalType"": ""address"",
        ""name"": ""spender"",
        ""type"": ""address""
      }
    ],
    ""name"": ""allowance"",
    ""outputs"": [
      {
        ""internalType"": ""uint256"",
        ""name"": """",
        ""type"": ""uint256""
      }
    ],
    ""stateMutability"": ""view"",
    ""type"": ""function""
  },
  {
    ""inputs"": [
      {
        ""internalType"": ""address"",
        ""name"": ""spender"",
        ""type"": ""address""
      },
      {
        ""internalType"": ""uint256"",
        ""name"": ""amount"",
        ""type"": ""uint256""
      }
    ],
    ""name"": ""approve"",
    ""outputs"": [
      {
        ""internalType"": ""bool"",
        ""name"": """",
        ""type"": ""bool""
      }
    ],
    ""stateMutability"": ""nonpayable"",
    ""type"": ""function""
  },
  {
    ""inputs"": [
      {
        ""internalType"": ""address"",
        ""name"": ""account"",
        ""type"": ""address""
      }
    ],
    ""name"": ""balanceOf"",
    ""outputs"": [
      {
        ""internalType"": ""uint256"",
        ""name"": """",
        ""type"": ""uint256""
      }
    ],
    ""stateMutability"": ""view"",
    ""type"": ""function""
  },
  {
    ""inputs"": [],
    ""name"": ""totalSupply"",
    ""outputs"": [
      {
        ""internalType"": ""uint256"",
        ""name"": """",
        ""type"": ""uint256""
      }
    ],
    ""stateMutability"": ""view"",
    ""type"": ""function""
  },
  {
    ""inputs"": [
      {
        ""internalType"": ""address"",
        ""name"": ""recipient"",
        ""type"": ""address""
      },
      {
        ""internalType"": ""uint256"",
        ""name"": ""amount"",
        ""type"": ""uint256""
      }
    ],
    ""name"": ""transfer"",
    ""outputs"": [
      {
        ""internalType"": ""bool"",
        ""name"": """",
        ""type"": ""bool""
      }
    ],
    ""stateMutability"": ""nonpayable"",
    ""type"": ""function""
  },
  {
    ""inputs"": [
      {
        ""internalType"": ""address"",
        ""name"": ""sender"",
        ""type"": ""address""
      },
      {
        ""internalType"": ""address"",
        ""name"": ""recipient"",
        ""type"": ""address""
      },
      {
        ""internalType"": ""uint256"",
        ""name"": ""amount"",
        ""type"": ""uint256""
      }
    ],
    ""name"": ""transferFrom"",
    ""outputs"": [
      {
        ""internalType"": ""bool"",
        ""name"": """",
        ""type"": ""bool""
      }
    ],
    ""stateMutability"": ""nonpayable"",
    ""type"": ""function""
  }
]";

    public static void Map(WebApplication app)
    {
        app.MapPost("/contracts/erc20", async (
            DeployErc20ContractRequest request,
            [FromServices] IOptions<Web3Options> web3Options) =>
        {
            var account = new Account(request.PrivateKey);

            var web3 = new Web3(account, web3Options.Value.Url);

            var deploymentMessage = new SimpleTokenDeployment
            {
                TotalSupply = 1_000,
            };

            var deploymentHandler = web3.Eth.GetContractDeploymentHandler<SimpleTokenDeployment>();
            var transactionHash = await deploymentHandler.SendRequestAsync(deploymentMessage);

            return new {transactionHash};
        });

        app.MapPost("/contracts/erc20/{contractAddress}/balance", async (
            string contractAddress,
            GetErc20BalanceRequest request,
            [FromServices] Web3 web3) =>
        {
            var balanceOfFunctionMessage = new BalanceOfFunction()
            {
                Owner = request.Address,
            };

            var balanceHandler = web3.Eth.GetContractQueryHandler<BalanceOfFunction>();
            var balance = await balanceHandler.QueryAsync<BigInteger>(contractAddress, balanceOfFunctionMessage);

            var balanceInTokens = Web3.Convert.FromWei(balance, 6);

            return new {balanceInTokens};
        });

        app.MapPost("/contracts/erc20/{contractAddress}/transfer", async (
            string contractAddress,
            TransferErc20Request request,
            [FromServices] IOptions<Web3Options> web3Options) =>
        {
            var account = new Account(request.PrivateKey);
            var web3 = new Web3(account, web3Options.Value.Url);

            var transferHandler = web3.Eth.GetContractTransactionHandler<TransferFunction>();
            var transfer = new TransferFunction
            {
                To = request.To,
                TokenAmount = Web3.Convert.ToWei(request.Amount, 6),
            };

            var transactionHash = await transferHandler.SendRequestAsync(contractAddress, transfer);

            return new {transactionHash};
        });

        app.MapGet("/contracts/erc20/{contractAddress}/transfers", async (
            string contractAddress,
            [FromServices] Web3 web3) =>
        {
            var contract = web3.Eth.GetContract(Abi, contractAddress);

            var transferEvent = contract.GetEvent<TransferEventDTO>();

            var filterAllTransferEventsForContract = transferEvent.CreateFilterInput(new BlockParameter(6250000),
              new BlockParameter(6266000));

            var transferEvents = await transferEvent.GetAllChangesAsync(filterAllTransferEventsForContract);

            return new {transfers = transferEvents.Select(x => new
            {
                From = x.Event.From,
                To = x.Event.To,
                Amount = Web3.Convert.FromWei(x.Event.Value, 6),
            })};
        });
    }

    public record DeployErc20ContractRequest(string PrivateKey);

    public record GetErc20BalanceRequest(string Address);

    public record TransferErc20Request(string PrivateKey, string To, decimal Amount);

    public class SimpleTokenDeployment : ContractDeploymentMessage
    {
        private static string ByteCode = "608060405234801561000f575f80fd5b506040516112143803806112148339818101604052810190610031919061013a565b601260ff16600a61004291906102c1565b8161004d919061030b565b6002819055506002545f803373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019081526020015f20819055503373ffffffffffffffffffffffffffffffffffffffff165f73ffffffffffffffffffffffffffffffffffffffff167fddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef6002546040516100f5919061035b565b60405180910390a350610374565b5f80fd5b5f819050919050565b61011981610107565b8114610123575f80fd5b50565b5f8151905061013481610110565b92915050565b5f6020828403121561014f5761014e610103565b5b5f61015c84828501610126565b91505092915050565b7f4e487b71000000000000000000000000000000000000000000000000000000005f52601160045260245ffd5b5f8160011c9050919050565b5f808291508390505b60018511156101e7578086048111156101c3576101c2610165565b5b60018516156101d25780820291505b80810290506101e085610192565b94506101a7565b94509492505050565b5f826101ff57600190506102ba565b8161020c575f90506102ba565b8160018114610222576002811461022c5761025b565b60019150506102ba565b60ff84111561023e5761023d610165565b5b8360020a91508482111561025557610254610165565b5b506102ba565b5060208310610133831016604e8410600b84101617156102905782820a90508381111561028b5761028a610165565b5b6102ba565b61029d848484600161019e565b925090508184048111156102b4576102b3610165565b5b81810290505b9392505050565b5f6102cb82610107565b91506102d683610107565b92506103037fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff84846101f0565b905092915050565b5f61031582610107565b915061032083610107565b925082820261032e81610107565b9150828204841483151761034557610344610165565b5b5092915050565b61035581610107565b82525050565b5f60208201905061036e5f83018461034c565b92915050565b610e93806103815f395ff3fe608060405234801561000f575f80fd5b5060043610610091575f3560e01c8063313ce56711610064578063313ce5671461013157806370a082311461014f57806395d89b411461017f578063a9059cbb1461019d578063dd62ed3e146101cd57610091565b806306fdde0314610095578063095ea7b3146100b357806318160ddd146100e357806323b872dd14610101575b5f80fd5b61009d6101fd565b6040516100aa9190610a5b565b60405180910390f35b6100cd60048036038101906100c89190610b0c565b610236565b6040516100da9190610b64565b60405180910390f35b6100eb610391565b6040516100f89190610b8c565b60405180910390f35b61011b60048036038101906101169190610ba5565b61039a565b6040516101289190610b64565b60405180910390f35b6101396106e5565b6040516101469190610c10565b60405180910390f35b61016960048036038101906101649190610c29565b6106ea565b6040516101769190610b8c565b60405180910390f35b61018761072f565b6040516101949190610a5b565b60405180910390f35b6101b760048036038101906101b29190610b0c565b610768565b6040516101c49190610b64565b60405180910390f35b6101e760048036038101906101e29190610c54565b610969565b6040516101f49190610b8c565b60405180910390f35b6040518060400160405280600b81526020017f53696d706c65546f6b656e00000000000000000000000000000000000000000081525081565b5f8073ffffffffffffffffffffffffffffffffffffffff168373ffffffffffffffffffffffffffffffffffffffff16036102a5576040517f08c379a000000000000000000000000000000000000000000000000000000000815260040161029c90610cdc565b60405180910390fd5b8160015f3373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019081526020015f205f8573ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019081526020015f20819055508273ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff167f8c5be1e5ebec7d5bd14f71427d1e84f3dd0314c0f7b2291e5b200ac8c7c3b9258460405161037f9190610b8c565b60405180910390a36001905092915050565b5f600254905090565b5f805f8573ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019081526020015f205482111561041a576040517f08c379a000000000000000000000000000000000000000000000000000000000815260040161041190610d44565b60405180910390fd5b60015f8573ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019081526020015f205f3373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019081526020015f20548211156104d5576040517f08c379a00000000000000000000000000000000000000000000000000000000081526004016104cc90610dac565b60405180910390fd5b5f73ffffffffffffffffffffffffffffffffffffffff168373ffffffffffffffffffffffffffffffffffffffff1603610543576040517f08c379a000000000000000000000000000000000000000000000000000000000815260040161053a90610cdc565b60405180910390fd5b815f808673ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019081526020015f205f82825461058e9190610df7565b92505081905550815f808573ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019081526020015f205f8282546105e09190610e2a565b925050819055508160015f8673ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019081526020015f205f3373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019081526020015f205f82825461066e9190610df7565b925050819055508273ffffffffffffffffffffffffffffffffffffffff168473ffffffffffffffffffffffffffffffffffffffff167fddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef846040516106d29190610b8c565b60405180910390a3600190509392505050565b601281565b5f805f8373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019081526020015f20549050919050565b6040518060400160405280600381526020017f53494d000000000000000000000000000000000000000000000000000000000081525081565b5f805f3373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019081526020015f20548211156107e8576040517f08c379a00000000000000000000000000000000000000000000000000000000081526004016107df90610d44565b60405180910390fd5b5f73ffffffffffffffffffffffffffffffffffffffff168373ffffffffffffffffffffffffffffffffffffffff1603610856576040517f08c379a000000000000000000000000000000000000000000000000000000000815260040161084d90610cdc565b60405180910390fd5b815f803373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019081526020015f205f8282546108a19190610df7565b92505081905550815f808573ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019081526020015f205f8282546108f39190610e2a565b925050819055508273ffffffffffffffffffffffffffffffffffffffff163373ffffffffffffffffffffffffffffffffffffffff167fddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef846040516109579190610b8c565b60405180910390a36001905092915050565b5f60015f8473ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019081526020015f205f8373ffffffffffffffffffffffffffffffffffffffff1673ffffffffffffffffffffffffffffffffffffffff1681526020019081526020015f2054905092915050565b5f81519050919050565b5f82825260208201905092915050565b8281835e5f83830152505050565b5f601f19601f8301169050919050565b5f610a2d826109eb565b610a3781856109f5565b9350610a47818560208601610a05565b610a5081610a13565b840191505092915050565b5f6020820190508181035f830152610a738184610a23565b905092915050565b5f80fd5b5f73ffffffffffffffffffffffffffffffffffffffff82169050919050565b5f610aa882610a7f565b9050919050565b610ab881610a9e565b8114610ac2575f80fd5b50565b5f81359050610ad381610aaf565b92915050565b5f819050919050565b610aeb81610ad9565b8114610af5575f80fd5b50565b5f81359050610b0681610ae2565b92915050565b5f8060408385031215610b2257610b21610a7b565b5b5f610b2f85828601610ac5565b9250506020610b4085828601610af8565b9150509250929050565b5f8115159050919050565b610b5e81610b4a565b82525050565b5f602082019050610b775f830184610b55565b92915050565b610b8681610ad9565b82525050565b5f602082019050610b9f5f830184610b7d565b92915050565b5f805f60608486031215610bbc57610bbb610a7b565b5b5f610bc986828701610ac5565b9350506020610bda86828701610ac5565b9250506040610beb86828701610af8565b9150509250925092565b5f60ff82169050919050565b610c0a81610bf5565b82525050565b5f602082019050610c235f830184610c01565b92915050565b5f60208284031215610c3e57610c3d610a7b565b5b5f610c4b84828501610ac5565b91505092915050565b5f8060408385031215610c6a57610c69610a7b565b5b5f610c7785828601610ac5565b9250506020610c8885828601610ac5565b9150509250929050565b7f496e76616c6964206164647265737300000000000000000000000000000000005f82015250565b5f610cc6600f836109f5565b9150610cd182610c92565b602082019050919050565b5f6020820190508181035f830152610cf381610cba565b9050919050565b7f496e73756666696369656e742062616c616e63650000000000000000000000005f82015250565b5f610d2e6014836109f5565b9150610d3982610cfa565b602082019050919050565b5f6020820190508181035f830152610d5b81610d22565b9050919050565b7f416c6c6f77616e636520657863656564656400000000000000000000000000005f82015250565b5f610d966012836109f5565b9150610da182610d62565b602082019050919050565b5f6020820190508181035f830152610dc381610d8a565b9050919050565b7f4e487b71000000000000000000000000000000000000000000000000000000005f52601160045260245ffd5b5f610e0182610ad9565b9150610e0c83610ad9565b9250828203905081811115610e2457610e23610dca565b5b92915050565b5f610e3482610ad9565b9150610e3f83610ad9565b9250828201905080821115610e5757610e56610dca565b5b9291505056fea2646970667358221220a90cc32e5f3435c59f609c0ec1efc479e5923e2f5fe4128dbe4189abe37ce25a64736f6c634300081a0033";

        public SimpleTokenDeployment() : base(ByteCode){}

        [Parameter("uint256", "totalSupply")]
        public BigInteger TotalSupply { get; set; }
    }

    [Function("balanceOf", "uint256")]
    public class BalanceOfFunction : FunctionMessage
    {
        [Parameter("address", "_owner", 1)]
        public string? Owner { get; set; }
    }

    [Function("transfer", "bool")]
    public class TransferFunction : FunctionMessage
    {
        [Parameter("address", "_to", 1)]
        public string? To { get; set; }

        [Parameter("uint256", "_value", 2)]
        public BigInteger TokenAmount { get; set; }
    }

    [Event("Transfer")]
    public class TransferEventDTO : IEventDTO
    {
        [Parameter("address", "from", 1, true)]
        public string From { get; set; }

        [Parameter("address", "to", 2, true)]
        public string To { get; set; }

        [Parameter("uint256", "value", 3, false)]
        public BigInteger Value { get; set; }
    }
}
