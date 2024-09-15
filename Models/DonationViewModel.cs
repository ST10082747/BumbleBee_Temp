using System.ComponentModel.DataAnnotations;

namespace BumbleBeeFoundation.Models
{
    public class DonationViewModel
    {
        public int DonationId { get; set; }

        [Required]
        [Display(Name = "Donation Type")]
        public string DonationType { get; set; }

        [Required]
        [Display(Name = "Donation Amount")]
        [DataType(DataType.Currency)]
        public decimal DonationAmount { get; set; }

        [Required]
        [Display(Name = "Donor Name")]
        public string DonorName { get; set; }

        [Required]
        [Display(Name = "ID Number")]
        public string DonorIDNumber { get; set; }

        [Required]
        [Display(Name = "Tax Number")]
        public string DonorTaxNumber { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string DonorEmail { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Phone")]
        public string DonorPhone { get; set; }

        public DateTime DonationDate { get; set; }
    }
}
