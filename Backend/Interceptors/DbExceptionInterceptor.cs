using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Data.SqlClient;
using CampusServicesPortal.Exceptions;

namespace CampusServicesPortal.Data.Interceptors
{
    public class DbExceptionInterceptor : SaveChangesInterceptor
    {
        public override void SaveChangesFailed(DbContextErrorEventData eventData)
        {
            EvaluateException(eventData.Exception);
            base.SaveChangesFailed(eventData);
        }

        private void EvaluateException(Exception exception)
        {
            if (exception is DbUpdateException dbUpdateException &&
                dbUpdateException.InnerException is SqlException sqlException)
            {
                foreach (SqlError error in sqlException.Errors)
                {
                    // =========================================================================
                    // 1. UNIQUE INDEX / CONSTRAINT VIOLATIONS (Errors 2601, 2627)
                    // =========================================================================
                    if (error.Number == 2601 || error.Number == 2627)
                    {
                        string msg = error.Message;

                        if (msg.Contains("UX_LabBookings_Student_ActiveSlot"))
                            throw new DuplicateBookingException("You already hold an active booking slot for this specific date and time frame.");

                        if (msg.Contains("UX_LabBookings_Seat_ActiveSlot"))
                            throw new DuplicateBookingException("This specific lab seat is already reserved by another student for this slot.");

                        if (msg.Contains("UQ_StudentPhoneNumbers_PhoneNumber"))
                            throw new DuplicateBookingException("This telephone number is already registered under another account profile.");

                        if (msg.Contains("IX_Users_Email"))
                            throw new DuplicateBookingException("A user profile with this email address already exists.");

                        if (msg.Contains("IX_StudentMasterLists_IndexNumber"))
                            throw new DuplicateBookingException("This student Index Number is already registered in the master list.");

                        // Generic unique index fallback
                        throw new DuplicateBookingException("A record with these unique details already exists in the system.");
                    }

                    // =========================================================================
                    // 2. FOREIGN KEY / DELETE RESTRICTION VIOLATIONS (Error 547)
                    // =========================================================================
                    if (error.Number == 547)
                    {
                        string msg = error.Message;

                        // --- Prevent Delete Restrictions (DeleteBehavior.Restrict) ---
                        if (msg.Contains("DELETE statement conflicted"))
                        {
                            if (msg.Contains("FK_Rooms_Hostels_HostelId"))
                                throw new InvalidOperationException("Cannot delete this hostel because it still contains assigned rooms.");

                            if (msg.Contains("FK_HostelApplications_Rooms_AssignedRoomId"))
                                throw new InvalidOperationException("Cannot delete or close this room because students are currently assigned to it.");

                            if (msg.Contains("FK_StudentMasterLists_Faculties_FacultyId"))
                                throw new InvalidOperationException("Cannot delete this faculty because it is assigned to an active student master tracking list.");

                            if (msg.Contains("FK_Events_Venues_VenueId"))
                                throw new InvalidOperationException("Cannot delete this venue because there are active events scheduled to take place inside it.");

                            throw new InvalidOperationException("This record cannot be deleted because it is currently linked to active operational data.");
                        }

                        // --- Prevent Bad Insertions (Missing Parent Records) ---
                        if (msg.Contains("INSERT statement conflicted") || msg.Contains("UPDATE statement conflicted"))
                        {
                            if (msg.Contains("FK_LabBookings_Students_StudentId"))
                                throw new InvalidOperationException("Cannot create booking. The specified Student record does not exist.");

                            if (msg.Contains("FK_LabBookings_LabSeats_SeatId"))
                                throw new InvalidOperationException("The requested lab seat selection does not exist.");

                            throw new InvalidOperationException("Failed to save changes because a related referenced entity record could not be found.");
                        }
                    }
                }
            }
        }
    }
}
