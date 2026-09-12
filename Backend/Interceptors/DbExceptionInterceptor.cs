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

        public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, System.Threading.CancellationToken cancellationToken = default)
        {
            EvaluateException(eventData.Exception);
            return base.SaveChangesFailedAsync(eventData, cancellationToken);
        }

        private void EvaluateException(Exception? exception)
        {
            if (exception is DbUpdateException dbUpdateException &&
                dbUpdateException.InnerException is SqlException sqlException)
            {
                foreach (SqlError error in sqlException.Errors)
                {
                    // =========================================================================
                    // 1. UNIQUE INDEX / CONSTRAINT VIOLATIONS (Errors 2601, 2627)
                    // =========================================================================
                    try
                    {
                        if (error.Number == 2601 || error.Number == 2627)
                        {
                            string msg = error.Message;

                            if (msg.Contains("UX_LabBookings_Student_ActiveSlot"))
                                throw new DuplicateBookingException("You already hold an active booking slot for this specific date and time frame.");

                            if (msg.Contains("UX_LabBookings_Seat_ActiveSlot"))
                                throw new DuplicateBookingException("This specific lab seat is already reserved by another student for this slot.");

                            if (msg.Contains("IX_StudentPhoneNumbers_PhoneNumber") || msg.Contains("UQ_StudentPhoneNumbers_PhoneNumber"))
                                throw new DuplicateBookingException("This telephone number is already registered under another account profile.");

                            if (msg.Contains("IX_Students_ContactDetails"))
                                throw new DuplicateBookingException("This primary contact telephone number is already registered under another user profile.");

                            if (msg.Contains("IX_Users_Email"))
                                throw new DuplicateBookingException("A user profile with this email address already exists.");

                            if (msg.Contains("IX_StudentMasterLists_IndexNumber"))
                                throw new DuplicateBookingException("This student Index Number is already registered in the master list.");

                            if (msg.Contains("IX_Students_IndexNumber"))
                                throw new DuplicateBookingException("This Student Index Number is already allocated to an active profile.");

                            if (msg.Contains("IX_Hostels_Name"))
                                throw new DuplicateBookingException("A hostel building with this name already exists in the administrative list.");

                            if (msg.Contains("UX_Rooms_Hostel_RoomNumber"))
                                throw new DuplicateBookingException("This specific room number has already been allocated within the chosen hostel building.");

                            if (msg.Contains("IX_Labs_Name"))
                                throw new DuplicateBookingException("A laboratory facility with this name is already registered.");

                            if (msg.Contains("IX_Venues_Name"))
                                throw new DuplicateBookingException("An event venue with this exact name already exists in the system master directory.");

                            if (msg.Contains("UX_Events_Venue_Schedule"))
                                throw new DuplicateBookingException("Scheduling Collision! This physical venue is already booked for another event at the specified date and time.");

                            if (msg.Contains("IX_FeeTypes_Name"))
                                throw new DuplicateBookingException("A billing ledger fee category configuration with this exact name already exists.");

                            if (msg.Contains("IX_CertificateTypes_Name"))
                                throw new DuplicateBookingException("A certificate type with this title already exists.");

                            if (msg.Contains("UX_CertificateRequests_Student_Pending"))
                                throw new DuplicateBookingException("A pending request already exists for this certificate type.");

                            if (msg.Contains("IX_ComplaintCategories_Name"))
                                throw new DuplicateBookingException("A complaint category with this name already exists.");

                            if (msg.Contains("IX_Faculties_Name"))
                                throw new DuplicateBookingException("A faculty with this designated title already exists.");

                            if (msg.Contains("UX_EventRegistrations_Event_Student"))
                                throw new DuplicateBookingException("You are already registered for this event.");

                            // Generic unique index fallback
                            throw new DuplicateBookingException("A record with these unique details already exists in the system.");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
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
