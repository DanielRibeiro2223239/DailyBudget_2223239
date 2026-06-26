using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace DailyBudgetWPF.Dados.Servicos
{
    public static class ServicoEmail
    {
        // Configurações SMTP standard (Ex: Gmail)
        // Para usar envio real, basta substituir com credenciais válidas e um App Password do Google
        private const string SMTP_HOST = "smtp.gmail.com";
        private const string SMTP_PORT = "587";
        private const string SMTP_USER = "teu-email@gmail.com"; 
        private const string SMTP_PASS = "tua-app-password"; 

        public static async Task<(bool sucesso, string erroOuInfo)> EnviarCodigoVerificacaoAsync(string emailDestinatario, string codigo)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Daily Budget", "noreply@dailybudget.com"));
                message.To.Add(new MailboxAddress("", emailDestinatario));
                message.Subject = "Código de Recuperação - Daily Budget";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 500px; margin: auto; padding: 30px; border: 1px solid #333333; border-radius: 16px; background-color: #141914; color: #ffffff;'>
                            <h2 style='color: #e67e22; text-align: center; font-size: 24px; margin-bottom: 20px;'>Daily Budget 🔑</h2>
                            <p style='font-size: 16px; line-height: 1.5; color: #cbd5e1;'>Olá,</p>
                            <p style='font-size: 16px; line-height: 1.5; color: #cbd5e1;'>Recebemos um pedido para redefinir a palavra-passe da tua conta.</p>
                            <div style='background-color: #1e291b; border: 1.5px solid #e67e22; border-radius: 12px; padding: 20px; margin: 25px 0; text-align: center;'>
                                <span style='font-size: 36px; font-weight: bold; letter-spacing: 6px; color: #e67e22; font-family: monospace;'>{codigo}</span>
                            </div>
                            <p style='font-size: 14px; color: #94a3b8; line-height: 1.5;'>Este código expira em 10 minutos. Se não solicitaste este pedido, podes ignorar este e-mail em segurança.</p>
                            <hr style='border: 0; border-top: 1px solid #333333; margin: 25px 0;' />
                            <p style='font-size: 12px; color: #64748b; text-align: center;'>Daily Budget - Gestão de Finanças Pessoais</p>
                        </div>"
                };

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                // Aceita qualquer certificado SSL para evitar problemas locais em redes escolares/empresariais
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                await client.ConnectAsync(SMTP_HOST, int.Parse(SMTP_PORT), SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(SMTP_USER, SMTP_PASS);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return (true, "Código enviado com sucesso!");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
