using FluentValidation;
using HotelPro.Application.DTOs.Guests;
using HotelPro.Application.DTOs.Hotel;
using HotelPro.Application.DTOs.Rooms;

namespace HotelPro.Application.Validators;

public class UpdateHotelSettingsValidator : AbstractValidator<UpdateHotelSettingsRequest>
{
    public UpdateHotelSettingsValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(40);
        RuleFor(x => x.CurrencyCode).NotEmpty().MaximumLength(10);
        RuleFor(x => x.CurrencySymbol).NotEmpty().MaximumLength(10);
        RuleFor(x => x.TaxRatePercent).InclusiveBetween(0, 100);
        RuleFor(x => x.ServiceChargePercent).InclusiveBetween(0, 100);
        RuleFor(x => x.CancellationFeePercent).InclusiveBetween(0, 100);
        RuleFor(x => x.CancellationPolicyHours).GreaterThanOrEqualTo(0);
    }
}

public class SaveRoomTypeValidator : AbstractValidator<SaveRoomTypeRequest>
{
    public SaveRoomTypeValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.BasePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxAdults).GreaterThan(0);
        RuleFor(x => x.MaxChildren).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BedCount).GreaterThan(0);
        RuleFor(x => x.AreaSqm).GreaterThan(0);
    }
}

public class SaveRoomValidator : AbstractValidator<SaveRoomRequest>
{
    public SaveRoomValidator()
    {
        RuleFor(x => x.RoomTypeId).NotEmpty();
        RuleFor(x => x.RoomNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Floor).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxCapacity).GreaterThan(0);
        RuleFor(x => x.BedCount).GreaterThan(0);
        RuleFor(x => x.BasePriceOverride).GreaterThanOrEqualTo(0).When(x => x.BasePriceOverride.HasValue);
    }
}

public class SaveGuestValidator : AbstractValidator<SaveGuestRequest>
{
    public SaveGuestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleForEach(x => x.Documents).ChildRules(doc =>
        {
            doc.RuleFor(d => d.DocumentNumber).NotEmpty().MaximumLength(60);
        });
    }
}

