namespace CMS.Web.Areas.Admin.ViewModels;

public sealed class CtaHelperPageViewModel
{
    public string Brand { get; init; } = "Nova Clinic";
    public string PhoneDisplay { get; init; } = "۰۲۱-۹۱۰۹۳۴۹۲";
    public string PhoneDigits { get; init; } = "02191093492";
    public string TelHref { get; init; } = "tel:02191093492";
    public string WhatsAppUrl { get; init; } = "tel:02191093492";
    public string ContactUrl { get; init; } = "/contact-us/";
    public string DoctorsUrl { get; init; } = "/doctors/";
    public string ServicesUrl { get; init; } = "/services/";
    public string DoctorImageUrl { get; init; } = "/template/assets/images/doctors/dr-hamedani.webp";
    public bool IsPopup { get; init; }
}
