namespace LethalCredit.Manager.Saves;

internal class GameSaveData
{
    public int BankBalance { get; set; }

    internal void ResetGameState()
    {
        BankBalance = 0;
    }
}