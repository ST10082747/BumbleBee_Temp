using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using BumbleBeeFoundation.Models;


namespace BumbleBeeFoundation.Controllers
{
    public class DonorController : Controller
    {

        private readonly string _connectionString;
        private readonly ILogger<DonorController> _logger;

        public DonorController(IConfiguration configuration, ILogger<DonorController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Donate()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Donate(DonationViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"INSERT INTO Donations (DonationDate, DonationType, DonationAmount, DonorName, DonorIDNumber, DonorTaxNumber, DonorEmail, DonorPhone)
                                     VALUES (@DonationDate, @DonationType, @DonationAmount, @DonorName, @DonorIDNumber, @DonorTaxNumber, @DonorEmail, @DonorPhone);
                                     SELECT SCOPE_IDENTITY();";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@DonationDate", DateTime.Now);
                        command.Parameters.AddWithValue("@DonationType", model.DonationType);
                        command.Parameters.AddWithValue("@DonationAmount", model.DonationAmount);
                        command.Parameters.AddWithValue("@DonorName", model.DonorName);
                        command.Parameters.AddWithValue("@DonorIDNumber", model.DonorIDNumber);
                        command.Parameters.AddWithValue("@DonorTaxNumber", model.DonorTaxNumber);
                        command.Parameters.AddWithValue("@DonorEmail", model.DonorEmail);
                        command.Parameters.AddWithValue("@DonorPhone", model.DonorPhone);

                        int donationId = Convert.ToInt32(await command.ExecuteScalarAsync());
                        return RedirectToAction(nameof(DonationConfirmation), new { id = donationId });
                    }
                }
            }
            return View(model);
        }

        public async Task<IActionResult> DonationConfirmation(int id)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT * FROM Donations WHERE DonationID = @DonationID";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DonationID", id);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var donation = new DonationViewModel
                            {
                                DonationId = reader.GetInt32(reader.GetOrdinal("DonationID")),
                                DonationDate = reader.GetDateTime(reader.GetOrdinal("DonationDate")),
                                DonationType = reader.GetString(reader.GetOrdinal("DonationType")),
                                DonationAmount = reader.GetDecimal(reader.GetOrdinal("DonationAmount")),
                                DonorName = reader.GetString(reader.GetOrdinal("DonorName")),
                                DonorIDNumber = reader.GetString(reader.GetOrdinal("DonorIDNumber")),
                                DonorTaxNumber = reader.GetString(reader.GetOrdinal("DonorTaxNumber")),
                                DonorEmail = reader.GetString(reader.GetOrdinal("DonorEmail")),
                                DonorPhone = reader.GetString(reader.GetOrdinal("DonorPhone"))
                            };
                            return View(donation);
                        }
                    }
                }
            }
            return NotFound();
        }

        public async Task<IActionResult> DonationHistory()
        {
            // Retrieve the logged-in user's role and relevant identifiers (UserID, CompanyID, Email) from the session
            string userRole = HttpContext.Session.GetString("UserRole");
            int? companyId = HttpContext.Session.GetInt32("CompanyID");
            string userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(userRole) || (userRole == "Company" && companyId == null))
            {
                // Handle cases where user is not logged in or the role/ID is missing
                _logger.LogWarning("User role or company ID not found.");
                return RedirectToAction("Login", "Account");
            }

            var donations = new List<DonationViewModel>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = "";

                // Build the query based on the user's role
                if (userRole == "Company")
                {
                    _logger.LogInformation("Retrieving donation history for company with ID: {0}", companyId);
                    query = "SELECT * FROM Donations WHERE CompanyID = @CompanyID ORDER BY DonationDate DESC";
                }
                else if (userRole == "Donor")
                {
                    _logger.LogInformation("Retrieving donation history for donor with email: {0}", userEmail);
                    query = "SELECT * FROM Donations WHERE DonorEmail = @DonorEmail ORDER BY DonationDate DESC";
                }

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Add parameters based on the user's role
                    if (userRole == "Company")
                    {
                        command.Parameters.AddWithValue("@CompanyID", companyId);
                    }
                    else if (userRole == "Donor")
                    {
                        if (!string.IsNullOrEmpty(userEmail))
                        {
                            command.Parameters.AddWithValue("@DonorEmail", userEmail);
                        }
                        else
                        {
                            _logger.LogError("Donor email is null or empty.");
                            return RedirectToAction("Login", "Account");
                        }
                    }

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            donations.Add(new DonationViewModel
                            {
                                DonationId = reader.GetInt32(reader.GetOrdinal("DonationID")),
                                DonationDate = reader.GetDateTime(reader.GetOrdinal("DonationDate")),
                                DonationType = reader.GetString(reader.GetOrdinal("DonationType")),
                                DonationAmount = reader.GetDecimal(reader.GetOrdinal("DonationAmount")),
                                DonorName = reader.GetString(reader.GetOrdinal("DonorName"))
                            });
                        }
                    }
                }
            }

            return View(donations);
        }

    }
}
