using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using BumbleBeeFoundation.Models;

namespace BumbleBeeFoundation.Controllers
{
    public class CompanyController : Controller
    {
        private readonly string _connectionString;

        public CompanyController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IActionResult> Index()
        {
            // Fetch CompanyID from session
            int? companyId = HttpContext.Session.GetInt32("CompanyID");
            if (companyId == null)
            {
                // Handle case where CompanyID is not available
                return RedirectToAction("Login", "Account");
            }

            // Retrieve company information from the database
            CompanyViewModel companyInfo = await GetCompanyInfo(companyId.Value);

            if (companyInfo == null)
            {
                // Handle case where company info is not found
                return NotFound();
            }

            return View(companyInfo);
        }

        public IActionResult RequestFunding()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RequestFunding(FundingRequestViewModel model)
        {
            // Log model state
            Console.WriteLine("Model State Is Valid: " + ModelState.IsValid);

            if (ModelState.IsValid)
            {
                // Log model values
                Console.WriteLine("ProjectDescription: " + model.ProjectDescription);
                Console.WriteLine("RequestedAmount: " + model.RequestedAmount);
                Console.WriteLine("ProjectImpact: " + model.ProjectImpact);
                Console.WriteLine("Status: " + model.Status);

                try
                {
                    // Retrieve CompanyID from the session
                    int? companyId = HttpContext.Session.GetInt32("CompanyID");
                    if (companyId == null)
                    {
                        Console.WriteLine("CompanyID not found in session.");
                        ModelState.AddModelError(string.Empty, "Company ID not found in session.");
                        return View(model);
                    }

                    // Log CompanyID
                    Console.WriteLine("CompanyID: " + companyId.Value);

                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();
                        Console.WriteLine("Database connection opened.");

                        // Insert the funding request
                        string requestQuery = @"INSERT INTO FundingRequests (CompanyID, ProjectDescription, RequestedAmount, ProjectImpact, Status, SubmittedAt)
                                        VALUES (@CompanyID, @ProjectDescription, @RequestedAmount, @ProjectImpact, @Status, GETDATE());
                                        SELECT SCOPE_IDENTITY();";

                        using (SqlCommand command = new SqlCommand(requestQuery, connection))
                        {
                            command.Parameters.AddWithValue("@CompanyID", companyId.Value);
                            command.Parameters.AddWithValue("@ProjectDescription", model.ProjectDescription ?? (object)DBNull.Value);
                            command.Parameters.AddWithValue("@RequestedAmount", model.RequestedAmount);
                            command.Parameters.AddWithValue("@ProjectImpact", model.ProjectImpact ?? (object)DBNull.Value);
                            command.Parameters.AddWithValue("@Status", model.Status); // Pass Status

                            // Log query execution
                            Console.WriteLine("Executing query: " + requestQuery);

                            int requestId = Convert.ToInt32(await command.ExecuteScalarAsync());

                            // Log new request ID
                            Console.WriteLine("New RequestID: " + requestId);

                            // Redirect to the confirmation page
                            return RedirectToAction(nameof(FundingRequestConfirmation), new { id = requestId });
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log exception details
                    Console.WriteLine("An error occurred: " + ex.Message);
                    ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
                }
            }
            else
            {
                // Log model state errors
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine("ModelState Error: " + error.ErrorMessage);
                }
            }

            // Return the view with the model if the model state is not valid or an error occurred
            return View(model);
        }


        public async Task<IActionResult> FundingRequestConfirmation(int id)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT * FROM FundingRequests WHERE RequestID = @RequestID";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@RequestID", id);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var request = new FundingRequestViewModel
                            {
                                RequestID = reader.GetInt32(reader.GetOrdinal("RequestID")),
                                CompanyID = reader.GetInt32(reader.GetOrdinal("CompanyID")),
                                ProjectDescription = reader.GetString(reader.GetOrdinal("ProjectDescription")),
                                RequestedAmount = reader.GetDecimal(reader.GetOrdinal("RequestedAmount")),
                                ProjectImpact = reader.GetString(reader.GetOrdinal("ProjectImpact")),
                                Status = reader.GetString(reader.GetOrdinal("Status")),
                                SubmittedAt = reader.GetDateTime(reader.GetOrdinal("SubmittedAt"))
                            };
                            return View(request);
                        }
                    }
                }
            }
            return NotFound();
        }

        public async Task<IActionResult> FundingRequestHistory()
        {
            // Retrieve the CompanyID from the session
            int? companyId = HttpContext.Session.GetInt32("CompanyID");

            if (companyId == null)
            {
                
                return RedirectToAction("Login", "Account");
            }

            var requests = new List<FundingRequestViewModel>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Query to fetch funding requests for the logged-in company
                string query = "SELECT * FROM FundingRequests WHERE CompanyID = @CompanyID ORDER BY SubmittedAt DESC";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CompanyID", companyId);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            requests.Add(new FundingRequestViewModel
                            {
                                RequestID = reader.GetInt32(reader.GetOrdinal("RequestID")),
                                CompanyID = reader.GetInt32(reader.GetOrdinal("CompanyID")),
                                ProjectDescription = reader.GetString(reader.GetOrdinal("ProjectDescription")),
                                RequestedAmount = reader.GetDecimal(reader.GetOrdinal("RequestedAmount")),
                                Status = reader.GetString(reader.GetOrdinal("Status")),
                                SubmittedAt = reader.GetDateTime(reader.GetOrdinal("SubmittedAt"))
                            });
                        }
                    }
                }
            }

            return View(requests);
        }


        private async Task<CompanyViewModel> GetCompanyInfo(int companyId)
        {
            // Retrieve the UserID from the session to ensure the user is authorized to get this company's info
            int? userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                
                return null;
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Query to retrieve company info where both CompanyID and UserID match
                string query = @"SELECT * FROM Companies WHERE CompanyID = @CompanyID AND UserID = @UserID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CompanyID", companyId);
                    command.Parameters.AddWithValue("@UserID", userId); 

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new CompanyViewModel
                            {
                                CompanyID = reader.GetInt32(reader.GetOrdinal("CompanyID")),
                                CompanyName = reader.GetString(reader.GetOrdinal("CompanyName")),
                                ContactEmail = reader.GetString(reader.GetOrdinal("ContactEmail")),
                                ContactPhone = reader.GetString(reader.GetOrdinal("ContactPhone")),
                                DateJoined = reader.GetDateTime(reader.GetOrdinal("DateJoined")),
                                Status = reader.GetString(reader.GetOrdinal("Status"))
                            };
                        }
                    }
                }
            }

            // Return null if no company info is found or the user does not own the company
            return null;
        }

    }
}
