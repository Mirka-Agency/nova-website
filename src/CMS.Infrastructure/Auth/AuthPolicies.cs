namespace CMS.Infrastructure.Auth;

public static class AuthPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string ManageBlog = "ManageBlog";
    public const string ManageNews = "ManageNews";
    public const string ManageServices = "ManageServices";
    public const string ManageVideo = "ManageVideo";
    public const string ManageTeam = "ManageTeam";
    public const string ManageHonors = "ManageHonors";
    public const string ManageShop = "ManageShop";
    public const string ManageForms = "ManageForms";
    public const string ManageFormSubmissions = "ManageFormSubmissions";
    public const string ExportFormSubmissions = "ExportFormSubmissions";
    public const string ManageMedia = "ManageMedia";
    public const string ManageComments = "ManageComments";
    public const string ManagePopup = "ManagePopup";
    public const string ManageSeo = "ManageSeo";
    public const string ViewBlog = "ViewBlog";
    public const string ViewNews = "ViewNews";
    public const string ViewServices = "ViewServices";
    public const string ViewVideo = "ViewVideo";
    public const string ViewTeam = "ViewTeam";
    public const string ViewHonors = "ViewHonors";
    public const string ViewShop = "ViewShop";
    public const string ViewForms = "ViewForms";
    public const string ViewMedia = "ViewMedia";
    public const string ViewComments = "ViewComments";
    public const string ViewPopup = "ViewPopup";
    public const string ViewSeo = "ViewSeo";
    public const string Customer = "Customer";
}

public static class AuthRoles
{
    public const string Admin = "Admin";
    public const string Editor = "Editor";
    public const string ShopManager = "ShopManager";
    public const string Viewer = "Viewer";
}
