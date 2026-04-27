namespace Template.Infrastructure.Email;

public sealed class EmailOptions
{
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUser { get; set; }
    public string? SmtpPassword { get; set; }
    public string? FromAddress { get; set; }
    public string? FromName { get; set; } = "Template";
    public bool UseSsl { get; set; } = true;
    public string? OakenServiceUrl { get; set; }
}
