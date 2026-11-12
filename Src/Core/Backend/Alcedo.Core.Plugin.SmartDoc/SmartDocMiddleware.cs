using System.Text;
using Alcedo.Core.Presentation.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Alcedo.Core.Plugin.SmartDoc;

public class SmartDocMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _docContent;
    private readonly string _routeTemplate;

    public SmartDocMiddleware(
        RequestDelegate next,
        string docContent,
        string routeTemplate)
    {
        _next = next;
        _docContent = docContent ?? throw new ArgumentNullException(nameof(docContent));
        _routeTemplate = routeTemplate ?? throw new ArgumentNullException(nameof(routeTemplate));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.Equals(_routeTemplate, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (!HttpMethods.IsGet(context.Request.Method))
        {
            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            return;
        }

        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.StatusCode = StatusCodes.Status200OK;

        await context.Response.WriteAsync(_docContent, Encoding.UTF8);
    }
}

public class SmartDocUiMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _uiRoute;
    private readonly string _jsonRoute;

    public SmartDocUiMiddleware(RequestDelegate next, string uiRoute, string jsonRoute)
    {
        _next = next;
        _uiRoute = uiRoute ?? throw new ArgumentNullException(nameof(uiRoute));
        _jsonRoute = jsonRoute ?? throw new ArgumentNullException(nameof(jsonRoute));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.Equals(_uiRoute, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.StatusCode = StatusCodes.Status200OK;

        string htmlPage = BuildHtmlUiPage(_jsonRoute);
        await context.Response.WriteAsync(htmlPage, Encoding.UTF8);
    }

    private static string BuildHtmlUiPage(string targetJsonApi)
    {
        return $$$"""
                  <!DOCTYPE html>
                  <html lang="en">
                  <head>
                      <meta charset="UTF-8">
                      <title>Alcedo Smart Doc UI</title>
                      <style>
                      body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; max-width: 1400px; margin: 0 auto; padding: 20px; background: #f8f9fa; color: #333; }
                      .dashboard-header { display: flex; justify-content: space-between; align-items: center; border-bottom: 2px solid #007bff; padding-bottom: 10px; margin-bottom: 30px; }
                      .dashboard-header h1 { margin: 0; color: #007bff; font-size: 28px; border: none; padding: 0; }
                      .api-link { display: inline-flex; align-items: center; background-color: #e9ecef; color: #495057; text-decoration: none; padding: 6px 12px; border-radius: 4px; font-family: Consolas, monospace; font-size: 13px; font-weight: 600; border: 1px solid #ced4da; transition: all 0.2s ease; }
                      .api-link:hover { background-color: #007bff; color: #fff; border-color: #007bff; }
                      .section { background: #fff; padding: 25px; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.06); margin-bottom: 25px; overflow-x: auto; }
                      table { width: 100%; border-collapse: collapse; margin: 15px 0; font-size: 14px; }
                      th, td { border: 1px solid #dee2e6; padding: 12px 10px; text-align: left; vertical-align: top; }
                      th { background-color: #f1f3f5; font-weight: 600; color: #495057; white-space: nowrap; }
                      tr:nth-child(even) { background-color: #fcfcfc; }
                      tr:hover { background-color: #f1f3f5; }
                      code.inline-code { background: #f1f3f5; color: #d63384; padding: 2px 6px; border-radius: 4px; font-family: Consolas, monospace; font-size: 13px; }
                      .config-key { font-family: Consolas, monospace; font-weight: 600; color: #212529; }
                      .config-value { font-family: Consolas, monospace; color: #0d6efd; }
                      .config-comment { color: #6c757d; font-size: 13px; }
                  </style>
                  </head>
                  <body>

                      <div class="dashboard-header">
                      <h1>Alcedo Smart Doc</h1>
                      <a class="api-link" href="{{{targetJsonApi}}}" target="_blank" rel="noopener noreferrer">
                          Json Doc
                      </a>
                  </div>

                      <div class="section">
                          <h2>1. Aggregate Roots</h2>
                          <div id="agg-root-container">Loading data via API...</div>
                      </div>

                      <div class="section">
                          <h2>2. Reference Relations</h2>
                          <div id="ref-relation-container">Loading data via API...</div>
                      </div>

                      <div class="section">
                          <h2>3. Application Settings Configuration</h2>
                          <div id="appsettings-container">Loading data via API...</div>
                      </div>

                      <script>
                          document.addEventListener('DOMContentLoaded', async () => {
                              try {
                                  const response = await fetch('{{{targetJsonApi}}}');
                                  if (!response.ok) throw new Error('Network response was not ok');
                                  const data = await response.json();

                                  // Render AggregateRoots
                                  if (data.AggregateRoots && data.AggregateRoots.length > 0) {
                                      let html = '<table><thead><tr><th>Code</th><th>Name</th><th>Module</th><th>Identity</th><th>TypeName</th></tr></thead><tbody>';
                                      data.AggregateRoots.forEach(item => {
                                          html += `<tr><td><strong>${item.Code}</strong></td><td>${item.Name}</td><td><code class="inline-code">${item.ModuleCode}</code></td><td><code class="inline-code">${item.Identity}</code></td><td style="color:#6c757d;">${item.TypeName}</td></tr>`;
                                      });
                                      html += '</tbody></table>';
                                      document.getElementById('agg-root-container').innerHTML = html;
                                  } else { document.getElementById('agg-root-container').innerHTML = 'No aggregate root data available.'; }

                                  // Render ReferenceRelations
                                  if (data.ReferenceRelations && data.ReferenceRelations.length > 0) {
                                      let html = '<table><thead><tr><th>Source Aggregate</th><th>Principal Aggregate</th><th>Foreign Key</th><th>Reference Mode</th><th>Navigation Path</th></tr></thead><tbody>';
                                      data.ReferenceRelations.forEach(item => {
                                          let pathStr = 'Directly Reference';
                                          if(item.NavigationPath && item.NavigationPath.length > 0) {
                                              pathStr = item.NavigationPath.map(p => `${p.PropertyName} (${p.PropertyType}${p.IsCollection ? '[]' : ''})`).join(' -> ');
                                          }
                                          html += `<tr><td>${item.SourceAggregateType.split('.').pop()}</td><td>${item.PrincipalAggregateType.split('.').pop()}</td><td><code class="inline-code">${item.ForeignKeyProperty}</code></td><td><span style="font-weight:600; color:${item.ReferenceMode==='Strict'?'#dc3545':'#28a745'}">${item.ReferenceMode}</span></td><td><small style="color:#495057;">${pathStr}</small></td></tr>`;
                                      });
                                      html += '</tbody></table>';
                                      document.getElementById('ref-relation-container').innerHTML = html;
                                  } else { document.getElementById('ref-relation-container').innerHTML = 'No reference relations data available.'; }

                                  // Render AppSettings
                                  if (data.AppSettings) {
                                      let html = '<table><thead><tr><th>Configuration Key</th><th>Value</th><th>Description / Comment</th></tr></thead><tbody>';
                                      
                                      function renderConfigRows(obj, prefix = '') {
                                          let rows = '';
                                          for (const key in obj) {
                                              const currentKey = prefix ? `${prefix}.${key}` : key;
                                              const node = obj[key];
                                              if (node && typeof node === 'object' && !Array.isArray(node)) {
                                                  if ('value' in node && 'comment' in node) {
                                                      let valDisplay = typeof node.value === 'object' ? JSON.stringify(node.value) : node.value;
                                                      rows += `<tr><td class="config-key">${currentKey}</td><td class="config-value">${valDisplay}</td><td class="config-comment">${node.comment || '-'}</td></tr>`;
                                                  } else {
                                                      rows += renderConfigRows(node, currentKey);
                                                  }
                                              } else {
                                                  let valDisplay = typeof node === 'object' ? JSON.stringify(node) : node;
                                                  rows += `<tr><td class="config-key">${currentKey}</td><td class="config-value">${valDisplay}</td><td class="config-comment">-</td></tr>`;
                                              }
                                          }
                                          return rows;
                                      }
                                      
                                      html += renderConfigRows(data.AppSettings);
                                      html += '</tbody></table>';
                                      document.getElementById('appsettings-container').innerHTML = html;
                                  } else { document.getElementById('appsettings-container').innerHTML = 'No configuration data available.'; }

                              } catch (err) {
                                  document.getElementById('agg-root-container').innerHTML = `<span style="color:red">Failed to load: ${err.message}</span>`;
                              }
                          });
                      </script>
                  </body>
                  </html>
                  """;
    }
}