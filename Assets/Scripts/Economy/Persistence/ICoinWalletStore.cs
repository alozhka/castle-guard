namespace CastleGuard.Economy.Persistence;

public interface ICoinWalletStore
{
    int Load();
    void Save(int balance);
}
