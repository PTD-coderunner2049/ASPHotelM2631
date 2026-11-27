using System.Net.Mail;
using ASPHotelM2631.Domain.Bookings;

namespace ASPHotelM2631.Application.Bookings;

public sealed class GuestService(IGuestStore guests)
{
    public async Task<GuestDetails> GetAsync(int id, CancellationToken cancellationToken) =>
        ToDetails(await GetGuestAsync(id, cancellationToken));

    public Task<Page<GuestDetails>> SearchAsync(string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        BookingService.ValidatePage(page, pageSize);
        return guests.FindGuestsAsync(search?.Trim(), page, pageSize, cancellationToken);
    }

    public async Task<GuestDetails> CreateAsync(GuestInput input, CancellationToken cancellationToken)
    {
        var guest = new GuestProfile();
        SetContact(guest, input);
        guests.Add(guest);
        await guests.SaveAsync(cancellationToken);
        return ToDetails(guest);
    }

    public async Task<GuestDetails> UpdateAsync(int id, GuestInput input, CancellationToken cancellationToken)
    {
        var guest = await GetGuestAsync(id, cancellationToken);
        SetContact(guest, input);
        await guests.SaveAsync(cancellationToken);
        return ToDetails(guest);
    }

    internal async Task<GuestProfile> GetGuestAsync(int id, CancellationToken cancellationToken) =>
        await guests.FindGuestAsync(id, cancellationToken)
        ?? throw new BookingException(BookingFailure.NotFound, "Không tìm thấy hồ sơ khách.");

    internal static GuestDetails ToDetails(GuestProfile guest) =>
        new(guest.Id, guest.FullName, guest.Phone, guest.Email);

    private static void SetContact(GuestProfile guest, GuestInput input)
    {
        var name = input.FullName?.Trim();
        var phone = input.Phone?.Trim();
        var email = input.Email?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 100)
            throw new BookingException(BookingFailure.Invalid, "Họ tên phải có từ 1 đến 100 ký tự.");
        if (string.IsNullOrEmpty(phone) || phone.Length > 20 || !phone.Any(char.IsAsciiDigit) ||
            phone.Any(character => !char.IsAsciiDigit(character) && character is not ('+' or ' ' or '(' or ')' or '-')))
            throw new BookingException(BookingFailure.Invalid, "Số điện thoại không hợp lệ.");
        if (!string.IsNullOrEmpty(email) &&
            (email.Length > 150 || !MailAddress.TryCreate(email, out var address) || address.Address != email))
            throw new BookingException(BookingFailure.Invalid, "Email không hợp lệ.");

        guest.FullName = name;
        guest.Phone = phone;
        guest.Email = string.IsNullOrEmpty(email) ? null : email;
    }
}
