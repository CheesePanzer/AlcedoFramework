using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Alcedo.Core.Presentation.Mvc.TagHelpers;

[HtmlTargetElement("form")]
public sealed class AntiforgeryInjectionTagHelper : TagHelper
{
    private readonly IAntiforgery _antiForgery;

    public AntiforgeryInjectionTagHelper(IAntiforgery antiForgery)
    {
        _antiForgery = antiForgery;
    }

    [ViewContext] [HtmlAttributeNotBound] public ViewContext ViewContext { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (IsNotGetMethod(output))
        {
            var tokens = _antiForgery.GetAndStoreTokens(ViewContext.HttpContext);
            var input = new TagBuilder("input");
            input.Attributes.Add("name", "__RequestVerificationToken");
            input.Attributes.Add("type", "hidden");
            input.Attributes.Add("value", tokens.RequestToken);

            output.PostContent.AppendHtml(input);
        }
    }

    private static bool IsNotGetMethod(TagHelperOutput output)
    {
        var unsafeMethods = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            "post",
            "put",
            "patch",
            "delete"
        };

        if (output.Attributes.TryGetAttribute("method", out var method) &&
            method.Value is not null &&
            unsafeMethods.Contains(method.Value.ToString()!))
        {
            return true;
        }

        foreach (var attr in output.Attributes)
        {
            if (attr.Name.StartsWith("hx-", StringComparison.OrdinalIgnoreCase))
            {
                var httpMethod = attr.Name[3..]; // hx-post -> post

                if (unsafeMethods.Contains(httpMethod))
                {
                    return true;
                }
            }
        }

        return false;
    }
}