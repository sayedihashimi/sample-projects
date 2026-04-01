using FitnessStudioApi.DTOs;

namespace FitnessStudioApi.Services;

public interface IBookingService
{
    Task<BookingResponse> CreateAsync(CreateBookingRequest request, CancellationToken ct);
    Task<BookingResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<BookingResponse> CancelAsync(int id, CancelBookingRequest request, CancellationToken ct);
    Task<BookingResponse> CheckInAsync(int id, CancellationToken ct);
    Task<BookingResponse> NoShowAsync(int id, CancellationToken ct);
}
