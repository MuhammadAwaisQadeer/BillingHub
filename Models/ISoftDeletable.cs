namespace Client_Invoice_System.Models
{
    public interface ISoftDeletable
    {
        bool IsDeleted { get; set; }
    }
}
