using FluentValidation;
using HotelPro.Application.DTOs.HR;
using HotelPro.Application.DTOs.Inventory;

namespace HotelPro.Application.Validators;

public class SaveEmployeeValidator : AbstractValidator<SaveEmployeeRequest>
{
    public SaveEmployeeValidator()
    {
        RuleFor(x => x.EmployeeCode).NotEmpty().MaximumLength(30);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.JobTitle).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.BaseSalary).GreaterThanOrEqualTo(0);
    }
}

public class SaveInventoryItemValidator : AbstractValidator<SaveInventoryItemRequest>
{
    public SaveInventoryItemValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0);
        RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0);
    }
}

public class CreateStockTransactionValidator : AbstractValidator<CreateStockTransactionRequest>
{
    public CreateStockTransactionValidator()
    {
        RuleFor(x => x.InventoryItemId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

