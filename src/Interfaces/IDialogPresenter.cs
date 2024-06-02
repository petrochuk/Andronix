namespace Andronix.Interfaces;

public interface IDialogPresenter
{
    void ShowDialog(string fullDialog);

    void UpdateStatus(string status);
}
