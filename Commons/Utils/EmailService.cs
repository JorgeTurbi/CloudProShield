namespace Commons.Utils;

using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Configuration;
using RazorLight;
using Services.EmailServices;

public class EmailService : IEmailService
{
    private readonly IConfiguration _cfg;
    private readonly SmtpClient _client;
    private readonly string _from;
    private readonly RazorLightEngine _razorEngine;
    private readonly string _logoUrl;
    private readonly string _inlineCss;

    public EmailService(IConfiguration cfg, RazorLightEngine razorEngine)
    {
        _cfg = cfg;
        _from = _cfg["EmailSettings:From"]!;
        _client = BuildSmtpClient();
        _razorEngine = razorEngine;
        _logoUrl = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Mail",
            "Assets",
            "img",
            "logo.png"
        );
        var cssPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Mail",
            "Assets",
            "css",
            "styles.css"
        );
        _inlineCss = File.Exists(cssPath) ? File.ReadAllText(cssPath) : string.Empty;
    }

    private SmtpClient BuildSmtpClient()
    {
        var host = _cfg["EmailSettings:SmtpHost"];
        var port = int.Parse(_cfg["EmailSettings:SmtpPort"]!);
        var user = _cfg["EmailSettings:SmtpUser"];
        var pass = _cfg["EmailSettings:SmtpPass"];
        var ssl = bool.Parse(_cfg["EmailSettings:EnableSsl"] ?? "true");

        return new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(user, pass),
            EnableSsl = ssl,
        };
    }

    private async Task<string> RenderTemplateAsync<T>(string templateName, T modelData)
    {
        try
        {
            // Agregamos propiedades base al modelo
            dynamic model = new System.Dynamic.ExpandoObject();

            // Copiar las propiedades del modelo original
            var expandoDict = model as IDictionary<string, object>;

            if (modelData is IDictionary<string, object> sourceDict)
            {
                foreach (var kvp in sourceDict)
                {
                    expandoDict[kvp.Key] = kvp.Value;
                }
            }
            else
            {
                foreach (var prop in modelData.GetType().GetProperties())
                {
                    expandoDict[prop.Name] = prop.GetValue(modelData);
                }
            }

            // Agregar propiedades comunes si no existen
            if (!expandoDict.ContainsKey("AppName"))
                expandoDict["AppName"] = _cfg["Application:Name"] ?? "CloudShield";

            if (!expandoDict.ContainsKey("Year"))
                expandoDict["Year"] = DateTime.Now.Year;

            if (!expandoDict.ContainsKey("InlineCss"))
                expandoDict["InlineCss"] = new HtmlString(_inlineCss);

            // Cargar y renderizar la plantilla
            string result = await _razorEngine.CompileRenderAsync($"{templateName}.cshtml", model);
            return result;
        }
        catch (Exception ex)
        {
            // Si hay un error al renderizar la plantilla, utilizar un contenido alternativo
            Console.WriteLine($"Error renderizando la plantilla {templateName}: {ex.Message}");
            throw new Exception($"Error al procesar la plantilla de correo: {ex.Message}", ex);
        }
    }

    /* ---------- método genérico para enviar emails ---------- */
    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var mail = new MailMessage
        {
            From = new MailAddress(_from),
            Subject = subject,
            IsBodyHtml = true,
        };
        mail.To.Add(to);

        var alternateView = AlternateView.CreateAlternateViewFromString(
            htmlBody,
            null,
            "text/html"
        );

        if (File.Exists(_logoUrl))
        {
            var logoResource = new LinkedResource(_logoUrl)
            {
                ContentId = "logoCid",
                TransferEncoding = System.Net.Mime.TransferEncoding.Base64,
                ContentType = new System.Net.Mime.ContentType("image/png"),
            };
            alternateView.LinkedResources.Add(logoResource);
        }

        mail.AlternateViews.Add(alternateView);

        await _client.SendMailAsync(mail);
    }

    /* ---------- Confirmación de cuenta / Bienvenida ---------- */
    public async Task SendConfirmationEmailAsync(string email, string token, string originUrl)
    {
        var appName = _cfg["Application:Name"] ?? "CloudShield";
        var baseUrl = string.IsNullOrEmpty(originUrl) ? _cfg["Frontend:BaseUrl"] : originUrl;
        var confirmUrl = $"{baseUrl}/confirm?token={token}";

        var model = new
        {
            AppName = appName,
            ConfirmUrl = confirmUrl,
            Year = DateTime.Now.Year,
        };

        var htmlBody = await RenderTemplateAsync("Welcome", model);
        await SendAsync(email, $"Confirma tu cuenta - {appName}", htmlBody);
    }

    /* ---------- Confirmación de cuenta exitosa o Validada ---------- */
    public async Task SendAccountConfirmedAsync(string email)
    {
        var appName = _cfg["Application:Name"] ?? "CloudShield";
        var loginUrl = $"{_cfg["Frontend:BaseUrl"]}/login";

        var model = new
        {
            AppName = appName,
            LoginUrl = loginUrl,
            Year = DateTime.Now.Year,
        };

        var htmlBody = await RenderTemplateAsync("AccountConfirmed", model);
        await SendAsync(email, $"Cuenta confirmada - {appName}", htmlBody);
    }

    /* ---------- Link para restablecer contraseña ---------- */
    public async Task SendResetLinkAsync(string to, string resetLink)
    {
        var appName = _cfg["Application:Name"] ?? "CloudShield";

        var model = new
        {
            AppName = appName,
            ResetLink = resetLink,
            ExpirationHours = 1,
            Year = DateTime.Now.Year,
        };

        var htmlBody = await RenderTemplateAsync("ForgotPassword", model);
        await SendAsync(to, $"Restablecimiento de contraseña – {appName}", htmlBody);
    }

    /* ---------- OTP ---------- */
    public async Task SendOtpAsync(string to, string otp)
    {
        var appName = _cfg["Application:Name"] ?? "CloudShield";

        var model = new
        {
            AppName = appName,
            Otp = otp,
            ExpirationMinutes = 5,
            Year = DateTime.Now.Year,
        };

        var htmlBody = await RenderTemplateAsync("Otp", model);
        await SendAsync(to, $"Código de verificación – {appName}", htmlBody);
    }

    /* ---------- Notificación de contraseña cambiada ---------- */
    public async Task SendPasswordChangedAsync(string to, string ipInfo)
    {
        var appName = _cfg["Application:Name"] ?? "CloudShield";

        var model = new
        {
            AppName = appName,
            IpInfo = ipInfo,
            Date = DateTime.UtcNow.ToString("g") + " UTC",
            Year = DateTime.Now.Year,
        };

        var htmlBody = await RenderTemplateAsync("PasswordChanged", model);
        await SendAsync(to, $"Contraseña actualizada – {appName}", htmlBody);
    }

    /* ---------- Notificación de inicio de sesión ---------- */
    public async Task SendLoginNotificationAsync(string to, string ipInfo, string device)
    {
        var appName = _cfg["Application:Name"] ?? "CloudShield";

        var model = new
        {
            AppName = appName,
            IpInfo = ipInfo,
            Device = device,
            Date = DateTime.UtcNow.ToString("g") + " UTC",
            Year = DateTime.Now.Year,
        };

        var htmlBody = await RenderTemplateAsync("Login", model);
        await SendAsync(to, $"Nuevo inicio de sesión – {appName}", htmlBody);
    }

    /* ---------- Notificación de creacion de cuenta automática via TaxPro ---------- */
    public async Task SendTemplatedEmailAsync<T>(
        string to,
        string subject,
        string template,
        T model
    )
    {
        var htmlBody = await RenderTemplateAsync(template, model);
        await SendAsync(to, subject, htmlBody);
    }

    /* ---------- Método genérico para enviar cualquier plantilla ---------- */
    public async Task SendAutoCreatedAccountAsync(string to, string tempPassword, string loginUrl)
    {
        var app = _cfg["Application:Name"] ?? "CloudShield";

        var model = new
        {
            AppName = app,
            Password = tempPassword,
            LoginUrl = loginUrl,
            Year = DateTime.Now.Year,
        };

        var html = await RenderTemplateAsync("AccountTaxProCreated", model);
        await SendAsync(to, $"Tu nueva cuenta en {app}", html);
    }
}
