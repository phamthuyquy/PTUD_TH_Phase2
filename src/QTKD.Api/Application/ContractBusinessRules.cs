using QTKD.Api.Domain;

namespace QTKD.Api.Application;

public static class ContractBusinessRules
{
    public static bool CanEdit(string status) =>
        status is ContractStatuses.Draft or ContractStatuses.RevisionRequested;

    public static bool CanSubmit(string status) =>
        status == ContractStatuses.Draft;

    public static bool CanSetStatusDirectly(string? requestedStatus) =>
        requestedStatus is null;

    public static string GetStatusAfterSubmission() =>
        ContractStatuses.PendingApproval;

    public static IReadOnlyList<string> GetFailedSubmissionConditions(
        bool customerIsValid,
        DateOnly startDate,
        DateOnly endDate,
        DateOnly today,
        string? serviceTerms)
    {
        var failedConditions = new List<string>();
        if (!customerIsValid)
        {
            failedConditions.Add("Customer must exist and have Active status.");
        }

        if (startDate > endDate || endDate < today)
        {
            failedConditions.Add("Contract effective dates must be ordered and not expired.");
        }

        if (string.IsNullOrWhiteSpace(serviceTerms))
        {
            failedConditions.Add(
                "At least one required content item must be provided (demo assumption: ServiceTerms).");
        }

        return failedConditions;
    }
}
