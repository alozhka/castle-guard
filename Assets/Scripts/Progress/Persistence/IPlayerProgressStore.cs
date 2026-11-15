namespace CastleGuard.Progress.Persistence;

public interface IPlayerProgressStore
{
    int LoadLevel();
    int LoadCoins();
    void SaveLevel(int levelNumber);
    void SaveCoins(int balance);
}
