using FluentValidation;
using HotelPro.Application.DTOs.Operations;

namespace HotelPro.Application.Validators;

public class CheckInValidator : AbstractValidator<CheckInRequest>
{
    public CheckInValidator()
    {
        RuleFor(x => x.ReservationId).NotEmpty();
        RuleFor(x => x.ActualAdults).GreaterThan(0);
        RuleFor(x => x.ActualChildren).GreaterThanOrEqualTo(0);
    }
}

public class SaveGuestServiceValidator : AbstractValidator<SaveGuestServiceRequest>
{
    public SaveGuestServiceValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}

public class CreateServiceOrderValidator : AbstractValidator<CreateServiceOrderRequest>
{
    public CreateServiceOrderValidator()
    {
        RuleFor(x => x.ReservationId).NotEmpty();
        RuleFor(x => x.RoomId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(i =>
        {
            i.RuleFor(x => x.GuestServiceId).NotEmpty();
            i.RuleFor(x => x.Quantity).GreaterThan(0);
        });
    }
}

public class CreateMaintenanceValidator : AbstractValidator<CreateMaintenanceRequest>
{
    public CreateMaintenanceValidator()
    {
        RuleFor(x => x.IssueType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
    }
}

