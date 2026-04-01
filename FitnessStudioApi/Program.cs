using System.Text.Json.Serialization;
using FitnessStudioApi.Data;
using FitnessStudioApi.DTOs;
using FitnessStudioApi.Middleware;
using FitnessStudioApi.Models;
using FitnessStudioApi.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<FitnessDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddScoped<IMembershipPlanService, MembershipPlanService>();
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<IMembershipService, MembershipService>();
builder.Services.AddScoped<IInstructorService, InstructorService>();
builder.Services.AddScoped<IClassTypeService, ClassTypeService>();
builder.Services.AddScoped<IClassScheduleService, ClassScheduleService>();
builder.Services.AddScoped<IBookingService, BookingService>();

// OpenAPI
builder.Services.AddOpenApi();

// JSON enum serialization as strings
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Error handling
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Middleware
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Seed database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FitnessDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DataSeeder.SeedAsync(db);
}

// ==================== Membership Plans ====================
var plansGroup = app.MapGroup("/api/membership-plans").WithTags("Membership Plans");

plansGroup.MapGet("/", async (IMembershipPlanService service, CancellationToken ct) =>
    TypedResults.Ok(await service.GetAllActiveAsync(ct)))
    .WithName("GetMembershipPlans")
    .WithSummary("List all active membership plans")
    .Produces<IReadOnlyList<MembershipPlanResponse>>();

plansGroup.MapGet("/{id:int}", async Task<Results<Ok<MembershipPlanResponse>, NotFound>> (
    int id, IMembershipPlanService service, CancellationToken ct) =>
{
    var plan = await service.GetByIdAsync(id, ct);
    return plan is null ? TypedResults.NotFound() : TypedResults.Ok(plan);
})
    .WithName("GetMembershipPlanById")
    .WithSummary("Get a membership plan by ID");

plansGroup.MapPost("/", async (CreateMembershipPlanRequest request, IMembershipPlanService service, CancellationToken ct) =>
{
    var plan = await service.CreateAsync(request, ct);
    return TypedResults.Created($"/api/membership-plans/{plan.Id}", plan);
})
    .WithName("CreateMembershipPlan")
    .WithSummary("Create a new membership plan");

plansGroup.MapPut("/{id:int}", async Task<Results<Ok<MembershipPlanResponse>, NotFound>> (
    int id, UpdateMembershipPlanRequest request, IMembershipPlanService service, CancellationToken ct) =>
{
    var plan = await service.UpdateAsync(id, request, ct);
    return TypedResults.Ok(plan);
})
    .WithName("UpdateMembershipPlan")
    .WithSummary("Update a membership plan");

plansGroup.MapDelete("/{id:int}", async (int id, IMembershipPlanService service, CancellationToken ct) =>
{
    await service.DeactivateAsync(id, ct);
    return TypedResults.NoContent();
})
    .WithName("DeactivateMembershipPlan")
    .WithSummary("Deactivate a membership plan");

// ==================== Members ====================
var membersGroup = app.MapGroup("/api/members").WithTags("Members");

membersGroup.MapGet("/", async (string? search, bool? isActive, int? page, int? pageSize,
    IMemberService service, CancellationToken ct) =>
    TypedResults.Ok(await service.GetAllAsync(search, isActive, page ?? 1, pageSize ?? 10, ct)))
    .WithName("GetMembers")
    .WithSummary("List members with optional search and filtering");

membersGroup.MapGet("/{id:int}", async Task<Results<Ok<MemberResponse>, NotFound>> (
    int id, IMemberService service, CancellationToken ct) =>
{
    var member = await service.GetByIdAsync(id, ct);
    return member is null ? TypedResults.NotFound() : TypedResults.Ok(member);
})
    .WithName("GetMemberById")
    .WithSummary("Get member details including active membership");

membersGroup.MapPost("/", async (CreateMemberRequest request, IMemberService service, CancellationToken ct) =>
{
    var member = await service.CreateAsync(request, ct);
    return TypedResults.Created($"/api/members/{member.Id}", member);
})
    .WithName("CreateMember")
    .WithSummary("Register a new member");

membersGroup.MapPut("/{id:int}", async (int id, UpdateMemberRequest request, IMemberService service, CancellationToken ct) =>
    TypedResults.Ok(await service.UpdateAsync(id, request, ct)))
    .WithName("UpdateMember")
    .WithSummary("Update a member profile");

membersGroup.MapDelete("/{id:int}", async (int id, IMemberService service, CancellationToken ct) =>
{
    await service.DeactivateAsync(id, ct);
    return TypedResults.NoContent();
})
    .WithName("DeactivateMember")
    .WithSummary("Deactivate a member");

membersGroup.MapGet("/{id:int}/bookings", async (int id, string? status, DateOnly? fromDate, DateOnly? toDate,
    int? page, int? pageSize, IMemberService service, CancellationToken ct) =>
    TypedResults.Ok(await service.GetBookingsAsync(id, status, fromDate, toDate, page ?? 1, pageSize ?? 10, ct)))
    .WithName("GetMemberBookings")
    .WithSummary("Get a member's bookings with optional filters");

membersGroup.MapGet("/{id:int}/bookings/upcoming", async (int id, IMemberService service, CancellationToken ct) =>
    TypedResults.Ok(await service.GetUpcomingBookingsAsync(id, ct)))
    .WithName("GetMemberUpcomingBookings")
    .WithSummary("Get a member's upcoming confirmed bookings");

membersGroup.MapGet("/{id:int}/memberships", async (int id, IMemberService service, CancellationToken ct) =>
    TypedResults.Ok(await service.GetMembershipsAsync(id, ct)))
    .WithName("GetMemberMemberships")
    .WithSummary("Get membership history for a member");

// ==================== Memberships ====================
var membershipsGroup = app.MapGroup("/api/memberships").WithTags("Memberships");

membershipsGroup.MapPost("/", async (CreateMembershipRequest request, IMembershipService service, CancellationToken ct) =>
{
    var membership = await service.CreateAsync(request, ct);
    return TypedResults.Created($"/api/memberships/{membership.Id}", membership);
})
    .WithName("CreateMembership")
    .WithSummary("Purchase/create a membership for a member");

membershipsGroup.MapGet("/{id:int}", async Task<Results<Ok<MembershipResponse>, NotFound>> (
    int id, IMembershipService service, CancellationToken ct) =>
{
    var membership = await service.GetByIdAsync(id, ct);
    return membership is null ? TypedResults.NotFound() : TypedResults.Ok(membership);
})
    .WithName("GetMembershipById")
    .WithSummary("Get membership details");

membershipsGroup.MapPost("/{id:int}/cancel", async (int id, IMembershipService service, CancellationToken ct) =>
    TypedResults.Ok(await service.CancelAsync(id, ct)))
    .WithName("CancelMembership")
    .WithSummary("Cancel a membership");

membershipsGroup.MapPost("/{id:int}/freeze", async (int id, FreezeMembershipRequest request, IMembershipService service, CancellationToken ct) =>
    TypedResults.Ok(await service.FreezeAsync(id, request, ct)))
    .WithName("FreezeMembership")
    .WithSummary("Freeze a membership for a specified duration");

membershipsGroup.MapPost("/{id:int}/unfreeze", async (int id, IMembershipService service, CancellationToken ct) =>
    TypedResults.Ok(await service.UnfreezeAsync(id, ct)))
    .WithName("UnfreezeMembership")
    .WithSummary("Unfreeze a membership and extend end date");

membershipsGroup.MapPost("/{id:int}/renew", async (int id, IMembershipService service, CancellationToken ct) =>
{
    var membership = await service.RenewAsync(id, ct);
    return TypedResults.Created($"/api/memberships/{membership.Id}", membership);
})
    .WithName("RenewMembership")
    .WithSummary("Renew an expired membership");

// ==================== Instructors ====================
var instructorsGroup = app.MapGroup("/api/instructors").WithTags("Instructors");

instructorsGroup.MapGet("/", async (string? specialization, bool? isActive,
    IInstructorService service, CancellationToken ct) =>
    TypedResults.Ok(await service.GetAllAsync(specialization, isActive, ct)))
    .WithName("GetInstructors")
    .WithSummary("List instructors with optional filters");

instructorsGroup.MapGet("/{id:int}", async Task<Results<Ok<InstructorResponse>, NotFound>> (
    int id, IInstructorService service, CancellationToken ct) =>
{
    var instructor = await service.GetByIdAsync(id, ct);
    return instructor is null ? TypedResults.NotFound() : TypedResults.Ok(instructor);
})
    .WithName("GetInstructorById")
    .WithSummary("Get instructor details");

instructorsGroup.MapPost("/", async (CreateInstructorRequest request, IInstructorService service, CancellationToken ct) =>
{
    var instructor = await service.CreateAsync(request, ct);
    return TypedResults.Created($"/api/instructors/{instructor.Id}", instructor);
})
    .WithName("CreateInstructor")
    .WithSummary("Create a new instructor");

instructorsGroup.MapPut("/{id:int}", async (int id, UpdateInstructorRequest request, IInstructorService service, CancellationToken ct) =>
    TypedResults.Ok(await service.UpdateAsync(id, request, ct)))
    .WithName("UpdateInstructor")
    .WithSummary("Update an instructor");

instructorsGroup.MapGet("/{id:int}/schedule", async (int id, DateOnly? fromDate, DateOnly? toDate,
    IInstructorService service, CancellationToken ct) =>
    TypedResults.Ok(await service.GetScheduleAsync(id, fromDate, toDate, ct)))
    .WithName("GetInstructorSchedule")
    .WithSummary("Get an instructor's class schedule");

// ==================== Class Types ====================
var classTypesGroup = app.MapGroup("/api/class-types").WithTags("Class Types");

classTypesGroup.MapGet("/", async (DifficultyLevel? difficulty, bool? isPremium,
    IClassTypeService service, CancellationToken ct) =>
    TypedResults.Ok(await service.GetAllAsync(difficulty, isPremium, ct)))
    .WithName("GetClassTypes")
    .WithSummary("List class types with optional filters");

classTypesGroup.MapGet("/{id:int}", async Task<Results<Ok<ClassTypeResponse>, NotFound>> (
    int id, IClassTypeService service, CancellationToken ct) =>
{
    var classType = await service.GetByIdAsync(id, ct);
    return classType is null ? TypedResults.NotFound() : TypedResults.Ok(classType);
})
    .WithName("GetClassTypeById")
    .WithSummary("Get class type details");

classTypesGroup.MapPost("/", async (CreateClassTypeRequest request, IClassTypeService service, CancellationToken ct) =>
{
    var classType = await service.CreateAsync(request, ct);
    return TypedResults.Created($"/api/class-types/{classType.Id}", classType);
})
    .WithName("CreateClassType")
    .WithSummary("Create a new class type");

classTypesGroup.MapPut("/{id:int}", async (int id, UpdateClassTypeRequest request, IClassTypeService service, CancellationToken ct) =>
    TypedResults.Ok(await service.UpdateAsync(id, request, ct)))
    .WithName("UpdateClassType")
    .WithSummary("Update a class type");

// ==================== Class Schedules ====================
var classesGroup = app.MapGroup("/api/classes").WithTags("Class Schedules");

classesGroup.MapGet("/", async (DateOnly? fromDate, DateOnly? toDate, int? classTypeId, int? instructorId,
    bool? hasAvailability, int? page, int? pageSize,
    IClassScheduleService service, CancellationToken ct) =>
    TypedResults.Ok(await service.GetAllAsync(fromDate, toDate, classTypeId, instructorId, hasAvailability, page ?? 1, pageSize ?? 10, ct)))
    .WithName("GetClasses")
    .WithSummary("List scheduled classes with optional filters and pagination");

classesGroup.MapGet("/{id:int}", async Task<Results<Ok<ClassScheduleResponse>, NotFound>> (
    int id, IClassScheduleService service, CancellationToken ct) =>
{
    var schedule = await service.GetByIdAsync(id, ct);
    return schedule is null ? TypedResults.NotFound() : TypedResults.Ok(schedule);
})
    .WithName("GetClassById")
    .WithSummary("Get class details including enrollment and availability");

classesGroup.MapPost("/", async (CreateClassScheduleRequest request, IClassScheduleService service, CancellationToken ct) =>
{
    var schedule = await service.CreateAsync(request, ct);
    return TypedResults.Created($"/api/classes/{schedule.Id}", schedule);
})
    .WithName("CreateClass")
    .WithSummary("Schedule a new class");

classesGroup.MapPut("/{id:int}", async (int id, UpdateClassScheduleRequest request, IClassScheduleService service, CancellationToken ct) =>
    TypedResults.Ok(await service.UpdateAsync(id, request, ct)))
    .WithName("UpdateClass")
    .WithSummary("Update class details");

classesGroup.MapPatch("/{id:int}/cancel", async (int id, CancelClassRequest request, IClassScheduleService service, CancellationToken ct) =>
    TypedResults.Ok(await service.CancelAsync(id, request, ct)))
    .WithName("CancelClass")
    .WithSummary("Cancel a class and all its bookings");

classesGroup.MapGet("/{id:int}/roster", async (int id, IClassScheduleService service, CancellationToken ct) =>
    TypedResults.Ok(await service.GetRosterAsync(id, ct)))
    .WithName("GetClassRoster")
    .WithSummary("Get the list of confirmed/attended members for a class");

classesGroup.MapGet("/{id:int}/waitlist", async (int id, IClassScheduleService service, CancellationToken ct) =>
    TypedResults.Ok(await service.GetWaitlistAsync(id, ct)))
    .WithName("GetClassWaitlist")
    .WithSummary("Get the waitlist for a class");

classesGroup.MapGet("/available", async (IClassScheduleService service, CancellationToken ct) =>
    TypedResults.Ok(await service.GetAvailableAsync(ct)))
    .WithName("GetAvailableClasses")
    .WithSummary("Get classes with available spots in the next 7 days");

// ==================== Bookings ====================
var bookingsGroup = app.MapGroup("/api/bookings").WithTags("Bookings");

bookingsGroup.MapPost("/", async (CreateBookingRequest request, IBookingService service, CancellationToken ct) =>
{
    var booking = await service.CreateAsync(request, ct);
    return TypedResults.Created($"/api/bookings/{booking.Id}", booking);
})
    .WithName("CreateBooking")
    .WithSummary("Book a class (enforces all booking rules)");

bookingsGroup.MapGet("/{id:int}", async Task<Results<Ok<BookingResponse>, NotFound>> (
    int id, IBookingService service, CancellationToken ct) =>
{
    var booking = await service.GetByIdAsync(id, ct);
    return booking is null ? TypedResults.NotFound() : TypedResults.Ok(booking);
})
    .WithName("GetBookingById")
    .WithSummary("Get booking details");

bookingsGroup.MapPost("/{id:int}/cancel", async (int id, CancelBookingRequest request, IBookingService service, CancellationToken ct) =>
    TypedResults.Ok(await service.CancelAsync(id, request, ct)))
    .WithName("CancelBooking")
    .WithSummary("Cancel a booking (promotes waitlist if applicable)");

bookingsGroup.MapPost("/{id:int}/check-in", async (int id, IBookingService service, CancellationToken ct) =>
    TypedResults.Ok(await service.CheckInAsync(id, ct)))
    .WithName("CheckInBooking")
    .WithSummary("Check in for a class");

bookingsGroup.MapPost("/{id:int}/no-show", async (int id, IBookingService service, CancellationToken ct) =>
    TypedResults.Ok(await service.NoShowAsync(id, ct)))
    .WithName("NoShowBooking")
    .WithSummary("Mark a booking as no-show");

app.Run();
