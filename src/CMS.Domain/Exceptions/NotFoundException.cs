namespace CMS.Domain.Exceptions;

public class NotFoundException : DomainException
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string name, object key)
        : base($"مورد «{LocalizeEntityName(name)}» ({key}) یافت نشد.")
    {
    }

    private static string LocalizeEntityName(string name) => name switch
    {
        "Post" => "مطلب",
        "Category" => "دسته",
        "Tag" => "برچسب",
        "Product" => "محصول",
        "ShopCategory" => "دسته فروشگاه",
        "Order" => "سفارش",
        "CartItem" => "آیتم سبد",
        "FormDefinition" => "فرم",
        "FormField" => "فیلد فرم",
        "MediaAsset" => "رسانه",
        _ => name
    };
}
