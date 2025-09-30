namespace Expense.Api.Models;

public enum UserRole
{
    Employee = 0,
    Manager = 1,
    Finance = 2
}

public enum RequestStatus
{
    Draft = 0,
    Submitted = 1,
    ManagerApproved = 2,
    FinanceProcessing = 3,
    PartiallyApproved = 4,
    FullyApproved = 5,
    Rejected = 6,
    Paid = 7
}

public enum ItemStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    ClarificationRequested = 3,
    Paid = 4
}

public enum ExpenseCategory
{
    Meal = 0,
    Accommodation = 1,
    Transport = 2,
    Other = 3
}

