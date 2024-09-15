namespace BumbleBeeFoundation.Models
{
    public class Document
    {
        public int DocumentID { get; set; }
        public string DocumentName { get; set; }
        public string DocumentType { get; set; }
        public DateTime UploadDate { get; set; }
        public string Status { get; set; }
        public string CompanyName { get; set; }
    }
}
