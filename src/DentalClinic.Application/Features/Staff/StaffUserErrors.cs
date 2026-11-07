using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Application.Features.Staff;

public static class StaffUserErrors
{
    public static Error ManageForbidden =>
        Error.Forbidden(
            code: "StaffUser.ManageForbidden",
            description: "Only administrators can manage staff accounts.");

    public static Error RoleNotAllowed =>
        Error.Validation(
            code: "StaffUser.RoleNotAllowed",
            description: "Only the Admin, Doctor and Receptionist roles can be assigned.");

    public static Error NoRolesSpecified =>
        Error.Validation(
            code: "StaffUser.NoRolesSpecified",
            description: "At least one role must be assigned to the account.");

    public static Error CannotModifySelf =>
        Error.Conflict(
            code: "StaffUser.CannotModifySelf",
            description: "You cannot change the roles or activation of your own account.");

    public static Error LastAdminProtected =>
        Error.Conflict(
            code: "StaffUser.LastAdminProtected",
            description: "At least one active administrator account must remain.");

    public static Error UserNotDoctor =>
        Error.Conflict(
            code: "StaffUser.UserNotDoctor",
            description: "Only accounts with the Doctor role can be linked to a doctor profile.");

    public static Error DoctorUserAlreadyLinked =>
        Error.Conflict(
            code: "Doctor.UserAlreadyLinked",
            description: "That user account is already linked to another doctor profile.");
}