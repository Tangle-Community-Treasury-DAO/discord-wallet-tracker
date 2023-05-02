namespace Pathin.WalletWatcher.Services.TransactionParsers;

public record TransactionInfo(TransactionType Type, string PartyAddress, decimal Value, string Symbol, decimal NewTotal, string? AdditionalInfo = null);

public enum TransactionType{
    Send,
    Receive,
}