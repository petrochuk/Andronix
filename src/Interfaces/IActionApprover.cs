namespace Andronix.Interfaces;

public interface IActionApprover
{
    /// <summary>
    /// Approves an action.
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    Task<(bool isApproved, string declineReason)> ApproveAction(string action);
}
