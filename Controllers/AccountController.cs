using Microsoft.AspNetCore.Mvc;
using BumbleBeeFoundation.Models;
using System.Text;
using System.Security.Cryptography;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace BumbleBeeFoundation.Controllers
{
    public class AccountController : Controller
    {

        private readonly string _connectionString;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IConfiguration configuration, ILogger<AccountController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Query to get UserID, Password, and Role from Users table
                    string userQuery = "SELECT UserID, Password, Role FROM Users WHERE Email = @Email";
                    using (SqlCommand command = new SqlCommand(userQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Email", model.Email);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string storedPassword = reader["Password"].ToString();
                                if (VerifyPassword(model.Password, storedPassword))
                                {
                                    // Get UserID and Role
                                    int userId = reader.GetInt32(reader.GetOrdinal("UserID"));
                                    string role = reader.GetString(reader.GetOrdinal("Role"));

                                    string userEmail = model.Email;

                                    // Close the reader before running another query
                                    reader.Close();

                                    // Store the user role in the session
                                    HttpContext.Session.SetString("UserRole", role);
                                    HttpContext.Session.SetInt32("UserID", userId);

                                    HttpContext.Session.SetString("UserEmail", userEmail); // Set UserEmail in session

                                    // Check if the user is a Company, otherwise skip CompanyID lookup
                                    if (role == "Company")
                                    {
                                        // Query to get CompanyID from Companies table
                                        string companyQuery = "SELECT CompanyID FROM Companies WHERE UserID = @UserID";
                                        using (SqlCommand companyCommand = new SqlCommand(companyQuery, connection))
                                        {
                                            companyCommand.Parameters.AddWithValue("@UserID", userId);
                                            using (SqlDataReader companyReader = await companyCommand.ExecuteReaderAsync())
                                            {
                                                if (await companyReader.ReadAsync())
                                                {
                                                    // Get CompanyID
                                                    int companyId = companyReader.GetInt32(companyReader.GetOrdinal("CompanyID"));

                                                    // Store CompanyID in session
                                                    HttpContext.Session.SetInt32("CompanyID", companyId);
                                                    // Redirect to the Company dashboard
                                                    return RedirectToAction("Index", "Company");
                                                }
                                                else
                                                {
                                                    ModelState.AddModelError(string.Empty, "Company ID not found.");
                                                    return View(model);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // If the role is Admin or Donor, no need to look for CompanyID
                                        if (role == "Admin")
                                        {
                                            return RedirectToAction("Dashboard", "Admin");
                                        }
                                        else if (role == "Donor")
                                        {
                                            return RedirectToAction("Index", "Donor");
                                        }
                                        else
                                        {
                                            return RedirectToAction("Index", "Home");
                                        }
                                    }
                                }
                                else
                                {
                                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                                    return View(model);
                                }
                            }
                            else
                            {
                                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                                return View(model);
                            }
                        }
                    }
                }
            }
            return View(model);
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            _logger.LogInformation($"Registration attempt - Role: {model.Role}, Email: {model.Email}");

            // Remove company-specific validations from ModelState if role is not Company
            if (model.Role != "Company")
            {
                ModelState.Remove("CompanyName");
                ModelState.Remove("CompanyDescription");
                ModelState.Remove("ContactPhone");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid at the beginning of the action");
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        _logger.LogWarning($"ModelState Error: {error.ErrorMessage}");
                    }
                }
            }

            // Additional validation for Company role
            if (model.Role == "Company")
            {
                _logger.LogInformation("Validating Company-specific fields");
                if (string.IsNullOrEmpty(model.CompanyName))
                {
                    ModelState.AddModelError("CompanyName", "Company Name is required for Company role.");
                    _logger.LogWarning("CompanyName is required but not provided");
                }
                if (string.IsNullOrEmpty(model.CompanyDescription))
                {
                    ModelState.AddModelError("CompanyDescription", "Company Description is required for Company role.");
                    _logger.LogWarning("CompanyDescription is required but not provided");
                }
                if (string.IsNullOrEmpty(model.ContactPhone))
                {
                    ModelState.AddModelError("ContactPhone", "Contact Phone is required for Company role.");
                    _logger.LogWarning("ContactPhone is required but not provided");
                }
            }

            if (ModelState.IsValid)
            {
                _logger.LogInformation("ModelState is valid, proceeding with registration");
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    _logger.LogInformation("Database connection opened");
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Insert into Users table
                            string userQuery = @"INSERT INTO Users (FirstName, LastName, Email, Password, Role, CreatedAt) 
                                         VALUES (@FirstName, @LastName, @Email, @Password, @Role, GETDATE());
                                         SELECT SCOPE_IDENTITY();";
                            int userId;
                            using (SqlCommand command = new SqlCommand(userQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@FirstName", model.FirstName);
                                command.Parameters.AddWithValue("@LastName", model.LastName);
                                command.Parameters.AddWithValue("@Email", model.Email);
                                command.Parameters.AddWithValue("@Password", HashPassword(model.Password));
                                command.Parameters.AddWithValue("@Role", model.Role);

                                _logger.LogInformation("Executing user insertion query");
                                userId = Convert.ToInt32(await command.ExecuteScalarAsync());
                                _logger.LogInformation($"User inserted with ID: {userId}");
                            }

                            // If registering as a company, insert into Companies table
                            if (model.Role == "Company")
                            {
                                _logger.LogInformation("Inserting company data");
                                string companyQuery = @"INSERT INTO Companies (CompanyName, ContactEmail, ContactPhone, Description, DateJoined, Status, UserID) 
                                                VALUES (@CompanyName, @ContactEmail, @ContactPhone, @Description, GETDATE(), 'Pending', @UserID)";
                                using (SqlCommand command = new SqlCommand(companyQuery, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@CompanyName", model.CompanyName);
                                    command.Parameters.AddWithValue("@ContactEmail", model.Email);
                                    command.Parameters.AddWithValue("@ContactPhone", model.ContactPhone);
                                    command.Parameters.AddWithValue("@Description", model.CompanyDescription);
                                    command.Parameters.AddWithValue("@UserID", userId);

                                    await command.ExecuteNonQueryAsync();
                                    _logger.LogInformation("Company data inserted successfully");
                                }
                            }

                            transaction.Commit();
                            _logger.LogInformation("Transaction committed successfully");
                            return RedirectToAction(nameof(Login));
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            _logger.LogError(ex, "Error during registration process");
                            ModelState.AddModelError(string.Empty, "Registration failed. Please try again.");
                        }
                    }
                }
            }
            else
            {
                _logger.LogWarning("ModelState is invalid after additional validation");
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        _logger.LogWarning($"ModelState Error: {error.ErrorMessage}");
                    }
                }
            }

            _logger.LogInformation("Returning to Register view due to validation errors or exception");
            return View(model);
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private bool VerifyPassword(string inputPassword, string storedPassword)
        {
            string hashedInput = HashPassword(inputPassword);
            return string.Equals(hashedInput, storedPassword);
        }
    }
}
