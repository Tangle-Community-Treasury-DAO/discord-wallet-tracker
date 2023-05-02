namespace Pathin.WalletWatcher.Services.TransactionParsers;

public record TransactionDisplayInfo(string Title, List<TransactionInfo> Transactions, string? Url, decimal GasUsed, string TransactionHash);