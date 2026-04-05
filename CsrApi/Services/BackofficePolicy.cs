namespace CsrApi.Services;

public interface IBackofficePolicy
{
    string GetRole(HttpContext httpContext);
    bool CanViewFullProfile(HttpContext httpContext);
    bool CanViewPhotos(HttpContext httpContext);
    bool CanViewDocuments(HttpContext httpContext);
    bool CanUpdateReviewStatus(HttpContext httpContext);
    bool CanEditInternalNote(HttpContext httpContext);
}

public class BackofficePolicy : IBackofficePolicy
{
    public string GetRole(HttpContext httpContext)
    {
        return httpContext.Items["StaffRole"]?.ToString() ?? "";
    }

    public bool CanViewFullProfile(HttpContext httpContext)
    {
        var role = GetRole(httpContext);
        return role == "Teacher";
    }

    public bool CanViewPhotos(HttpContext httpContext)
    {
        var role = GetRole(httpContext);
        // Pending PDPA decision: only Teacher for now
        return role == "Teacher";
    }

    public bool CanViewDocuments(HttpContext httpContext)
    {
        var role = GetRole(httpContext);
        // Pending PDPA decision: only Teacher for now
        return role == "Teacher";
    }

    public bool CanUpdateReviewStatus(HttpContext httpContext)
    {
        var role = GetRole(httpContext);
        return role == "Teacher";
    }

    public bool CanEditInternalNote(HttpContext httpContext)
    {
        var role = GetRole(httpContext);
        return role == "Teacher";
    }
}
