using TuningShop.Api.Domain;

namespace TuningShop.Api.Contracts;

// Типи повідомлень програмного інтерфейсу (docs/api.md). Поля запитів допускають null,
// щоб відсутнє значення отримало зрозуміле повідомлення біля поля, а не помилку розбору (NFR-03).

public sealed record ApiError(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string>? Fields = null,
    IReadOnlyList<string>? Details = null);

// --- Вхід і працівники (FR-01) ---

public sealed record LoginRequest(string? Login, string? Password);

public sealed record CurrentUserDto(int Id, string FullName, string Login, Role Role);

public sealed record EmployeeDto(int Id, string FullName, Role Role);

// --- Клієнти й автомобілі (FR-02 – FR-05, FR-18) ---

public sealed record SaveClientRequest(string? FullName, string? Phone, string? Email);

public sealed record ClientDto(int Id, string FullName, string Phone, string? Email);

public sealed record ClientDetailsDto(
    int Id, string FullName, string Phone, string? Email,
    IReadOnlyList<CarDto> Cars,
    IReadOnlyList<OrderSummaryDto> Orders);

public sealed record SaveCarRequest(string? Make, string? Model, int? Year, string? Vin, string? PlateNumber);

public sealed record CarDto(int Id, int ClientId, string Make, string Model, int Year, string Vin, string PlateNumber);

public sealed record CarDetailsDto(
    CarDto Car,
    ClientDto Owner,
    int? ActiveOrderId,
    string? ActiveOrderNumber,
    IReadOnlyList<OrderSummaryDto> Orders);

// --- Замовлення та роботи (FR-06 – FR-09, FR-12 – FR-17, FR-19) ---

public sealed record WorkRequest(WorkCategory? Category, string? Description, decimal? EstimatedCost, int? MasterId);

public sealed record CreateOrderRequest(int? CarId, string? ClientRequest, IReadOnlyList<WorkRequest>? Works);

public sealed record AssignMasterRequest(int? MasterId);

public sealed record CancelOrderRequest(string? Reason);

public sealed record OrderSummaryDto(
    int Id, string Number, DateTime CreatedAt,
    OrderStatus Status, string StatusName,
    int CarId, string CarTitle, string PlateNumber,
    int ClientId, string ClientName,
    int DoneCount, int WorkCount, string Progress, bool AllWorksDone,
    DateTime? IssuedAt, string? CancelReason);

public sealed record PartDto(
    int Id, string Name, int Quantity, PartStatus Status, string StatusName,
    DateOnly? ExpectedAt, DateTime? ReceivedAt);

public sealed record WorkItemDto(
    int Id, WorkCategory Category, string Description, decimal EstimatedCost,
    WorkStatus Status, string StatusName, DateTime StatusChangedAt,
    int? MasterId, string? MasterName,
    bool AllPartsReceived, IReadOnlyList<PartDto> Parts);

public sealed record StatusChangeDto(
    DateTime ChangedAt, OrderStatus? FromStatus, string? FromName,
    OrderStatus ToStatus, string ToName, string EmployeeName, string? Comment);

public sealed record OrderDetailsDto(
    int Id, string Number, DateTime CreatedAt, string ClientRequest,
    OrderStatus Status, string StatusName,
    DateTime? IssuedAt, string? CancelReason,
    ClientDto Client, CarDto Car,
    IReadOnlyList<WorkItemDto> Works,
    int DoneCount, int WorkCount, string Progress, bool AllWorksDone,
    decimal TotalEstimate,
    bool CanEditWorks,
    IReadOnlyList<string> AllowedActions,
    IReadOnlyList<StatusChangeDto> History);

// --- Робоче місце майстра (FR-10, FR-11) ---

public sealed record MasterWorkDto(
    int WorkId, int OrderId, string OrderNumber, string CarTitle, string PlateNumber,
    WorkCategory Category, string Description,
    WorkStatus Status, string StatusName, DateTime StatusChangedAt,
    string OrderProgress, IReadOnlyList<PartDto> Parts);

// --- Деталі (FR-20) ---

public sealed record AddPartRequest(string? Name, int? Quantity, DateOnly? ExpectedAt);

// --- Відстеження клієнтом (FR-21) ---

public sealed record TrackingPartDto(string Name, int Quantity, PartStatus Status, string StatusName,
    DateOnly? ExpectedAt, DateTime? ReceivedAt);

public sealed record TrackingWorkDto(WorkCategory Category, string Description, WorkStatus Status, string StatusName,
    IReadOnlyList<TrackingPartDto> Parts);

public sealed record TrackingDto(
    string Number, string CarTitle, string PlateNumber,
    OrderStatus Status, string StatusName,
    int DoneCount, int WorkCount, string Progress,
    IReadOnlyList<TrackingWorkDto> Works);
