using QTKD.Api.Application;
using QTKD.Api.Domain;

namespace QTKD.Api.Tests;

[Trait("Category", "Unit")]
public sealed class ContractBusinessRulesTests
{
    [Theory]
    [InlineData(ContractStatuses.Draft)]
    [InlineData(ContractStatuses.RevisionRequested)]
    public void CanEdit_ReturnsTrueForEditableStatuses(string status)
    {
        Assert.True(ContractBusinessRules.CanEdit(status));
    }

    [Fact]
    public void CanEdit_ReturnsFalseForPendingApproval()
    {
        Assert.False(ContractBusinessRules.CanEdit(ContractStatuses.PendingApproval));
    }

    [Fact]
    public void GetFailedSubmissionConditions_ReturnsNoErrorsForValidContract()
    {
        var today = new DateOnly(2026, 11, 11);

        var errors = ContractBusinessRules.GetFailedSubmissionConditions(
            customerIsValid: true,
            startDate: today,
            endDate: today.AddDays(1),
            today,
            serviceTerms: "Port service terms");

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(false, 0, 1, "Port service terms", "Customer must exist and have Active status.")]
    [InlineData(true, 0, -1, "Port service terms", "Contract effective dates must be ordered and not expired.")]
    [InlineData(true, 0, 1, " ", "At least one required content item must be provided (demo assumption: ServiceTerms).")]
    public void GetFailedSubmissionConditions_ReturnsErrorForInvalidRequirements(
        bool customerIsValid,
        int startOffset,
        int endOffset,
        string? serviceTerms,
        string expectedError)
    {
        var today = new DateOnly(2026, 11, 11);

        var errors = ContractBusinessRules.GetFailedSubmissionConditions(
            customerIsValid,
            today.AddDays(startOffset),
            today.AddDays(endOffset),
            today,
            serviceTerms);

        Assert.Contains(expectedError, errors);
    }

    [Fact]
    public void CanSubmit_AllowsDraftAndRejectsOtherStatuses()
    {
        Assert.True(ContractBusinessRules.CanSubmit(ContractStatuses.Draft));
        Assert.False(ContractBusinessRules.CanSubmit(ContractStatuses.PendingApproval));
        Assert.False(ContractBusinessRules.CanSubmit(ContractStatuses.Approved));
    }

    [Fact]
    public void GetStatusAfterSubmission_ReturnsPendingApproval()
    {
        Assert.Equal(
            ContractStatuses.PendingApproval,
            ContractBusinessRules.GetStatusAfterSubmission());
    }

    [Fact]
    public void CanSetStatusDirectly_RejectsRequestedStatus()
    {
        Assert.True(ContractBusinessRules.CanSetStatusDirectly(null));
        Assert.False(ContractBusinessRules.CanSetStatusDirectly(ContractStatuses.Approved));
    }
}
