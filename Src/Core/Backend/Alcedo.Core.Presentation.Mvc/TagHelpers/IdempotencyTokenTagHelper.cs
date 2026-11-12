using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Alcedo.Core.Presentation.Mvc.TagHelpers;

[HtmlTargetElement("form")]
public sealed class IdempotencyTokenTagHelper : TagHelper
{
    [ViewContext] [HtmlAttributeNotBound] public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var isPost = output.Attributes.ContainsName("hx-post") ||
                     (output.Attributes.TryGetAttribute("method", out var m) &&
                      m.Value?.ToString()?.Equals("post", StringComparison.OrdinalIgnoreCase) == true);
        if (!isPost)
        {
            return;
        }

        var hasStrictAttr = output.Attributes.TryGetAttribute("use-strict-idempotency", out var attr);
        var isStrict = hasStrictAttr &&
                       attr.Value?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

        string? token;
        if (isStrict)
        {
            token = ViewContext.ViewData["StrictIdempotencyKey"]?.ToString();
            if (string.IsNullOrEmpty(token))
            {
                token = "MISSING_STRICT_KEY";
            }
        }
        else
        {
            token = Guid.NewGuid().ToString();
        }

        var input = $"<input name=\"__FormUniqueRequestToken\" type=\"hidden\" value=\"{token}\" />";
        output.PostContent.AppendHtml(input);
        if (output.Attributes.ContainsName("hx-post") && !output.Attributes.ContainsName("method"))
        {
            output.Attributes.SetAttribute("method", "post");
        }
    }
}