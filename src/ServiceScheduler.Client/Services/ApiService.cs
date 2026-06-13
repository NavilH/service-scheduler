using System.Net.Http.Json;
using ServiceScheduler.Shared.DTOs;

namespace ServiceScheduler.Client.Services;

public class ApiService(HttpClient http)
{
    // Services
    public Task<List<ServiceItemDto>?> GetServicesAsync() =>
        http.GetFromJsonAsync<List<ServiceItemDto>>("api/services");

    public Task<List<ServiceItemDto>?> GetAllServicesAdminAsync() =>
        http.GetFromJsonAsync<List<ServiceItemDto>>("api/services?includeInactive=true");

    public Task<HttpResponseMessage> CreateServiceAsync(CreateServiceItemDto dto) =>
        http.PostAsJsonAsync("api/services", dto);

    public Task<HttpResponseMessage> UpdateServiceAsync(int id, UpdateServiceItemDto dto) =>
        http.PutAsJsonAsync($"api/services/{id}", dto);

    public Task<HttpResponseMessage> DeleteServiceAsync(int id) =>
        http.DeleteAsync($"api/services/{id}");

    // Slots
    public Task<List<AvailableSlotDto>?> GetAvailableSlotsAsync(int serviceId, DateOnly date, string timeZone = "UTC") =>
        http.GetFromJsonAsync<List<AvailableSlotDto>>($"api/slots?serviceId={serviceId}&date={date:yyyy-MM-dd}&timeZone={Uri.EscapeDataString(timeZone)}");

    // Appointments
    public Task<List<AppointmentDto>?> GetAppointmentsAsync() =>
        http.GetFromJsonAsync<List<AppointmentDto>>("api/appointments");

    public Task<HttpResponseMessage> BookAppointmentAsync(CreateAppointmentDto dto) =>
        http.PostAsJsonAsync("api/appointments", dto);

    public Task<HttpResponseMessage> UpdateAppointmentStatusAsync(int id, UpdateAppointmentStatusDto dto) =>
        http.PatchAsJsonAsync($"api/appointments/{id}/status", dto);

    // Availability
    public Task<List<AvailabilityWindowDto>?> GetAvailabilityAsync() =>
        http.GetFromJsonAsync<List<AvailabilityWindowDto>>("api/availability");

    public Task<HttpResponseMessage> CreateAvailabilityWindowAsync(CreateAvailabilityWindowDto dto) =>
        http.PostAsJsonAsync("api/availability", dto);

    public Task<HttpResponseMessage> DeleteAvailabilityWindowAsync(int id) =>
        http.DeleteAsync($"api/availability/{id}");

    // Auth
    public Task<HttpResponseMessage> LoginAsync(LoginDto dto) =>
        http.PostAsJsonAsync("api/auth/login", dto);
}
