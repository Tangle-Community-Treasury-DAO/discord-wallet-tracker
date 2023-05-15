// <copyright file="BatchRpcClient.cs" company="palow UG (haftungsbeschränkt) and palow GmbH">
// Copyright (c) palow UG (haftungsbeschränkt) and palow GmbH 2023. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using RestSharp;

namespace Pathin.WalletWatcher.Services;

/// <summary>
/// The BatchRpcClient class provides methods to send batch requests to an Ethereum JSON-RPC API.
/// </summary>
public class BatchRpcClient
{
    private readonly RestClient _client;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchRpcClient"/> class.
    /// </summary>
    /// <param name="rpcUrl">The EVM RPC Url.</param>
    /// <param name="logger">The logger instance.</param>
    public BatchRpcClient(string rpcUrl, ILogger logger)
    {
        _client = new RestClient(rpcUrl);
        _logger = logger;
    }

    /// <summary>
    /// Sends a batch request to the EVM to retrieve the transaction receipts for the specified transaction hashes.
    /// </summary>
    /// <param name="transactionHashes">The list of transaction hashes to retrieve the transaction receipts for.</param>
    /// <returns>A list of Transaction Receipts.</returns>
    /// <exception cref="RpcClientUnknownException">Thrown when the RPC request encounters any error.</exception>
    public async Task<List<TransactionReceipt?>> GetTransactionReceiptsAsync(IEnumerable<string> transactionHashes)
    {
        const int maxBatchSize = 100;
        var allTransactionHashes = transactionHashes.ToList();
        var totalTransactions = allTransactionHashes.Count;
        var receipts = new List<TransactionReceipt?>();

        for (int i = 0; i < totalTransactions; i += maxBatchSize)
        {
            var batchTransactionHashes = allTransactionHashes.Skip(i).Take(maxBatchSize);
            var batchRequests = batchTransactionHashes
                .Select((hash, index) => new
                {
                    jsonrpc = "2.0",
                    id = i + index,
                    method = "eth_getTransactionReceipt",
                    @params = new[] { hash },
                })
                .ToList();

            var request = new RestRequest()
            {
                Method = Method.Post,
                RequestFormat = DataFormat.Json,
            };
            request.AddParameter("application/json", JsonSerializer.Serialize(batchRequests), ParameterType.RequestBody);

            var response = await _client.ExecuteAsync(request);

            List<JsonElement> batchResponses = new();
            if (response.IsSuccessful)
            {
                if (response.Content != null)
                {
                    batchResponses = JsonSerializer.Deserialize<List<JsonElement>>(response.Content) ?? throw new RpcClientUnknownException("Error in BatchRpcClient", new Exception(response.ErrorMessage));
                }
                else
                {
                    _logger.LogError("Error in BatchRpcClient: {ErrorMessage}", response.ErrorMessage);
                    throw new RpcClientUnknownException("Error in BatchRpcClient", new Exception(response.ErrorMessage));
                }

                foreach (var element in batchResponses)
                {
                    var result = element.GetProperty("result").GetRawText();

                    // There may be 'null' results for transactions that have not been mined yet
                    if (result != "null")
                    {
                        var receipt = Newtonsoft.Json.JsonConvert.DeserializeObject<TransactionReceipt>(result);
                        receipts.Add(receipt);
                    }
                }
            }
            else
            {
                _logger.LogError("Error in BatchRpcClient: {ErrorMessage}", response.ErrorMessage);
                throw new RpcClientUnknownException("Error in BatchRpcClient", new Exception(response.ErrorMessage));
            }
        }

        return receipts;
    }
}