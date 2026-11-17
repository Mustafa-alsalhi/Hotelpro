using FluentValidation;
using HotelPro.Application.DTOs.Rates;
using HotelPro.Application.DTOs.Reservations;

namespace HotelPro.Application.Validators;

public class SaveRatePlanValidator : AbstractValidator<SaveRatePlanRequest>
{
    public SaveRatePlanValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.WeekendMultiplier).InclusiveBetween(0.5m, 5.0m);
        RuleFor(x => x.LongStayMinNights).GreaterThan(0);
        RuleFor(x => x.LongStayDiscountPercent).InclusiveBetween(0, 80);
    }
}

public class SaveRoomRateValidator : AbstractValidator<SaveRoomRateRequest>
{
    public SaveRoomRateValidator()
    {
        RuleFor(x => x.RoomTypeId).NotEmpty();
        RuleFor(x => x.RatePlanId).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate);
        RuleFor(x => x.PricePerNight).GreaterThanOrEqualTo(0);
    }
}

public class CreateReservationValidator : AbstractValidator<CreateReservationRequest>
{
    public CreateReservationValidator()
    {
        RuleFor(x => x.PrimaryGuestId).NotEmpty();
        RuleFor(x => x.RatePlanId).NotEmpty();
        RuleFor(x => x.CheckOutDate).GreaterThan(x => x.CheckInDate)
            .WithMessage("Check-out date must be after check-in date.");
        RuleFor(x => x.AdultsCount).GreaterThan(0);
        RuleFor(x => x.ChildrenCount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.RoomIds).NotEmpty().WithMessage("At least one room must be selected.");
        RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DepositRequired).GreaterThanOrEqualTo(0);
    }
}

public class UpdateReservationStayValidator : AbstractValidator<UpdateReservationStayRequest>
{
    public UpdateReservationStayValidator()
    {
        RuleFor(x => x.CheckOutDate).GreaterThan(x => x.CheckInDate);
        RuleFor(x => x.AdultsCount).GreaterThan(0);
        RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0);
    }
}

