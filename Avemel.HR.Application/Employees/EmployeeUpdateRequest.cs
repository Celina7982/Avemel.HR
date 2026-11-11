namespace Avemel.HR.Application.Employees;

public sealed record EmployeeUpdateRequest
{
    public int EmployeeId { get; init; }
    public string EmployeeNumber { get; init; } = "";
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
    public string Email { get; init; } = "";
    public int? DivisionId { get; init; }
    public string Phone { get; init; } = "";
    public DateOnly? DateOfBirth { get; init; }
    public string IdNumber { get; init; } = "";
    public string JobTitle { get; init; } = "";
    public string Department { get; init; } = "";
    public int? ManagerId { get; init; }
    public string Role { get; init; } = "STAFF";
    public string EmploymentType { get; init; } = "PERMANENT";
    public string PayBasis { get; init; } = "MONTHLY";
    public decimal BasicSalary { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? ProbationEndDate { get; init; }
    public string Status { get; init; } = "ACTIVE";
    public DateOnly? EndDate { get; init; }
    public int WorkDaysPerWeek { get; init; } = 5;
    public bool WorksRoster { get; init; }
    public decimal AnnualLeaveDays { get; init; }
    public IReadOnlyCollection<string> RestDays { get; init; } = Array.Empty<string>();
    public decimal OrdinaryHoursPerDay { get; init; }
    public decimal OrdinaryHoursPerWeek { get; init; }
    public string EmergencyContactName { get; init; } = "";
    public string EmergencyContactPhone { get; init; } = "";

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(EmployeeNumber))
            errors.Add("Employee number is required when editing an employee.");

        errors.AddRange(ToCreationRequest().Validate());
        return errors;
    }

    public EmployeeCreationRequest ToCreationRequest() => new()
    {
        EmployeeNumber = EmployeeNumber,
        FirstName = FirstName,
        LastName = LastName,
        Email = Email,
        DivisionId = DivisionId,
        Phone = Phone,
        DateOfBirth = DateOfBirth,
        IdNumber = IdNumber,
        JobTitle = JobTitle,
        Department = Department,
        ManagerId = ManagerId,
        Role = Role,
        EmploymentType = EmploymentType,
        PayBasis = PayBasis,
        BasicSalary = BasicSalary,
        StartDate = StartDate,
        ProbationEndDate = ProbationEndDate,
        Status = Status,
        EndDate = EndDate,
        WorkDaysPerWeek = WorkDaysPerWeek,
        WorksRoster = WorksRoster,
        AnnualLeaveDays = AnnualLeaveDays,
        RestDays = RestDays,
        OrdinaryHoursPerDay = OrdinaryHoursPerDay,
        OrdinaryHoursPerWeek = OrdinaryHoursPerWeek,
        EmergencyContactName = EmergencyContactName,
        EmergencyContactPhone = EmergencyContactPhone
    };
}
