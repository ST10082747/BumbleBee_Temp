using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using BumbleBeeFoundation.Models;
using System.Reflection.Metadata;
using Document = BumbleBeeFoundation.Models.Document;

namespace BumbleBeeFoundation.Controllers
{
    public class AdminController : Controller
    {

        private readonly ILogger<AdminController> _logger;
        private readonly string _connectionString;

        // Constructor to inject IConfiguration
        public AdminController(IConfiguration configuration, ILogger<AdminController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        // Action method to handle requests to the Dashboard
        public IActionResult Dashboard()
        {
            var viewModel = new DashboardViewModel();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;

                    command.CommandText = "SELECT COUNT(*) FROM Users";
                    viewModel.TotalUsers = (int)command.ExecuteScalar();

                    command.CommandText = "SELECT COUNT(*) FROM Companies";
                    viewModel.TotalCompanies = (int)command.ExecuteScalar();

                    command.CommandText = "SELECT COUNT(*) FROM Donations";
                    viewModel.TotalDonations = (int)command.ExecuteScalar();

                    command.CommandText = "SELECT COUNT(*) FROM FundingRequests";
                    viewModel.TotalFundingRequests = (int)command.ExecuteScalar();
                }
            }

            return View(viewModel);
        }

        // User Management
        // Action method to display user management
        public IActionResult UserManagement()
        {
            var users = new List<User>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("SELECT * FROM Users", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(new User
                            {
                                UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                LastName = reader.GetString(reader.GetOrdinal("LastName")),
                                Email = reader.GetString(reader.GetOrdinal("Email")),
                                Role = reader.GetString(reader.GetOrdinal("Role"))
                            });
                        }
                    }
                }
            }

            return View(users);
        }

        public IActionResult CreateUser()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateUser(User user)
        {
            if (ModelState.IsValid)
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = "INSERT INTO Users (FirstName, LastName, Email, Password, Role) VALUES (@FirstName, @LastName, @Email, @Password, @Role)";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@FirstName", user.FirstName);
                        command.Parameters.AddWithValue("@LastName", user.LastName);
                        command.Parameters.AddWithValue("@Email", user.Email);
                        command.Parameters.AddWithValue("@Password", user.Password);
                        command.Parameters.AddWithValue("@Role", user.Role);
                        command.ExecuteNonQuery();
                    }
                }
                return RedirectToAction(nameof(UserManagement));
            }
            return View(user);
        }

        public IActionResult EditUser(int id)
        {
            UserForEdit userForEdit = null;
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = "SELECT UserID, FirstName, LastName, Email, Role FROM Users WHERE UserID = @UserID";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@UserID", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            userForEdit = new UserForEdit
                            {
                                UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                LastName = reader.GetString(reader.GetOrdinal("LastName")),
                                Email = reader.GetString(reader.GetOrdinal("Email")),
                                Role = reader.GetString(reader.GetOrdinal("Role"))
                            };
                        }
                    }
                }
            }

            if (userForEdit == null)
            {
                return NotFound();
            }

            return View(userForEdit);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditUser(int id, UserForEdit userForEdit)
        {
            if (id != userForEdit.UserID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = "UPDATE Users SET FirstName = @FirstName, LastName = @LastName, Email = @Email, Role = @Role WHERE UserID = @UserID";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@UserID", userForEdit.UserID);
                        command.Parameters.AddWithValue("@FirstName", userForEdit.FirstName);
                        command.Parameters.AddWithValue("@LastName", userForEdit.LastName);
                        command.Parameters.AddWithValue("@Email", userForEdit.Email);
                        command.Parameters.AddWithValue("@Role", userForEdit.Role);
                        command.ExecuteNonQuery();
                    }
                }
                return RedirectToAction(nameof(UserManagement));
            }
            return View(userForEdit);
        }

        public IActionResult DeleteUser(int id)
        {
            User user = null;
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = "SELECT * FROM Users WHERE UserID = @UserID";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@UserID", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            user = new User
                            {
                                UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                LastName = reader.GetString(reader.GetOrdinal("LastName")),
                                Email = reader.GetString(reader.GetOrdinal("Email")),
                                Role = reader.GetString(reader.GetOrdinal("Role"))
                            };
                        }
                    }
                }
            }

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteUserConfirmed(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = "DELETE FROM Users WHERE UserID = @UserID";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@UserID", id);
                    command.ExecuteNonQuery();
                }
            }
            return RedirectToAction(nameof(UserManagement));
        }

        /// company below
        public IActionResult CompanyManagement()
        {
            var companies = new List<Company>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("SELECT * FROM Companies", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            companies.Add(new Company
                            {
                                CompanyID = reader.GetInt32(reader.GetOrdinal("CompanyID")),
                                CompanyName = reader.GetString(reader.GetOrdinal("CompanyName")),
                                ContactEmail = reader.GetString(reader.GetOrdinal("ContactEmail")),
                                ContactPhone = reader.GetString(reader.GetOrdinal("ContactPhone")),
                                Description = reader.GetString(reader.GetOrdinal("Description")),
                                DateJoined = reader.GetDateTime(reader.GetOrdinal("DateJoined")),
                                Status = reader.GetString(reader.GetOrdinal("Status"))
                            });
                        }
                    }
                }
            }

            return View(companies);
        }

        public IActionResult CompanyDetails(int id)
        {
            Company company = null;
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = "SELECT * FROM Companies WHERE CompanyID = @CompanyID";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@CompanyID", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            company = new Company
                            {
                                CompanyID = reader.GetInt32(reader.GetOrdinal("CompanyID")),
                                CompanyName = reader.GetString(reader.GetOrdinal("CompanyName")),
                                ContactEmail = reader.GetString(reader.GetOrdinal("ContactEmail")),
                                ContactPhone = reader.GetString(reader.GetOrdinal("ContactPhone")),
                                Description = reader.GetString(reader.GetOrdinal("Description")),
                                DateJoined = reader.GetDateTime(reader.GetOrdinal("DateJoined")),
                                Status = reader.GetString(reader.GetOrdinal("Status"))
                            };
                        }
                    }
                }
            }

            if (company == null)
            {
                return NotFound();
            }

            return View(company);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveCompany(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = "UPDATE Companies SET Status = 'Approved' WHERE CompanyID = @CompanyID";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@CompanyID", id);
                    command.ExecuteNonQuery();
                }
            }
            return RedirectToAction(nameof(CompanyManagement));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectCompany(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = "UPDATE Companies SET Status = 'Rejected' WHERE CompanyID = @CompanyID";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@CompanyID", id);
                    command.ExecuteNonQuery();
                }
            }
            return RedirectToAction(nameof(CompanyManagement));
        }

        // donations
        // Donation Management
        public IActionResult DonationManagement()
        {
            var donations = new List<Donation>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(@"
                    SELECT d.*, c.CompanyName 
                    FROM Donations d 
                    LEFT JOIN Companies c ON d.CompanyID = c.CompanyID", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            donations.Add(new Donation
                            {
                                DonationID = reader.GetInt32(reader.GetOrdinal("DonationID")),
                                CompanyID = reader.IsDBNull(reader.GetOrdinal("CompanyID")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("CompanyID")),
                                CompanyName = reader.IsDBNull(reader.GetOrdinal("CompanyName")) ? null : reader.GetString(reader.GetOrdinal("CompanyName")),
                                DonationDate = reader.GetDateTime(reader.GetOrdinal("DonationDate")),
                                DonationType = reader.GetString(reader.GetOrdinal("DonationType")),
                                DonationAmount = reader.GetDecimal(reader.GetOrdinal("DonationAmount")),
                                DonorName = reader.GetString(reader.GetOrdinal("DonorName")),
                                DonorEmail = reader.GetString(reader.GetOrdinal("DonorEmail"))
                            });
                        }
                    }
                }
            }

            return View(donations);
        }

        public IActionResult DonationDetails(int id)
        {
            Donation donation = null;
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = @"
                    SELECT d.*, c.CompanyName 
                    FROM Donations d 
                    LEFT JOIN Companies c ON d.CompanyID = c.CompanyID 
                    WHERE d.DonationID = @DonationID";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@DonationID", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            donation = new Donation
                            {
                                DonationID = reader.GetInt32(reader.GetOrdinal("DonationID")),
                                CompanyID = reader.IsDBNull(reader.GetOrdinal("CompanyID")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("CompanyID")),
                                CompanyName = reader.IsDBNull(reader.GetOrdinal("CompanyName")) ? null : reader.GetString(reader.GetOrdinal("CompanyName")),
                                DonationDate = reader.GetDateTime(reader.GetOrdinal("DonationDate")),
                                DonationType = reader.GetString(reader.GetOrdinal("DonationType")),
                                DonationAmount = reader.GetDecimal(reader.GetOrdinal("DonationAmount")),
                                DonorName = reader.GetString(reader.GetOrdinal("DonorName")),
                                DonorIDNumber = reader.GetString(reader.GetOrdinal("DonorIDNumber")),
                                DonorTaxNumber = reader.GetString(reader.GetOrdinal("DonorTaxNumber")),
                                DonorEmail = reader.GetString(reader.GetOrdinal("DonorEmail")),
                                DonorPhone = reader.GetString(reader.GetOrdinal("DonorPhone"))
                            };
                        }
                    }
                }
            }

            if (donation == null)
            {
                return NotFound();
            }

            return View(donation);
        }

        // funding

        // Funding Request Management
        public IActionResult FundingRequestManagement()
        {
            var fundingRequests = new List<FundingRequest>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(@"
                    SELECT fr.*, c.CompanyName 
                    FROM FundingRequests fr 
                    JOIN Companies c ON fr.CompanyID = c.CompanyID", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            fundingRequests.Add(new FundingRequest
                            {
                                RequestID = reader.GetInt32(reader.GetOrdinal("RequestID")),
                                CompanyID = reader.GetInt32(reader.GetOrdinal("CompanyID")),
                                CompanyName = reader.GetString(reader.GetOrdinal("CompanyName")),
                                ProjectDescription = reader.GetString(reader.GetOrdinal("ProjectDescription")),
                                RequestedAmount = reader.GetDecimal(reader.GetOrdinal("RequestedAmount")),
                                ProjectImpact = reader.GetString(reader.GetOrdinal("ProjectImpact")),
                                Status = reader.GetString(reader.GetOrdinal("Status")),
                                SubmittedAt = reader.GetDateTime(reader.GetOrdinal("SubmittedAt"))
                            });
                        }
                    }
                }
            }

            return View(fundingRequests);
        }

        public IActionResult FundingRequestDetails(int id)
        {
            FundingRequest fundingRequest = null;
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = @"
                    SELECT fr.*, c.CompanyName 
                    FROM FundingRequests fr 
                    JOIN Companies c ON fr.CompanyID = c.CompanyID 
                    WHERE fr.RequestID = @RequestID";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@RequestID", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            fundingRequest = new FundingRequest
                            {
                                RequestID = reader.GetInt32(reader.GetOrdinal("RequestID")),
                                CompanyID = reader.GetInt32(reader.GetOrdinal("CompanyID")),
                                CompanyName = reader.GetString(reader.GetOrdinal("CompanyName")),
                                ProjectDescription = reader.GetString(reader.GetOrdinal("ProjectDescription")),
                                RequestedAmount = reader.GetDecimal(reader.GetOrdinal("RequestedAmount")),
                                ProjectImpact = reader.GetString(reader.GetOrdinal("ProjectImpact")),
                                Status = reader.GetString(reader.GetOrdinal("Status")),
                                SubmittedAt = reader.GetDateTime(reader.GetOrdinal("SubmittedAt"))
                            };
                        }
                    }
                }
            }

            if (fundingRequest == null)
            {
                return NotFound();
            }

            return View(fundingRequest);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveFundingRequest(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = "UPDATE FundingRequests SET Status = 'Approved' WHERE RequestID = @RequestID";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@RequestID", id);
                    command.ExecuteNonQuery();
                }
            }
            return RedirectToAction(nameof(FundingRequestManagement));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectFundingRequest(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = "UPDATE FundingRequests SET Status = 'Rejected' WHERE RequestID = @RequestID";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@RequestID", id);
                    command.ExecuteNonQuery();
                }
            }
            return RedirectToAction(nameof(FundingRequestManagement));
        }

        // documents
        // GET: Admin/Documents
        public ActionResult Documents()
        {
            List<Document> documents = new List<Document>();

           using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = @"SELECT d.DocumentID, d.DocumentName, d.DocumentType, d.UploadDate, d.Status, c.CompanyName 
                                 FROM Documents d
                                 INNER JOIN Companies c ON d.CompanyID = c.CompanyID
                                 ORDER BY d.UploadDate DESC";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            documents.Add(new Document
                            {
                                DocumentID = reader.GetInt32(0),
                                DocumentName = reader.GetString(1),
                                DocumentType = reader.GetString(2),
                                UploadDate = reader.GetDateTime(3),
                                Status = reader.GetString(4),
                                CompanyName = reader.GetString(5)
                            });
                        }
                    }
                }
            }

            return View(documents);
        }

        // POST: Admin/ApproveDocument
        [HttpPost]
        public ActionResult ApproveDocument(int documentId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "UPDATE Documents SET Status = 'Approved' WHERE DocumentID = @DocumentID";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@DocumentID", documentId);
                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Documents");
        }

        // POST: Admin/RejectDocument
        [HttpPost]
        public ActionResult RejectDocument(int documentId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "UPDATE Documents SET Status = 'Rejected' WHERE DocumentID = @DocumentID";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@DocumentID", documentId);
                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Documents");
        }

        // generate reports
        // GET: Admin/DonationReport
        public ActionResult DonationReport()
        {
            List<DonationReportItem> donations = new List<DonationReportItem>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = @"SELECT d.DonationID, d.DonationDate, d.DonationType, d.DonationAmount, 
                                        d.DonorName, c.CompanyName
                                 FROM Donations d
                                 LEFT JOIN Companies c ON d.CompanyID = c.CompanyID
                                 ORDER BY d.DonationDate DESC";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            donations.Add(new DonationReportItem
                            {
                                DonationID = reader.GetInt32(0),
                                DonationDate = reader.GetDateTime(1),
                                DonationType = reader.GetString(2),
                                DonationAmount = reader.GetDecimal(3),
                                DonorName = reader.GetString(4),
                                CompanyName = reader.IsDBNull(5) ? null : reader.GetString(5)
                            });
                        }
                    }
                }
            }

            return View(donations);
        }

        // GET: Admin/FundingRequestReport
        public ActionResult FundingRequestReport()
        {
            List<FundingRequestReportItem> requests = new List<FundingRequestReportItem>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = @"SELECT fr.RequestID, c.CompanyName, fr.ProjectDescription, 
                                        fr.RequestedAmount, fr.Status, fr.SubmittedAt
                                 FROM FundingRequests fr
                                 INNER JOIN Companies c ON fr.CompanyID = c.CompanyID
                                 ORDER BY fr.SubmittedAt DESC";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            requests.Add(new FundingRequestReportItem
                            {
                                RequestID = reader.GetInt32(0),
                                CompanyName = reader.GetString(1),
                                ProjectDescription = reader.GetString(2),
                                RequestedAmount = reader.GetDecimal(3),
                                Status = reader.GetString(4),
                                SubmittedAt = reader.GetDateTime(5)
                            });
                        }
                    }
                }
            }

            return View(requests);
        }

    }

}
