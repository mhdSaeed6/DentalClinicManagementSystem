using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Invoices.Commands.GenerateAppointmentInvoice;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Invoices;
using DentalClinic.Domain.Invoices.Enums;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.Services;
using DentalClinic.Domain.TreatmentRecords;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Invoices.Commands.GenerateAppointmentInvoice;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GenerateAppointmentInvoiceCommandHandlerTests(WebAppFactory factory) : IDisposable
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    public void Dispose() => factory.CurrentUser.AsAdmin();

    [Fact]
    public async Task Handle_WithServiceAndTreatments_ShouldCreateSingleDraftWithCorrectTotal()
    {
        // Arrange
        var (patient, _, _, appointment) = await SeedCompletedAppointmentAsync(servicePrice: 30m);
        await AddTreatmentAsync(appointment, 11, "Composite filling", 50m);
        await AddTreatmentAsync(appointment, 36, "Extraction", 120m);

        // Act
        var result = await _mediator.Send(new GenerateAppointmentInvoiceCommand(appointment.Id));

        // Assert
        Assert.True(result.IsSuccess);
        var created = result.Value;

        Assert.False(created.ReusedExisting);
        Assert.Equal(30m, created.ServiceSubtotal);
        Assert.Equal(170m, created.TreatmentSubtotal);
        Assert.Equal(200m, created.TotalAmount);
        Assert.Equal(3, created.Lines.Count);
        Assert.Equal(patient.Id, created.PatientId);
        Assert.Equal(appointment.Id, created.AppointmentId);

        var dbInvoice = await _context.Invoices
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == created.InvoiceId);

        Assert.NotNull(dbInvoice);
        Assert.Equal(appointment.Id, dbInvoice!.AppointmentId);
        Assert.Equal(patient.Id, dbInvoice.PatientId);
        Assert.Equal(200m, dbInvoice.TotalAmount);
        Assert.Equal(PaymentStatus.Unpaid, dbInvoice.Status);
    }

    [Fact]
    public async Task Handle_WithoutService_ShouldInvoiceTreatmentsOnly()
    {
        // Arrange
        var (_, _, _, appointment) = await SeedCompletedAppointmentAsync(withService: false);
        await AddTreatmentAsync(appointment, 11, "Composite filling", 80m);

        // Act
        var result = await _mediator.Send(new GenerateAppointmentInvoiceCommand(appointment.Id));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value.ServiceSubtotal);
        Assert.Equal(80m, result.Value.TreatmentSubtotal);
        Assert.Equal(80m, result.Value.TotalAmount);
        Assert.Single(result.Value.Lines);
    }

    [Fact]
    public async Task Handle_WithZeroPricedService_ShouldBillTreatmentsOnly()
    {
        // Arrange – a free service is a real price, not a fabricated charge.
        var (_, _, _, appointment) = await SeedCompletedAppointmentAsync(servicePrice: 0m);
        await AddTreatmentAsync(appointment, 11, "Composite filling", 80m);

        // Act
        var result = await _mediator.Send(new GenerateAppointmentInvoiceCommand(appointment.Id));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value.ServiceSubtotal);
        Assert.Equal(80m, result.Value.TotalAmount);
        Assert.Single(result.Value.Lines);
    }

    [Fact]
    public async Task Handle_ConsultationOnly_ShouldNotCreateEmptyInvoice()
    {
        // Arrange – nothing billable at all.
        var (_, _, _, appointment) = await SeedCompletedAppointmentAsync(withService: false);

        // Act
        var result = await _mediator.Send(new GenerateAppointmentInvoiceCommand(appointment.Id));

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("Invoice.NoBillableItems", result.TopError.Code);
        Assert.False(await _context.Invoices.AnyAsync(i => i.AppointmentId == appointment.Id));
    }

    [Fact]
    public async Task Handle_WithFreeServiceAndNoTreatments_ShouldNotCreateEmptyInvoice()
    {
        // Arrange
        var (_, _, _, appointment) = await SeedCompletedAppointmentAsync(servicePrice: 0m);

        // Act
        var result = await _mediator.Send(new GenerateAppointmentInvoiceCommand(appointment.Id));

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("Invoice.NoBillableItems", result.TopError.Code);
        Assert.False(await _context.Invoices.AnyAsync(i => i.AppointmentId == appointment.Id));
    }

    [Fact]
    public async Task Handle_WithSoftDeletedService_ShouldReportMissingPriceInsteadOfGuessing()
    {
        // Arrange – the appointment points at a service that no longer resolves,
        // so its price cannot be trusted and must not be silently dropped.
        var (_, _, service, appointment) = await SeedCompletedAppointmentAsync(servicePrice: 30m);
        service.Delete();
        await _context.SaveChangesAsync(default);

        // Act
        var result = await _mediator.Send(new GenerateAppointmentInvoiceCommand(appointment.Id));

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("Service.NotFound", result.TopError.Code);
        Assert.False(await _context.Invoices.AnyAsync(i => i.AppointmentId == appointment.Id));
    }

    [Fact]
    public async Task Handle_WithScheduledAppointment_ShouldBeRejected()
    {
        // Arrange – billing a visit that never happened would be incorrect.
        var appointment = await SeedScheduledAppointmentAsync();

        // Act
        var result = await _mediator.Send(new GenerateAppointmentInvoiceCommand(appointment.Id));

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("Invoice.AppointmentNotCompleted", result.TopError.Code);
        Assert.False(await _context.Invoices.AnyAsync(i => i.AppointmentId == appointment.Id));
    }

    [Fact]
    public async Task Handle_WithUnknownAppointment_ShouldReturnAppointmentNotFound()
    {
        // Act
        var result = await _mediator.Send(new GenerateAppointmentInvoiceCommand(Guid.NewGuid()));

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("Appointment.NotFound", result.TopError.Code);
    }

    [Fact]
    public async Task Handle_AsUnrelatedDoctor_ShouldBeForbidden()
    {
        // Arrange – a doctor may only bill their own appointments.
        var (_, _, _, appointment) = await SeedCompletedAppointmentAsync(servicePrice: 30m);
        await AddTreatmentAsync(appointment, 11, "Composite filling", 50m);

        factory.CurrentUser.AsDoctor(Guid.NewGuid());

        // Act
        var result = await _mediator.Send(new GenerateAppointmentInvoiceCommand(appointment.Id));

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("Appointment.AccessForbidden", result.TopError.Code);
        Assert.False(await _context.Invoices.AnyAsync(i => i.AppointmentId == appointment.Id));
    }

    [Fact]
    public async Task Handle_AsAssignedDoctor_ShouldSucceed()
    {
        // Arrange
        var (_, doctor, _, appointment) = await SeedCompletedAppointmentAsync(servicePrice: 30m);
        await AddTreatmentAsync(appointment, 11, "Composite filling", 50m);

        factory.CurrentUser.AsDoctor(doctor.Id);

        // Act
        var result = await _mediator.Send(new GenerateAppointmentInvoiceCommand(appointment.Id));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(80m, result.Value.TotalAmount);
    }

    [Fact]
    public async Task Handle_OnlyIncludesTreatmentsLinkedToThisAppointment()
    {
        // Arrange – another visit's treatments for the same patient must never leak.
        var (patient, doctor, service, appointment) = await SeedCompletedAppointmentAsync(servicePrice: 30m);
        await AddTreatmentAsync(appointment, 11, "Composite filling", 50m);

        var otherAppointment = Appointment.Create(
            patient.Id, doctor.Id, service.Id, DateTime.UtcNow.AddHours(2), 30).Value;
        otherAppointment.Complete();

        await _context.Appointments.AddAsync(otherAppointment);
        await _context.SaveChangesAsync(default);
        await AddTreatmentAsync(otherAppointment, 36, "Extraction", 999m);

        // Act
        var result = await _mediator.Send(new GenerateAppointmentInvoiceCommand(appointment.Id));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(30m, result.Value.ServiceSubtotal);
        Assert.Equal(50m, result.Value.TreatmentSubtotal);
        Assert.Equal(80m, result.Value.TotalAmount);
        Assert.Equal(2, result.Value.Lines.Count);
        Assert.DoesNotContain(result.Value.Lines, line => line.Amount == 999m);
    }

    [Fact]
    public async Task Handle_RepeatedRequests_ShouldReturnExistingDraftWithoutDuplicating()
    {
        // Arrange
        var (_, _, _, appointment) = await SeedCompletedAppointmentAsync(servicePrice: 30m);
        await AddTreatmentAsync(appointment, 11, "Composite filling", 50m);

        // Act
        var first = await _mediator.Send(new GenerateAppointmentInvoiceCommand(appointment.Id));
        var second = await _mediator.Send(new GenerateAppointmentInvoiceCommand(appointment.Id));

        // Assert
        Assert.True(first.IsSuccess);
        Assert.False(first.Value.ReusedExisting);

        Assert.True(second.IsSuccess);
        Assert.True(second.Value.ReusedExisting);
        Assert.Equal(first.Value.InvoiceId, second.Value.InvoiceId);

        var count = await _context.Invoices
            .AsNoTracking()
            .CountAsync(i => i.AppointmentId == appointment.Id);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Handle_WhenSettledInvoiceExists_ShouldNotCreateOrModifyIt()
    {
        // Arrange – an invoice with a recorded payment is settled and must be left alone.
        var (patient, _, _, appointment) = await SeedCompletedAppointmentAsync(servicePrice: 30m);
        await AddTreatmentAsync(appointment, 11, "Composite filling", 50m);

        var existingInvoice = Invoice.Create(patient.Id, 100m, appointment.Id).Value;
        existingInvoice.RegisterPayment(40m);
        await _context.Invoices.AddAsync(existingInvoice);
        await _context.SaveChangesAsync(default);

        // Act
        var result = await _mediator.Send(new GenerateAppointmentInvoiceCommand(appointment.Id));

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("Invoice.ExistingForAppointment", result.TopError.Code);

        var dbInvoice = await _context.Invoices
            .AsNoTracking()
            .FirstAsync(i => i.Id == existingInvoice.Id);
        Assert.Equal(100m, dbInvoice.TotalAmount);
        Assert.Equal(40m, dbInvoice.PaidAmount);
        Assert.Equal(PaymentStatus.PartiallyPaid, dbInvoice.Status);

        var count = await _context.Invoices
            .AsNoTracking()
            .CountAsync(i => i.AppointmentId == appointment.Id);
        Assert.Equal(1, count);
    }

    // ---------------------------------------------------------------------
    // Seeding helpers
    // ---------------------------------------------------------------------

    private async Task<Appointment> SeedScheduledAppointmentAsync()
    {
        var (_, _, _, appointment) = await SeedAppointmentAsync(withService: true, servicePrice: 30m, complete: false);
        return appointment;
    }

    private Task<(Patient Patient, Doctor Doctor, DentalService Service, Appointment Appointment)>
        SeedCompletedAppointmentAsync(bool withService = true, decimal servicePrice = 30m) =>
        SeedAppointmentAsync(withService, servicePrice, complete: true);

    private async Task<(Patient Patient, Doctor Doctor, DentalService Service, Appointment Appointment)>
        SeedAppointmentAsync(bool withService, decimal servicePrice, bool complete)
    {
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;

        var patient = Patient.Create("Ahmad", "Kareem", contactInfo, DateTime.UtcNow.AddYears(-25), Gender.Male).Value;
        var doctor = Doctor.Create("Omar", "Ali", "Orthodontics", contactInfo, Gender.Male).Value;
        var service = DentalService.Create("Teeth Cleaning", "A routine cleaning.", servicePrice).Value;

        var appointment = Appointment.Create(
            patient.Id,
            doctor.Id,
            withService ? service.Id : null,
            DateTime.UtcNow.AddHours(1),
            30).Value;

        if (complete)
        {
            appointment.Complete();
        }

        await _context.Patients.AddAsync(patient);
        await _context.Doctors.AddAsync(doctor);
        await _context.DentalServices.AddAsync(service);
        await _context.Appointments.AddAsync(appointment);
        await _context.SaveChangesAsync(default);

        return (patient, doctor, service, appointment);
    }

    private async Task<TreatmentRecord> AddTreatmentAsync(
        Appointment appointment,
        int toothNumber,
        string procedureDetails,
        decimal cost)
    {
        var treatment = TreatmentRecord.Create(
            appointment.PatientId,
            appointment.DoctorId,
            toothNumber,
            procedureDetails,
            cost,
            appointment.Id).Value;

        await _context.TreatmentRecords.AddAsync(treatment);
        await _context.SaveChangesAsync(default);

        return treatment;
    }
}
